using FishNet.Object;
using UnityEngine;
using UnityEngine.Events;
using Logger = SEEP.Utils.Logger;

namespace SEEP.Network.Controllers
{
    public class NetworkTrigger : NetworkBehaviour, IInteractable
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
            CmdInvokeEvent();
        }

        public string GetMessage()
        {
            return _message;
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
    }
}