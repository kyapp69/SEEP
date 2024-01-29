using DavidFDev.DevConsole;
using NaughtyAttributes;
using SEEP.InputHandlers;
using SEEP.Utils;
using UnityEngine;

namespace SEEP.Offline.Controllers
{
    public class DroneController : MonoBehaviour
    {
        [SerializeField] private Transform playerInputSpace;
        [SerializeField] private bool useCustomGravity;

        [Header("Speed settings")] [SerializeField]
        private bool climbingIsEnabled;

        [SerializeField] [Range(0f, 100f)] private float maxSpeed = 10f;

        [ShowIf(nameof(climbingIsEnabled))] [SerializeField] [Range(0f, 100f)]
        private float maxClimbSpeed = 4f;

        [SerializeField] [Range(0f, 100f)] private float
            maxAcceleration = 10f,
            maxAirAcceleration = 1f,
            maxClimbAcceleration = 40f;

        [Space(2f)] [Header("Jump settings")] [SerializeField]
        private bool jumpingIsEnabled;

        [ShowIf(nameof(jumpingIsEnabled))] [SerializeField] [Range(0f, 10f)]
        private float jumpHeight = 2f;

        [ShowIf(nameof(jumpingIsEnabled))] [SerializeField] [Range(0, 5)]
        private int maxAirJumps;

        [Space(2f)]
        [Header("Moving settings")]
        [InfoBox("Experimental feature")]
        [SerializeField]
        [HideIf(nameof(useCustomGravity))]
        private bool compensateStairsForce;

        [SerializeField] private bool groundMovingIsEnabled;

        [ShowIf(nameof(groundMovingIsEnabled))] [SerializeField] [Range(0, 90)]
        private float maxGroundAngle = 25f, maxStairsAngle = 50f;

        [ShowIf(nameof(groundMovingIsEnabled))] [SerializeField] [Range(90, 170)]
        private float maxClimbAngle = 140f;

        [ShowIf(nameof(groundMovingIsEnabled))] [SerializeField] [Range(0f, 100f)]
        private float maxSnapSpeed = 100f;

        [ShowIf(nameof(groundMovingIsEnabled))] [SerializeField] [Min(0f)]
        private float probeDistance = 1f;

        [ShowIf(nameof(groundMovingIsEnabled))] [SerializeField]
        private LayerMask probeMask = -1, stairsMask = -1;

        [ShowIf(nameof(climbingIsEnabled))] [SerializeField]
        private LayerMask climbMask = -1;

        [Space(2f)] [Header("Rotating settings")] [SerializeField]
        private RotationType rotationType;

        private Rigidbody _body, _connectedBody, _previousConnectedBody;

        private float _calculatedAngle, _currentAngleVelocity;

        private Vector3 _connectionWorldPosition, _connectionLocalPosition;

        private Vector3 _contactNormal, _steepNormal, _climbNormal, _lastClimbNormal;

        private bool _desiredJump, _desiresClimbing;

        private DroneInputHandler _droneInput;

        private Quaternion _gravityRotation, _gravityRotationVelocity;

        private int _groundContactCount, _steepContactCount, _climbContactCount;

        private int _jumpPhase;

        private float _minGroundDotProduct, _minStairsDotProduct, _minClimbDotProduct;

        private Vector2 _playerInput;

        private int _stepsSinceLastGrounded, _stepsSinceLastJump;

        private Vector3 _upAxis, _rightAxis, _forwardAxis, _gravity;

        private Vector3 _velocity, _connectionVelocity;

        private bool OnGround => _groundContactCount > 0;

        private bool OnSteep => _steepContactCount > 0;

        private bool Climbing => _climbContactCount > 0 && _stepsSinceLastJump > 2;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = !useCustomGravity;
            _droneInput = GetComponent<DroneInputHandler>();
            OnValidate();
        }

        private void Start()
        {
#if DEBUG
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Physics.simulationMode = SimulationMode.FixedUpdate;
            DebugInitialize();
#endif
        }

        private void Update()
        {
            if (_droneInput)
            {
                _playerInput = _droneInput.Control;
                _desiredJump |= _droneInput.Jump;
            }

            //_playerInput = Vector2.ClampMagnitude(_playerInput, 1f);

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

            //desiresClimbing = Input.GetButton("Interact");
            _desiresClimbing = false;
            RotateObject();
        }

        private void FixedUpdate()
        {
            /*if (useCustomGravity)
                _gravity = CustomGravity.GetGravity(_body.position, out _upAxis);*/
            UpdateState();
            AdjustVelocity();

            if (compensateStairsForce)
                CompensateStairsForce();

            if (_desiredJump && jumpingIsEnabled)
            {
                _desiredJump = false;
                Jump(_gravity);
            }

            if (Climbing)
                _velocity -= _contactNormal * (maxClimbAcceleration * 0.9f * Time.deltaTime);
            else if (OnGround && _velocity.sqrMagnitude < 0.01f)
                _velocity += _contactNormal * (Vector3.Dot(_gravity, _contactNormal) * Time.deltaTime);
            else if (_desiresClimbing && OnGround)
                _velocity += (_gravity - _contactNormal * (maxClimbAcceleration * 0.9f)) * Time.deltaTime;
            else if (useCustomGravity)
                _velocity += _gravity * Time.deltaTime;

            _body.velocity = _velocity;
            ClearState();
        }

        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            EvaluateCollision(collision);
        }


        private void OnValidate()
        {
            if (!useCustomGravity)
            {
                _upAxis = Vector3.up;
                _gravity = Physics.gravity;
            }
            else
            {
                compensateStairsForce = false;
            }

            _minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            _minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
            _minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
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
            _velocity -= ProjectOnContactPlane(_gravity) * Time.fixedDeltaTime;
        }

        private void RotateObject()
        {
            switch (rotationType)
            {
                case RotationType.RotateWithVelocity:
                    var cachedVelocity = _body.velocity;
                    cachedVelocity.y = 0;
                    if (cachedVelocity.magnitude > 0.1f)
                    {
                        var targetAngle = Mathf.Atan2(cachedVelocity.x, cachedVelocity.z) * Mathf.Rad2Deg;

                        _calculatedAngle = Mathf.SmoothDampAngle(_calculatedAngle, targetAngle,
                            ref _currentAngleVelocity,
                            0.3f);
                        transform.rotation = Quaternion.Euler(0, _calculatedAngle, 0);
                    }

                    break;
                case RotationType.RotateWithCamera:
                    _calculatedAngle = Mathf.SmoothDampAngle(_calculatedAngle, playerInputSpace.eulerAngles.y,
                        ref _currentAngleVelocity,
                        0.03f);
                    transform.rotation = Quaternion.Euler(0, _calculatedAngle, 0);
                    break;
            }
        }

        private void UpdateConnectionState()
        {
            if (_connectedBody == _previousConnectedBody)
            {
                var connectionMovement = _connectedBody.transform.TransformPoint(_connectionLocalPosition) -
                                         _connectionWorldPosition;
                _connectionVelocity = connectionMovement / Time.deltaTime;
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

        private void AdjustVelocity()
        {
            float acceleration, speed;
            Vector3 xAxis, zAxis;
            if (Climbing)
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
            }

            xAxis = ProjectDirectionOnPlane(xAxis, _contactNormal);
            zAxis = ProjectDirectionOnPlane(zAxis, _contactNormal);

            var relativeVelocity = _velocity - _connectionVelocity;
            var currentX = Vector3.Dot(relativeVelocity, xAxis);
            var currentZ = Vector3.Dot(relativeVelocity, zAxis);

            var maxSpeedChange = acceleration * Time.deltaTime;

            var newX = Mathf.MoveTowards(currentX, _playerInput.x * speed, maxSpeedChange);
            var newZ = Mathf.MoveTowards(currentZ, _playerInput.y * speed, maxSpeedChange);

            _velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
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
                if (_jumpPhase == 0) _jumpPhase = 1;

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
            if (alignedSpeed > 0f) jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);

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

        private static Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - _contactNormal * Vector3.Dot(vector, _contactNormal);
        }

        private float GetMinDot(int layer)
        {
            return (stairsMask & (1 << layer)) == 0 ? _minGroundDotProduct : _minStairsDotProduct;
        }

        #region DEBUG

#if DEBUG
        private void DebugInitialize()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Physics.simulationMode = SimulationMode.FixedUpdate;
            DevConsole.AddCommand(Command.Create<RotationType>(
                "rotationtype",
                "rotate",
                "Change rotation mode",
                Parameter.Create(
                    "mode",
                    "Rotation mode"
                ),
                mode => { rotationType = mode; }
            ));
            DevConsole.AddCommand(Command.Create(
                "physics",
                "phys",
                "none",
                () => { Physics.simulationMode = SimulationMode.FixedUpdate; }));
        }
#endif

        #endregion

        private enum RotationType
        {
            RotateWithVelocity,
            RotateWithCamera
        }
    }
}