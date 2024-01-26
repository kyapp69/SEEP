using System;
using DavidFDev.DevConsole;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using NaughtyAttributes;
using SEEP.InputHandlers;
using TMPro;
using UnityEngine;
using Logger = SEEP.Utils.Logger;

namespace SEEP.Network.Controllers
{
    /// <summary>
    /// Networked version of ground drone controller
    /// </summary>
    [RequireComponent(typeof(DroneInputHandler))]
    public class DroneController : NetworkBehaviour
    {
        #region SERIALIZED FIELDS

        /// <summary>
        /// A transform that is used to transform the player's input. This is usually an attached camera
        /// </summary>
        [SerializeField] private Transform playerInputSpace;

        /*
         * [SerializedField] private bool useCustomGravity;
         */

        /// <summary>
        /// Is climbing feature enabled? Currently not worked
        /// </summary>
        [Header("Speed settings")] [SerializeField]
        private bool climbingIsEnabled;

        /// <summary>
        /// Maximum horizontal speed in m/s
        /// </summary>
        [SerializeField] [Range(0f, 100f)] private float maxSpeed = 10f;

        /// <summary>
        /// Max speed for climbing
        /// </summary>
        [ShowIf(nameof(climbingIsEnabled))] [SerializeField] [Range(0f, 100f)]
        private float maxClimbSpeed = 4f;

        /// <summary>
        /// Max acceleration for ground moving
        /// </summary>
        [SerializeField] [Range(0f, 100f)] private float maxAcceleration = 10f;

        /// <summary>
        /// Max acceleration for air moving
        /// </summary>
        [SerializeField] [Range(0f, 100f)] private float maxAirAcceleration = 1f;

        /// <summary>
        /// Max acceleration for climbing moving. Currently not used
        /// </summary>
        [SerializeField] [Range(0f, 100f)] private float maxClimbAcceleration = 40f;

        /// <summary>
        /// Is jumping feature enabled?
        /// </summary>
        [Space(2f)] [Header("Jump settings")] [SerializeField]
        private bool jumpingIsEnabled;

        /// <summary>
        /// Desired height of jump in metres
        /// </summary>
        [ShowIf(nameof(jumpingIsEnabled))] [SerializeField] [Range(0f, 10f)]
        private float jumpHeight = 2f;

        /// <summary>
        /// How much jumps in air can does?
        /// </summary>
        [ShowIf(nameof(jumpingIsEnabled))] [SerializeField] [Range(0, 5)]
        private int maxAirJumps;

        /// <summary>
        /// EXPERIMENTAL: Compensate gravity force on slopes. Currently not needed
        /// </summary>
        [Space(2f)] [Header("Moving settings")] [InfoBox("Experimental feature")] [SerializeField]
        private bool compensateStairsForce;

        /// <summary>
        /// Max angle for ground layer
        /// </summary>
        [SerializeField] [Range(0, 90)] private float maxGroundAngle = 25f;

        /// <summary>
        /// Max angle for stairs layer
        /// </summary>
        [SerializeField] [Range(0, 90)] private float maxStairsAngle = 50f;

        /// <summary>
        /// Max angle for climbing
        /// </summary>
        [SerializeField] [Range(90, 170)] private float maxClimbAngle = 140f;

        /// <summary>
        /// The maximum speed at which an object will be snapping to the ground if it jumps on a bump,
        /// but the angle of the surface under it is still acceptable
        /// </summary>
        [SerializeField] [Range(0f, 100f)] private float maxSnapSpeed = 100f;

        /// <summary>
        /// The distance to which the raycast is released to check the location of the earth under
        /// the object for snapping to the earth
        /// </summary>
        [SerializeField] [Min(0f)] private float probeDistance = 1f;

        /// <summary>
        /// LayerMask which describes ground layers
        /// </summary>
        [SerializeField] private LayerMask probeMask = -1;

        /// <summary>
        /// LayerMask which describes stairs layers
        /// </summary>
        [SerializeField] private LayerMask stairsMask = -1;

        [ShowIf(nameof(climbingIsEnabled))] [SerializeField]
        private LayerMask climbMask = -1;

        #endregion

        #region PRIVATE

        /// <summary>
        /// Attached rigidbody component
        /// </summary>
        private Rigidbody _body;

        /// <summary>
        /// The rigidbody component of the object on which our drone was standing.
        /// It is necessary for smooth movement on moving objects
        /// </summary>
        private Rigidbody _connectedBody;

        /// <summary>
        /// Just like _connectedBody, it only stores the object that was in the last frame
        /// </summary>
        private Rigidbody _previousConnectedBody;

        /// <summary>
        /// It is needed for the Rotate method. Stores the calculated angle for object rotation.
        /// Each frame is smoothly overwritten
        /// </summary>
        private float _calculatedAngle;

        /// <summary>
        /// Stores the speed of the angle change needed for a smooth rotation of the object
        /// </summary>
        private float _currentAngleVelocity;

        /// <summary>
        /// Stores the world position of the _connectedBody object
        /// </summary>
        private Vector3 _connectionWorldPosition;

        /// <summary>
        /// Stores the local position of the _connectedBody object
        /// </summary>
        private Vector3 _connectionLocalPosition;

        /// <summary>
        /// Stores the normal of the surface, which is considered to be the earth
        /// </summary>
        private Vector3 _contactNormal;

        /// <summary>
        /// Stores the normal of the surface, which is considered to be the steep
        /// </summary>
        private Vector3 _steepNormal;

        /// <summary>
        /// Stores the normal of the surface, which is considered to be the climb surface
        /// </summary>
        private Vector3 _climbNormal;

        /// <summary>
        /// Stores the normal of the surface, which is considered to be
        /// the last climb surface. Needed for climbing calculations
        /// </summary>
        private Vector3 _lastClimbNormal;

        /// <summary>
        /// Stores the number of contacts with the surface, which is considered to be the earth surface
        /// </summary>
        private int _groundContactCount;

        /// <summary>
        /// Stores the number of contacts with the surface, which is considered to be the steep surface
        /// </summary>
        private int _steepContactCount;

        /// <summary>
        /// Stores the number of contacts with the surface, which is considered to be the climb surface
        /// </summary>
        private int _climbContactCount;

        /// <summary>
        /// An internal jump counter is required to count jumps in the air.
        /// Does not display the current jump
        /// </summary>
        private int _jumpPhase;

        /// <summary>
        /// Pre-calculated (OnValidate) value that translate the maximum angle of elevation on the ground.
        /// It is necessary to optimize the calculation
        /// </summary>
        private float _minGroundDotProduct;

        /// <summary>
        /// Pre-calculated (OnValidate) value that translate the maximum angle of elevation on the stairs.
        /// It is necessary to optimize the calculation
        /// </summary>
        private float _minStairsDotProduct;

        /// <summary>
        /// Pre-calculated (OnValidate) value that translate the maximum angle of elevation on the climb surface.
        /// It is necessary to optimize the calculation
        /// </summary>
        private float _minClimbDotProduct;

        /// <summary>
        /// The cached acceleration value. At the beginning of the tick,
        /// it is cached from the current state of rigidbody.
        /// As the tick is calculated and changed. At the end, the tick is applied to the rigidbody
        /// </summary>
        private Vector3 _velocity;

        private Vector3 _stairsForce;

        /// <summary>
        /// Velocity of _connectedBody
        /// </summary>
        private Vector3 _connectionVelocity;

        /// <summary>
        /// Internal counter of physics steps since last ground contact
        /// </summary>
        private int _stepsSinceLastGrounded;

        /// <summary>
        /// Internal counter of physics steps since last jump
        /// </summary>
        private int _stepsSinceLastJump;

        /// <summary>
        /// Vectors defining the axes relative to the object
        /// </summary>
        private Vector3 _upAxis, _rightAxis, _forwardAxis;

        /// <summary>
        /// Gravity direction and force at current position. Needed to work with custom gravity.
        /// Currently just link to Physics.gravity
        /// </summary>
        private Vector3 _gravity;

        /// <summary>
        /// Input handler
        /// </summary>
        private DroneInputHandler _droneInput;

        /// <summary>
        /// Cached input bool
        /// </summary>
        private bool _desiredJump, _desiresClimbing;

        /// <summary>
        /// Cached input vector
        /// </summary>
        private Vector2 _playerInput;

        private bool OnGround => _groundContactCount > 0;

        private bool OnSteep => _steepContactCount > 0;

        private bool Climbing => _climbContactCount > 0 && _stepsSinceLastJump > 2;

        #endregion

        #region MONOBEHAVIOUR

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _droneInput = GetComponent<DroneInputHandler>();
            _upAxis = Vector3.up;
            _gravity = Physics.gravity;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            OnValidate();
        }

        private void Update()
        {
            if (DevConsole.IsOpen || OwnerId != ClientManager.Connection.ClientId) return;

            if (_droneInput)
            {
                _playerInput = _droneInput.Control;
                _desiredJump |= _droneInput.Jump;
            }

            if (playerInputSpace)
            {
                _rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, _upAxis);
                _forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, _upAxis);
            }
            else
            {
                _rightAxis = ProjectDirectionOnPlane(Vector3.right, _upAxis);
                _forwardAxis = ProjectDirectionOnPlane(Vector3.forward, _upAxis);
            }

            _desiresClimbing = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            EvaluateCollision(collision);
        }


        protected override void OnValidate()
        {
            base.OnValidate();
            _minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            _minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
            _minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
            /*if (!useCustomGravity)
            {
                _upAxis = Vector3.up;
                _gravity = Physics.gravity;
            }
            else
            {
                compensateStairsForce = false;
            }*/
        }

        #endregion

        #region METHODS

        private void RotateObject()
        {
            var cachedVelocity = _body.velocity;
            cachedVelocity.y = 0;
            if (!(cachedVelocity.magnitude > 0.1f)) return;

            var targetAngle = Mathf.Atan2(cachedVelocity.x, cachedVelocity.z) * Mathf.Rad2Deg;

            _calculatedAngle = Mathf.SmoothDampAngle(_calculatedAngle, targetAngle,
                ref _currentAngleVelocity,
                0.04f);

            //Apply calculated angle
            transform.rotation = Quaternion.Euler(0, _calculatedAngle, 0);
        }

        private void ClearState()
        {
            _groundContactCount = _steepContactCount = _climbContactCount = 0;
            _contactNormal = _steepNormal = _climbNormal = Vector3.zero;
            _connectionVelocity = Vector3.zero;
            _previousConnectedBody = _connectedBody;
            _connectedBody = null;
        }

        private void UpdateState()
        {
            _stepsSinceLastGrounded += 1;
            _stepsSinceLastJump += 1;
            _velocity = _body.velocity;
            if (
                CheckClimbing() || OnGround || SnapToGround() || CheckSteepContacts()
            )
            {
                _stepsSinceLastGrounded = 0;
                if (_stepsSinceLastJump > 1) _jumpPhase = 0;

                if (_groundContactCount > 1) _contactNormal.Normalize();
            }
            else
            {
                _contactNormal = _upAxis;
            }

            if (!_connectedBody) return;

            if (_connectedBody.isKinematic || _connectedBody.mass >= _body.mass) UpdateConnectionState();
        }

        private void CompensateStairsForce()
        {
            if (!Physics.Raycast(_body.position, -_upAxis, out var hit, probeDistance, stairsMask) || !OnGround) return;

            _stairsForce = ProjectForceOnNormal(_gravity, hit.normal) * (float)TimeManager.TickDelta;
            _velocity -= _stairsForce;
        }

        private void UpdateConnectionState()
        {
            if (_connectedBody == _previousConnectedBody)
            {
                var connectionMovement = _connectedBody.transform.TransformPoint(_connectionLocalPosition) -
                                         _connectionWorldPosition;
                _connectionVelocity = connectionMovement / (float)TimeManager.TickDelta;
            }

            _connectionWorldPosition = _body.position;
            _connectionLocalPosition = _connectedBody.transform.InverseTransformPoint(_connectionWorldPosition);
        }

        private bool CheckClimbing()
        {
            if (!Climbing) return false;

            if (_climbContactCount > 1)
            {
                _climbNormal.Normalize();
                var upDot = Vector3.Dot(_upAxis, _climbNormal);
                if (upDot >= _minGroundDotProduct) _climbNormal = _lastClimbNormal;
            }

            _groundContactCount = 1;
            _contactNormal = _climbNormal;
            return true;
        }

        private bool SnapToGround()
        {
            if (_stepsSinceLastGrounded > 1 || _stepsSinceLastJump <= 2) return false;

            var speed = _velocity.magnitude;
            if (speed > maxSnapSpeed) return false;

            if (!Physics.Raycast(
                    _body.position, -_upAxis, out var hit,
                    probeDistance, probeMask
                ))
                return false;

            var upDot = Vector3.Dot(_upAxis, hit.normal);
            if (upDot < GetMinDot(hit.collider.gameObject.layer)) return false;

            _groundContactCount = 1;
            _contactNormal = hit.normal;
            var dot = Vector3.Dot(_velocity, hit.normal);
            if (dot > 0f) _velocity = (_velocity - hit.normal * dot).normalized * speed;

            _connectedBody = hit.rigidbody;
            return true;
        }

        private bool CheckSteepContacts()
        {
            if (_steepContactCount <= 1) return false;

            _steepNormal.Normalize();
            var upDot = Vector3.Dot(_upAxis, _steepNormal);
            if (!(upDot >= _minGroundDotProduct)) return false;

            _steepContactCount = 0;
            _groundContactCount = 1;
            _contactNormal = _steepNormal;
            return true;
        }

        private void AdjustVelocity(Vector2 playerInput, Vector3 rightAxis, Vector3 forwardAxis)
        {
            /*if (Climbing)
            {
                acceleration = maxClimbAcceleration;
                speed = maxClimbSpeed;
                xAxis = Vector3.Cross(_contactNormal, _upAxis);
                zAxis = _upAxis;
            }
            else
            {
                acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
                speed = OnGround && _desiresClimbing ? maxClimbSpeed : maxSpeed;
                xAxis = _rightAxis;
                zAxis = _forwardAxis;
            }*/
            //speed = OnGround && _desiresClimbing ? maxClimbSpeed : maxSpeed;

            var acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
            var speed = maxSpeed;
            var xAxis = rightAxis;
            var zAxis = forwardAxis;

            xAxis = ProjectDirectionOnPlane(xAxis, _contactNormal);
            zAxis = ProjectDirectionOnPlane(zAxis, _contactNormal);

            var relativeVelocity = _velocity - _connectionVelocity;
            var currentX = Vector3.Dot(relativeVelocity, xAxis);
            var currentZ = Vector3.Dot(relativeVelocity, zAxis);

            var maxSpeedChange = acceleration * (float)TimeManager.TickDelta;

            var newX = Mathf.MoveTowards(currentX, playerInput.x * speed, maxSpeedChange);
            var newZ = Mathf.MoveTowards(currentZ, playerInput.y * speed, maxSpeedChange);

            _velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        }

        public void PreventSnapToGround()
        {
            _stepsSinceLastJump = -1;
        }

        private void Jump(Vector3 gravity)
        {
            Vector3 jumpDirection;
            if (OnGround)
            {
                jumpDirection = _contactNormal;
            }
            else if (OnSteep)
            {
                jumpDirection = _steepNormal;
                _jumpPhase = 0;
            }
            else if (maxAirJumps > 0 && _jumpPhase <= maxAirJumps)
            {
                if (_jumpPhase == 0)
                {
                    _jumpPhase = 1;
                }

                jumpDirection = _contactNormal;
            }
            else
            {
                return;
            }

            _stepsSinceLastJump = 0;
            _jumpPhase += 1;
            var jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
            jumpDirection = (jumpDirection + _upAxis).normalized;
            var alignedSpeed = Vector3.Dot(_velocity, jumpDirection);
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }

            _velocity += jumpDirection * jumpSpeed;
        }

        private void EvaluateCollision(Collision collision)
        {
            var layer = collision.gameObject.layer;
            var minDot = GetMinDot(layer);
            for (var i = 0; i < collision.contactCount; i++)
            {
                var normal = collision.GetContact(i).normal;
                var upDot = Vector3.Dot(_upAxis, normal);
                if (upDot >= minDot)
                {
                    _groundContactCount += 1;
                    _contactNormal += normal;
                    _connectedBody = collision.rigidbody;
                }
                else
                {
                    if (upDot > -0.01f)
                    {
                        _steepContactCount += 1;
                        _steepNormal += normal;
                        if (_groundContactCount == 0) _connectedBody = collision.rigidbody;
                    }

                    if (!_desiresClimbing || !(upDot >= _minClimbDotProduct) ||
                        (climbMask & (1 << layer)) == 0) continue;

                    _climbContactCount += 1;
                    _climbNormal += normal;
                    _lastClimbNormal = normal;
                    _connectedBody = collision.rigidbody;
                }
            }
        }

        private DroneMoveData BuildMoveData()
        {
            var md = new DroneMoveData(_playerInput, _desiredJump, _rightAxis, _forwardAxis);

            return md;
        }

        #endregion

        #region UTILS

        private static Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        private Vector3 ProjectForceOnNormal(Vector3 vector, Vector3 normal)
        {
            return vector - normal * Vector3.Dot(vector, normal);
        }

        private float GetMinDot(int layer)
        {
            return (stairsMask & (1 << layer)) == 0 ? _minGroundDotProduct : _minStairsDotProduct;
        }

        #endregion

        #region TIMEMANAGER

        //It is called on the client and server, on the server tick
        private void TimeManager_OnTick()
        {
            if (base.IsOwner)
            {
                Reconciliation(default, false);
                Move(BuildMoveData(), false);
            }

            if (!base.IsServer) return;
            Move(default, true);
            var rd = new DroneReconcileData()
            {
                Position = transform.position,
                Velocity = _body.velocity,
                Rotation = transform.rotation
            };
            Reconciliation(rd, true);
        }

        #endregion

        #region NETWORK

        //Work only on client, when his is connected to server
        // ReSharper disable Unity.PerformanceAnalysis
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            //Subscribing on server tick events
            TimeManager.OnTick += TimeManager_OnTick;
            //TimeManager.OnPostTick += TimeManager_OnPostTick;

            if (OwnerId != ClientManager.Connection.ClientId) return;
            //Caching some components, that required on client
            _droneInput = GetComponent<DroneInputHandler>();
            if (Camera.main != null) playerInputSpace = Camera.main.transform;
        }

        //Called only on client, when disconnect from server
        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            //Desubscribing from server tick events
            TimeManager.OnTick -= TimeManager_OnTick;
        }

        [Replicate]
        private void Move(DroneMoveData md, bool asServer, Channel channel = Channel.Unreliable,
            bool replaying = false)
        {
            /*if (useCustomGravity)
                _gravity = CustomGravity.GetGravity(_body.position, out _upAxis);*/
            RotateObject();
            UpdateState();
            AdjustVelocity(md.PlayerInput, md.Right, md.Forward);
            _gravity = Physics.gravity;

            if (compensateStairsForce)
                CompensateStairsForce();

            if (md.Jump && jumpingIsEnabled)
            {
                _desiredJump = false;
                Jump(_gravity);
            }

            /*if (Climbing)
                _velocity -= _contactNormal * (maxClimbAcceleration * 0.9f * (float)TimeManager.TickDelta);
            else if (OnGround && _velocity.sqrMagnitude < 0.01f)
                _velocity += _contactNormal * (Vector3.Dot(_gravity, _contactNormal) * (float)TimeManager.TickDelta);
            else if (_desiresClimbing && OnGround)
                _velocity += (_gravity - _contactNormal * (maxClimbAcceleration * 0.9f)) * (float)TimeManager.TickDelta;
            else if (useCustomGravity)
                _velocity += _gravity * (float)TimeManager.TickDelta;*/

            if (OnGround && _velocity.sqrMagnitude < 0.01f)
                _velocity += _contactNormal * (Vector3.Dot(_gravity, _contactNormal) * (float)TimeManager.TickDelta);
            _body.velocity = _velocity;
            ClearState();
        }

        [Reconcile]
        private void Reconciliation(DroneReconcileData rd, bool asServer, Channel channel = Channel.Unreliable)
        {
            transform.SetPositionAndRotation(rd.Position, rd.Rotation);
            _body.velocity = rd.Velocity;
        }

        #endregion
    }
}