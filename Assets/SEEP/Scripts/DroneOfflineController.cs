using System;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using SEEP.VehicleController;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Logger = SEEP.Utils.Logger;

namespace SEEP
{
    public class DroneOfflineController : MonoBehaviour
    {
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float moveSpeed = 15f;
        [SerializeField] private float maxSpeed = 8f;
        [SerializeField] private float rotationSmoothTime = 5f;
        [FormerlySerializedAs("sidewaysDragMultiplier")] [SerializeField] private float sidewaysDragReduceMultiplier = 1f;
        [SerializeField] private TextMeshProUGUI speedText;

        #region PRIVATE

        private VehicleInput _input;
        private Rigidbody _rigidbody;
        private Vector3 _movement;
        private bool _jump;
        private float _distToGround;
        private float _calculatedAngle;
        private float _currentAngleVelocity;
        private Transform _mainCameraTransform;

        #endregion

        #region MONOBEHAVIOUR

        void OnScriptHotReload()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.position = new Vector3(0, 1, 0);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _input = GetComponent<VehicleInput>();
            _distToGround = GetComponent<CapsuleCollider>().bounds.extents.y;
            _mainCameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            _movement = new Vector3(_input.Control.x, 0, _input.Control.y);
            if (!_jump && _input.Jump && IsGrounded())
                _jump = true;

            speedText.text = $"{_rigidbody.velocity.magnitude:f2}";
            //_forwardMovement = orientationObject.forward * _input.Throttle + orientationObject.right * _input.Steering;
        }

        private bool IsGrounded()
        {
            var ray = new Ray(transform.position, -Vector3.up);
            return Physics.Raycast(ray, _distToGround + 0.1f);
        }

        private void FixedUpdate()
        {
            //If we has a input and we grounded - apply our movement forces
            if (_movement.magnitude >= 0.1f && IsGrounded())
            {
                //Calculate target angle to our movement
                var targetAngle = Mathf.Atan2(_movement.x, _movement.z) * Mathf.Rad2Deg +
                                  _mainCameraTransform.eulerAngles.y;
                
                //Calculate smoothed angle from current to target
                _calculatedAngle = Mathf.SmoothDampAngle(_calculatedAngle, targetAngle, ref _currentAngleVelocity,
                    rotationSmoothTime);
                
                //Apply calculated angle
                transform.rotation = Quaternion.Euler(0, _calculatedAngle, 0);

                //Calculate assigned vector to our smoothed angle
                var rotatedMovement = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
                
                //And apply him
                _rigidbody.AddForce(rotatedMovement * (moveSpeed), ForceMode.Force);
                
                //Get velocity of sideways drag
                var velocity = transform.InverseTransformDirection(_rigidbody.velocity);
                
                //Apply inverted force of sideways drag according to multiplier
                _rigidbody.AddForce(transform.right * (-velocity.x * sidewaysDragReduceMultiplier));
            }

            //If jump requested
            if (_jump)
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

        #endregion
    }
}