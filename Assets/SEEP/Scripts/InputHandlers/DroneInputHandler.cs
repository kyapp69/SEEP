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
        private InputAction _interactAction;

        private Vector2 _control;
        private bool _jump;
        private bool _isJumpHolding;
        private bool _interact;
        private bool _isInteractHolding;

        public Vector2 Control => _control;
        public bool Jump
        {
            get
            {
                if (_isJumpHolding)
                {
                    return false;
                }
                if (_jump && !_isJumpHolding)
                {
                    _isJumpHolding = true;
                }

                return _jump;
            }
        }

        public bool HoldedJump => _jump;
        public bool Interact
        {
            get
            {
                if (_isInteractHolding)
                {
                    return false;
                }
                if (_interact && !_isInteractHolding)
                {
                    _isInteractHolding = true;
                }

                return _interact;
            }
        }

        public bool HoldedInteract => _interact;

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
            _interactAction = _defaultMap.FindAction("Interact");

            _controlAction.performed += ControlAction;
            _controlAction.canceled += ControlAction;
            _jumpAction.performed += JumpAction;
            _jumpAction.canceled += JumpAction;
            _interactAction.performed += InteractAction;
            _interactAction.canceled += InteractAction;
        }

        private void InteractAction(InputAction.CallbackContext obj)
        {
            _interact = obj.ReadValueAsButton();
            if (!_interact)
                _isInteractHolding = false;
        }

        private void JumpAction(InputAction.CallbackContext obj)
        {
            _jump = obj.ReadValueAsButton();
            if (!_jump)
                _isJumpHolding = false;
        }

        private void ControlAction(InputAction.CallbackContext obj)
        {
            _control = obj.ReadValue<Vector2>();
        }
    }
}