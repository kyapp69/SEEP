using UnityEngine;
using UnityEngine.InputSystem;
using Logger = SEEP.Utils.Logger;

namespace SEEP.InputHandlers
{
    [RequireComponent(typeof(PlayerInput))]
    [DisallowMultipleComponent]
    public class DroneInputHandler : MonoBehaviour
    {
        private PlayerInput _input;
        private InputActionMap _defaultMap;
        private InputAction _controlAction;
        private InputAction _jumpAction;

        private Vector2 _control;
        private bool _jump;

        public Vector2 Control => _control;
        public bool Jump => _jump;

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
            _controlAction = _defaultMap.FindAction("Control");
            _jumpAction = _defaultMap.FindAction("Jump");
            
            _controlAction.performed += ControlAction;
            _controlAction.canceled += ControlAction;
            _jumpAction.performed += JumpAction;
            _jumpAction.canceled += JumpAction;
        }

        private void JumpAction(InputAction.CallbackContext obj)
        {
            _jump = obj.ReadValueAsButton();
        }

        private void ControlAction(InputAction.CallbackContext obj)
        {
            _control = obj.ReadValue<Vector2>();
        }
    }
}