using FishNet.Object;
using SEEP.Network.Controllers;
using SEEP.Offline;
using UnityEngine;
using UnityEngine.Events;

namespace SEEP.Network
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