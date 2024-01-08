using UnityEngine;
using UnityEngine.InputSystem;
using Logger = SEEP.Utils.Logger;

namespace SEEP.InputHandlers
{
    [RequireComponent(typeof(PlayerInput))]
    [DisallowMultipleComponent]
    public class QuadcopterInputHandler : MonoBehaviour
    {
        private PlayerInput _input;
        private InputActionMap _inputMap;
        private InputAction _controlAction;
        private InputAction _heightAction;
        private InputAction _yawAction;

        private Vector2 _movement;
        private float _height;
        private float _yaw;

        public Vector2 Movement => _movement;
        public float Height => _height;
        public float Yaw => _yaw;

        private void Start()
        {
            _input = GetComponent<PlayerInput>();
            
            if (_input.currentActionMap == null)
            {
                Logger.Warning(this,
                    "Action map doesn't assigned to player input. Vehicle input initialization will be stopped");
                return;
            }

            _inputMap = _input.currentActionMap;
            _controlAction = _inputMap.FindAction("Movement");
            _heightAction = _inputMap.FindAction("HeightControl");
            _yawAction = _inputMap.FindAction("Yaw");
            
            _controlAction.performed += ControlAction;
            _controlAction.canceled += ControlAction;
            _heightAction.performed += HeightAction;
            _heightAction.canceled += HeightAction;
            _yawAction.performed += YawAction;
            _yawAction.canceled += YawAction;
        }

        private void YawAction(InputAction.CallbackContext obj)
        {
            _yaw = obj.ReadValue<float>();
        }

        private void HeightAction(InputAction.CallbackContext obj)
        {
            _height = obj.ReadValue<float>();
        }

        private void ControlAction(InputAction.CallbackContext obj)
        {
            _movement = obj.ReadValue<Vector2>();
        }
    }
}

