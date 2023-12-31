using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;
using SEEP.InputHandlers;

namespace SEEP.Network.Controllers
{
    /// <summary>
    /// Controller for ground drone 
    /// </summary>
    [RequireComponent(typeof(DroneInputHandler))]
    public class DroneController : NetworkBehaviour
    {
        [Tooltip("Jump force in newtons")] [SerializeField]
        private float jumpForce = 15f;

        [Tooltip("Acceleration force in newtons")] [SerializeField]
        private float accelerationForce = 70f;

        [Tooltip("Acceleration force in air in newtons")] [SerializeField]
        private float airAccelerationForce = 15f;

        [Tooltip("Max horizontal speed")] [SerializeField]
        private float maxSpeed = 8f;

        [Tooltip("Time in seconds to cooldown jump ability")] [SerializeField]
        private float jumpCooldown = 1f;

        [Tooltip("Time in seconds, to smooth the rotation of the drone")] [SerializeField]
        private float rotationSmoothTime = 0.15f;

        [Tooltip("How fast drone should compensate sideways drag (Default = 1f)")] [SerializeField]
        private float sidewaysDragReduceMultiplier = 1f;

        [Tooltip("How far down will it be checked if there is ground under the drone")] [SerializeField]
        private float distanceToGround = 0.1f;

        [Tooltip("Which layers will be detected as ground")] [SerializeField]
        private LayerMask groundLayer;

        #region PRIVATE

        /// <summary>
        /// DroneInputHandler component
        /// </summary>
        private DroneInputHandler _input;

        /// <summary>
        /// Attached Rigidbody
        /// </summary>
        private Rigidbody _rigidbody;

        /// <summary>
        /// Attached Collider
        /// </summary>
        private Collider _collider;

        /// <summary>
        /// Transformed player movement to Vector3
        /// </summary>
        private Vector3 _movement;

        /// <summary>
        /// A variable for storing the jump. It is needed in order to cache the player's desire to jump
        /// and send it to the server when the server tick comes
        /// </summary>
        private bool _jump;

        /// <summary>
        /// Smoothed angle to rotate. Something between target angle and current angle.
        /// Changing depends on rotationSmoothTime.
        /// </summary>
        private float _calculatedAngle;

        /// <summary>
        /// Target rotation angle from our input
        /// </summary>
        private float _targetAngle;

        /// <summary>
        /// The variable needed to calculate the angle.
        /// Stores the acceleration of the angle change to the target angle
        /// </summary>
        private float _currentAngleVelocity;

        /// <summary>
        /// Cached MainCamera transform
        /// </summary>
        private Transform _mainCameraTransform;

        /// <summary>
        /// Float required to count jump cooldown. Updated in fixedUpdate
        /// </summary>
        private float _jumpTimer;

        private DroneMoveData _lastMoveData;

        #endregion

        #region MONOBEHAVIOUR

        //Caching some components
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();
        }

        //Update user input
        private void Update()
        {
            //Work only owner of object
            if (!IsOwner) return;

            //Converting Vector2 input to Vector3
            _movement = new Vector3(_input.Control.x, 0, _input.Control.y);

            //If player want to jump, check if we can do this, and caching request for next server tick
            if (!_jump && _input.Jump && _jumpTimer <= 0f && IsGrounded())
            {
                _jump = true;
                _jumpTimer = jumpCooldown;
            }
        }

        //Rotate drone in movement direction
        private void FixedUpdate()
        {
            if (!IsOwner) return;
            
            //Calculate our jumpTimer
            if (_jumpTimer > 0f)
                _jumpTimer -= Time.fixedDeltaTime;

            //If we dont have any input -> exit
            if (!(_input.Control.magnitude >= 0.1f)) return;

            //Calculate target angle to our movement
            _targetAngle = Mathf.Atan2(_movement.x, _movement.z) * Mathf.Rad2Deg +
                           _mainCameraTransform.eulerAngles.y;

            //Calculate smoothed angle from current to target
            _calculatedAngle = Mathf.SmoothDampAngle(_calculatedAngle, _targetAngle,
                ref _currentAngleVelocity, rotationSmoothTime);

            //Apply calculated angle
            transform.rotation = Quaternion.Euler(0, _calculatedAngle, 0);
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Method to check if we grounded
        /// </summary>
        /// <returns>True if grounded, and false if not</returns>
        private bool IsGrounded()
        {
            var boxCastSize = new Vector3(_collider.bounds.size.x, distanceToGround, _collider.bounds.size.z);
            return Physics.BoxCast(_collider.bounds.center, boxCastSize / 2, Vector3.down, out _,
                Quaternion.identity, _collider.bounds.extents.y + distanceToGround, groundLayer);
        }

        /// <summary>
        /// Build required data to replicate player input on server
        /// </summary>
        /// <returns>Built DroneMoveData</returns>
        private DroneMoveData BuildMoveData()
        {
            //Work only owner of object
            if (!IsOwner)
                return default;

            //Our DroneMoveData require only direction in which player want to move
            //There is a small problem here. If we play from the gamepad,
            //it doesn't matter how much we deflect the stick,
            //the drone will still start moving with maximum acceleration
            //TODO: Rework move data. Need to add acceleration force support
            var md = new DroneMoveData(_targetAngle, _movement.magnitude >= 0.1f, _jump, transform.rotation);

            return md;
        }

        #endregion

        #region TIMEMANAGER

        //It is called on the client and server, on the server tick
        private void TimeManager_OnTick()
        {
            Move(BuildMoveData());
        }

        //Called after the server tick
        private void TimeManager_OnPostTick()
        {
            //Works only on server
            if (!IsServer) return;

            //Build reconcile data, from server
            var rd = new DroneReconcileData(transform.position, _rigidbody.velocity,
                _rigidbody.angularVelocity);

            //And send to our clients
            Reconciliation(rd);
        }

        #endregion

        #region NETWORK

        //Work only on client, when his is connected to server
        // ReSharper disable Unity.PerformanceAnalysis
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            //Caching some components, that required on client
            _input = GetComponent<DroneInputHandler>();
            _mainCameraTransform = Camera.main?.transform;

            //Subscribing on server tick events
            TimeManager.OnTick += TimeManager_OnTick;
            TimeManager.OnPostTick += TimeManager_OnPostTick;
        }

        //Called only on client, when disconnect from server
        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            //Desubscribing from server tick events
            TimeManager.OnTick -= TimeManager_OnTick;
            TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }

        /// <summary>
        /// Move drone. Called at the same time on server, and clients. Required for client-side prediction
        /// </summary>
        /// <param name="md">Move data</param>
        /// <param name="state">Check FishNet docs</param>
        /// <param name="channel">Check FishNet docs</param>
        [ReplicateV2]
        private void Move(DroneMoveData md, ReplicateState state = ReplicateState.Invalid,
            Channel channel = Channel.Unreliable)
        {
            if (!IsOwner)
            {
                transform.rotation = md.Rotation;
                if (state is ReplicateState.ReplayedPredicted or ReplicateState.Predicted)
                {
                    uint tick = md.GetTick();
                    md = _lastMoveData;
                    md.SetTick(tick);
                }
                else
                {
                    _lastMoveData = md;
                }
            }
            
            //If we has a input and we grounded - apply our movement forces
            if (md.IsMoving && IsGrounded())
            {
                //Calculate assigned vector to our smoothed angle
                var rotatedMovement = Quaternion.Euler(0, md.TargetAngle, 0) * Vector3.forward;

                //And apply him
                _rigidbody.AddForce(rotatedMovement * accelerationForce, ForceMode.Force);

                //Get velocity of sideways drag
                var velocity = transform.InverseTransformDirection(_rigidbody.velocity);

                //Apply inverted force of sideways drag according to multiplier
                _rigidbody.AddForce(transform.right * (-velocity.x * sidewaysDragReduceMultiplier));
            }
            //If we have input and we an air
            else if (md.IsMoving)
            {
                //Calculate assigned vector to our smoothed angle
                var rotatedMovement = Quaternion.Euler(0, md.TargetAngle, 0) * Vector3.forward;

                //And apply him with air movement force
                _rigidbody.AddForce(rotatedMovement * airAccelerationForce, ForceMode.Force);

                //Get velocity of sideways drag
                var velocity = transform.InverseTransformDirection(_rigidbody.velocity);

                //Apply inverted force of sideways drag according to multiplier
                _rigidbody.AddForce(transform.right * (-velocity.x * sidewaysDragReduceMultiplier));
            }

            //If jump requested
            if (md.Jump)
            {
                _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                _jump = false;
            }

            //Get only horizontal velocity
            var filteredVelocity = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);

            //If horizontal velocity magnitude equal or less then max speed - exit from method
            if (!(filteredVelocity.magnitude > maxSpeed)) return;

            //Else clamp horizontal velocity
            filteredVelocity = Vector3.ClampMagnitude(filteredVelocity, maxSpeed);

            //And reapply to our rigidbody
            _rigidbody.velocity = new Vector3(filteredVelocity.x, _rigidbody.velocity.y, filteredVelocity.z);
        }

        /// <summary>
        /// Rollback client status to server status of object if client has a desynchronization from server 
        /// </summary>
        /// <param name="rd">Data for reconciliation</param>
        /// <param name="channel">Check FishNet docs</param>
        [ReconcileV2]
        private void Reconciliation(DroneReconcileData rd, Channel channel = Channel.Unreliable)
        {
            transform.position = rd.Position;
            _rigidbody.velocity = rd.Velocity;
            _rigidbody.angularVelocity = rd.AngularVelocity;
        }

        #endregion
    }
}