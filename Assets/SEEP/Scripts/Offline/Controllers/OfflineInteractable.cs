using System;
using SEEP.Network.Controllers;
using SEEP.Offline.Interfaces;
using SEEP.Utils;
using UnityEngine;
using Logger = SEEP.Utils.Logger;

namespace SEEP.Offline.Controllers
{
    public abstract class OfflineInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string message;
        [SerializeField] private InteractableType interactableType;

        private string _message;
        private InteractableType _type;
        private MeshRenderer _meshRenderer;

        protected virtual void Awake()
        {
            _type = interactableType;
            _message = message != "" ? message : "null";
            switch (_type)
            {
                case InteractableType.Object:
                    gameObject.layer = LayerMask.NameToLayer("Interactable");
                    break;
                case InteractableType.Lift:
                    gameObject.layer = LayerMask.NameToLayer("Default");
                    break;
                case InteractableType.Zone:
                    gameObject.layer = LayerMask.NameToLayer("Interactable");
                    _meshRenderer = GetComponent<MeshRenderer>();
                    _meshRenderer.enabled = false;
#if DEBUG
                    InstanceFinder.GameManager.OnTriggerChangeVisibility += ChangeTriggerVisibility;
#endif
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public InteractableType GetInteractableType() => _type;

        public string GetMessage() => _message;

        public abstract void Interact(InteractorController interactor);

#if DEBUG
        private void ChangeTriggerVisibility()
        {
            _meshRenderer.enabled = !_meshRenderer.enabled;
        }
#endif
    }
}