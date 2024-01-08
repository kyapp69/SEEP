using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = SEEP.Utils.Logger;

namespace SEEP.InputHandlers
{
    [RequireComponent(typeof(PlayerInput))]
    [DisallowMultipleComponent]
    public class SpiderInputHandler : MonoBehaviour
    {
        private PlayerInput _input;
        private InputActionMap _inputMap;
        private InputAction _movementAction;

        private Vector2 _movement;

        public Vector2 Movement => _movement;

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
            _movementAction = _inputMap.FindAction("Movement");
            
            _movementAction.performed += MovementAction;
            _movementAction.canceled += MovementAction;
        }

        private void MovementAction(InputAction.CallbackContext obj)
        {
            _movement = obj.ReadValue<Vector2>();
        }
    }
}