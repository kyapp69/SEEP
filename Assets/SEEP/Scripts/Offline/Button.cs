using System;
using UnityEngine;
using UnityEngine.Events;

namespace SEEP.Network.Controllers
{
    public class Button : MonoBehaviour, IInteractable
    {
        [SerializeField] private string message;
        [SerializeField] private UnityEvent callback;

        private string _message;

        private void Awake()
        {
            _message = message != "" ? message : "null";
        }

        public void Interact(InteractorController interactor)
        {
            callback?.Invoke();
        }

        public string GetMessage()
        {
            return _message;
        }
    }
}