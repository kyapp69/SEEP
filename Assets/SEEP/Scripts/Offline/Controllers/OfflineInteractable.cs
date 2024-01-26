using SEEP.Network.Controllers;
using SEEP.Offline.Interfaces;
using SEEP.Utils;
using UnityEngine;

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
            gameObject.layer = LayerMask.NameToLayer("Interactable");
            _message = message != "" ? message : "null";
            _type = interactableType;

            if (_type != InteractableType.Zone) return;

            _meshRenderer = GetComponent<MeshRenderer>();
            _meshRenderer.enabled = false;
#if DEBUG
            InstanceFinder.GameManager.OnTriggerChangeVisibility += ChangeTriggerVisibility;
#endif
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