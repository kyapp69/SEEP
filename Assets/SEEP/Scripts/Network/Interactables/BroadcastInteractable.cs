using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;
using SEEP.Network.Controllers;
using SEEP.Offline;
using SEEP.Offline.Controllers;
using SEEP.Offline.Interfaces;
using UnityEngine;

namespace SEEP.Network.Interactables
{
    public abstract class BroadcastInteractable<T> : OfflineInteractable where T : struct, IBroadcast, IIdentifcable
    {
        [SerializeField] private bool forceState;
        [SerializeField] private bool applyToAllInstance;
        public bool ApplyToAllInstance { get; protected set; }

        public bool ForceState { get; protected set; }

        private T InternalServerState { get; set; }

        protected override void Awake()
        {
            ApplyToAllInstance = applyToAllInstance;
            ForceState = forceState;
            base.Awake();
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

        public override void Interact(InteractorController interactor)
        {
            var state = GetState();
            state.ID = ApplyToAllInstance ? -1 : gameObject.GetInstanceID();
            SendState(state);
        }

        protected abstract T GetState();

        protected abstract void SetState(T state);

        protected abstract void UpdateState();

        private static void SendState(T state)
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

        private void OnClientStateChanged(T state)
        {
            if (state.ID != -1 && state.ID != gameObject.GetInstanceID()) return;

            SetState(state);
            if (InstanceFinder.IsHost)
                InternalServerState = state;
        }

        private void OnServerStateChanged(NetworkConnection conn, T state)
        {
            InstanceFinder.ServerManager.Broadcast(conn.FirstObject, state);

            if (state.ID != -1 && state.ID != gameObject.GetInstanceID()) return;
            if (InstanceFinder.IsHost)
                InternalServerState = state;
        }
    }
}