using UnityEngine;
using UnityEngine.InputSystem;
using Logger = SEEP.Utils.Logger;

namespace SEEP.VehicleController
{
    [RequireComponent(typeof(PlayerInput))]
    [DisallowMultipleComponent]
    public class VehicleInput : MonoBehaviour
    {
        private PlayerInput _input;
        private InputActionMap _defaultMap;
        private InputAction _throttleAction;
        private InputAction _steeringAction;

        private float _throttle;
        private float _steering;

        private void Start()
        {
            _input = GetComponent<PlayerInput>();

            if (_input.currentActionMap == null)
            {
                Logger.Warning(this,
                    "Action map doesn't assigned to player input. Vehicle input initialization will be stopped");
                return;
            }

            _defaultMap = _input.currentActionMap;
            _throttleAction = _defaultMap.FindAction("Throttle");
            _steeringAction = _defaultMap.FindAction("Steering");

            _throttleAction.performed += ThrottleAction;
            _throttleAction.canceled += ThrottleAction;
            _steeringAction.performed += SteeringAction;
            _steeringAction.canceled += SteeringAction;
        }

        private void ThrottleAction(InputAction.CallbackContext obj)
        {
            _throttle = obj.ReadValue<float>();
        }

        private void SteeringAction(InputAction.CallbackContext obj)
        {
            _steering = obj.ReadValue<float>();
        }
    }
}