using UnityEngine;
using UnityEngine.InputSystem;
using Logger = SEEP.Utils.Logger;

namespace SEEP.InputHandlers
{
    [RequireComponent(typeof(PlayerInput))]
    public class CameraInputHandler : MonoBehaviour
    {
        private PlayerInput _input;
        private InputActionMap _defaultMap;
        private InputAction _lookAction;

        private Vector2 _look;

        public Vector2 Look => _look;

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
            _lookAction = _defaultMap.FindAction("Look");

            _lookAction.performed += LookAction;
            _lookAction.canceled += LookAction;
        }

        private void LookAction(InputAction.CallbackContext obj)
        {
            _look = obj.ReadValue<Vector2>();
        }
    }
}