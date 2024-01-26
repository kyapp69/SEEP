using FishNet.Object;
using SEEP.Network.Controllers;
using SEEP.Offline.Interfaces;
using SEEP.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEEP.Network.Interactables
{
    public sealed class NetworkButton : NetworkBehaviour, IInteractable
    {
        [SerializeField] private string message;
        [SerializeField] private UnityEvent callback;
        [SerializeField] private InteractableType interactableType;

        private string _message;
        private InteractableType _type;
        private MeshRenderer _meshRenderer;

        protected override void OnValidate()
        {
            base.OnValidate();
            gameObject.layer = LayerMask.NameToLayer("Interactable");
        }
        
        private void Awake()
        {
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

        public void Interact(InteractorController interactor)
        {
            CmdInvokeEvent();
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void CmdInvokeEvent()
        {
            callback?.Invoke();
            RpcInvokeEvent();
        }

        [ObserversRpc]
        private void RpcInvokeEvent()
        {
            if (!IsClientOnly) return;
            callback?.Invoke();
        }

#if DEBUG
        private void ChangeTriggerVisibility()
        {
            _meshRenderer.enabled = !_meshRenderer.enabled;
        }
#endif
    }
}