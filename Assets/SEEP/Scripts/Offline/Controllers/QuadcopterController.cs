using SEEP.InputHandlers;
using UnityEngine;

namespace SEEP.Offline.Controllers
{
    public class QuadcopterController : MonoBehaviour
    {
        private QuadcopterInputHandler _inputHandler;
        private Rigidbody _rigidbody;
        private Transform _mainCameraTransform;
        private float _calculatedAngle;
        private float _currentAngleVelocity;

        [SerializeField] private float speed = 5f;

        private void Start()
        {
            _inputHandler = GetComponent<QuadcopterInputHandler>();
            _rigidbody = GetComponent<Rigidbody>();
            _mainCameraTransform = Camera.main.transform;
        }

        void OnScriptHotReload()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.position = new Vector3(0, 1, 0);
        }

        private void FixedUpdate()
        {
            Vector3 upwardForce = Vector3.up * (_rigidbody.mass * -Physics.gravity.y);
            _rigidbody.AddForce(upwardForce);

            Vector3 movement = transform.rotation * new Vector3(_inputHandler.Movement.x, 0.0f, _inputHandler.Movement.y);

            _rigidbody.AddForce(movement * speed);


            Vector3 sidewayAxis = Vector3.Cross(_rigidbody.velocity, Vector3.up);
            
            //Get velocity of sideways drag
            var velocity = transform.rotation * _rigidbody.velocity;

            //Apply inverted force of sideways drag according to multiplier
            _rigidbody.AddForce(sidewayAxis * ( -velocity.x * 15f));

            if (_inputHandler.Movement.magnitude == 0f)
                _rigidbody.AddForce(_rigidbody.velocity * -15f);

            _calculatedAngle = Mathf.SmoothDampAngle(_calculatedAngle, _mainCameraTransform.eulerAngles.y,
                ref _currentAngleVelocity, 0.15f);

            transform.rotation = Quaternion.Euler(0, _calculatedAngle, 0);

            /*var targetAngle = Mathf.Atan2(_inputHandler.Control.x, _inputHandler.Control.y) * Mathf.Rad2Deg +
                              _mainCameraTransform.eulerAngles.y;

            //Calculate smoothed angle from current to target
            _calculatedAngle = Mathf.SmoothDampAngle(_calculatedAngle, targetAngle, ref _currentAngleVelocity, 0.15f);

            //Apply calculated angle
            transform.rotation = Quaternion.Euler(0, _calculatedAngle, 0);

            //Calculate assigned vector to our smoothed angle
            var rotatedMovement = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;*/
        }
    }
}