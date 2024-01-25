using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;
using SEEP.Network.Controllers;
using SEEP.Offline;
using SEEP.Offline.Interfaces;
using UnityEngine;

namespace SEEP.Network
{
    public abstract class NetworkedInteractiveObject<T> : MonoBehaviour, IInteractable
        where T : struct, IBroadcast, IIdentifcable
    {
        [SerializeField] private string message;

        private string _message;
        private T _internalServerState;
        private bool _applyToAllInstance;
        private bool _forceState;

        public bool ApplyToAllInstance
        {
            get => _applyToAllInstance;
            protected set => _applyToAllInstance = value;
        }

        public bool ForceState
        {
            get => _forceState;
            protected set => _forceState = value;
        }

        private T InternalServerState
        {
            get => _internalServerState;
            set => _internalServerState = value;
        }

        protected virtual void Awake()
        {
            ApplyToAllInstance = false;
            ForceState = true;
            _message = message != "" ? message : "null";
        }

        private void OnEnable()
        {
            InstanceFinder.ClientManager.RegisterBroadcast<T>(OnClientStateChanged);
            InstanceFinder.ServerManager.RegisterBroadcast<T>(OnServerStateChanged);
            InstanceFinder.ClientManager.OnRemoteConnectionState += args =>
            {
                if (!InstanceFinder.IsHost) return;
                if (ForceState && args.ConnectionState == RemoteConnectionState.Started) return;

                SendState(InternalServerState);
            };
        }

        private void OnDisable()
        {
            InstanceFinder.ClientManager.UnregisterBroadcast<T>(OnClientStateChanged);
            InstanceFinder.ServerManager.UnregisterBroadcast<T>(OnServerStateChanged);
        }

        public string GetMessage() => _message;

        public virtual void Interact(InteractorController interactor)
        {
            var state = GetState();
            state.ID = ApplyToAllInstance ? -1 : gameObject.GetInstanceID();
            SendState(state);
        }

        private void ForceSendState(NetworkConnection target)
        {
            var state = GetState();
            state.ID = ApplyToAllInstance ? -1 : gameObject.GetInstanceID();
            InstanceFinder.ClientManager.Broadcast(state);
        }

        protected abstract T GetState();

        protected abstract void SetState(T state);

        protected abstract void UpdateState();

        protected virtual void SendState(T state)
        {
            if (InstanceFinder.IsServer)
            {
                InstanceFinder.ServerManager.Broadcast(state);
            }
            else if (InstanceFinder.IsClient)
            {
                InstanceFinder.ClientManager.Broadcast(state);
            }
        }

        protected virtual void OnClientStateChanged(T state)
        {
            if (state.ID == -1 || state.ID == gameObject.GetInstanceID())
            {
                Debug.Log($"OnClient only client: {InstanceFinder.IsClientOnly}. Host: {InstanceFinder.IsHost}");
                SetState(state);
                if (InstanceFinder.IsHost)
                    InternalServerState = state;
            }
        }

        protected virtual void OnServerStateChanged(NetworkConnection conn, T state)
        {
            if (state.ID == -1 || state.ID == gameObject.GetInstanceID())
            {
                if (InstanceFinder.IsHost)
                    InternalServerState = state;
            }
            InstanceFinder.ServerManager.Broadcast(conn.FirstObject, state);
        }
    }
}