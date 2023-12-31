using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using Logger = SEEP.Utils.Logger;

namespace SEEP.Network
{
    public class LobbyManager : NetworkBehaviour
    {
        #region PRIVATE

        /// <summary>
        /// SyncList of ClientsControllers
        /// </summary>
        [SyncObject] private readonly SyncList<ClientController> _clients = new SyncList<ClientController>();

        /// <summary>
        /// Array of cached clients. Caching on clients
        /// </summary>
        private ClientController[] _cachedClients;

        /// <summary>
        /// Required on client side. Prevents from multiple registration
        /// </summary>
        private bool _isInitialized;

        #endregion

        #region PUBLIC

        /// <summary>
        /// Readonly collection of ClientsControllers
        /// </summary>
        public IReadOnlyCollection<ClientController> Clients => _cachedClients;

        #endregion

        #region MONOBEHAVIOUR

        private void Start()
        {
            _cachedClients = Array.Empty<ClientController>();
            _clients.OnChange += OnClientsUpdate;
            if(IsServer)
                Initialize();
        }

        #endregion

        #region INITIALIZE

        /// <summary>
        /// Called on server side and subscribe on changes in remote connections
        /// </summary>
        [Server]
        private void Initialize()
        {
            FishNet.InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionChanged;
        }

        /// <summary>
        /// Called on client side and request server side of LobbyManager to registrate current client 
        /// </summary>
        [Client]
        private void InitializeClientSide()
        {
            CmdRegisterClient(ClientManager.Connection);
        }

        #endregion

        #region METHODS

        #endregion

        #region EVENTS

        /// <summary>
        /// Called when SyncList of clients are updated from server
        /// </summary>
        private void OnClientsUpdate(SyncListOperation op, int index, ClientController oldItem,
            ClientController newItem, bool asServer)
        {
            _cachedClients = _clients.ToArray();

            if (!asServer) return;

            switch (op)
            {
                case SyncListOperation.Add:
                    Logger.Log(this, $"User connected. Update list...");
                    break;
                case SyncListOperation.Insert:
                    break;
                case SyncListOperation.Set:
                    break;
                case SyncListOperation.RemoveAt:
                    Logger.Log(this, $"User disconnected. Update list...");
                    break;
                case SyncListOperation.Clear:
                    break;
                case SyncListOperation.Complete:
                    break;
            }
        }

        /// <summary>
        /// Called on server when new client connected or disconnected. On connecting will be called before client appear on server
        /// </summary>
        private void OnRemoteConnectionChanged(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case RemoteConnectionState.Stopped:
                    UpdateClientsList();
                    break;
                case RemoteConnectionState.Started:
                    break;
            }
        }

        #endregion

        #region NETWORK

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Started on client side. Calling initialize method to client side
        /// </summary>
        public override void OnStartClient()
        {
            if (_isInitialized) return;

            base.OnStartClient();
            _isInitialized = true;
            InitializeClientSide();
        }

        /// <summary>
        /// Server command for registrate new client, and synchronizing over network
        /// </summary>
        /// <param name="conn">Connection of new client</param>
        [ServerRpc(RequireOwnership = false)]
        private void CmdRegisterClient(NetworkConnection conn)
        {
            var newClient = FindClientByConnection(conn);
            if (newClient)
            {
                _clients.Add(newClient);
            }
            else
                Logger.Warning(this, "RegisterClient called, by client doesn't found.");
        }

        #endregion

        #region UTILS

        /// <summary>
        /// Trying to find GameObject and attached ClientController with specific owner
        /// </summary>
        /// <param name="conn">NetworkConnection of owner</param>
        /// <returns>Found ClientController</returns>
        private ClientController FindClientByConnection(NetworkConnection conn)
        {
            return FindObjectsByType<ClientController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(client => client.Owner == conn);
        }

        /// <summary>
        /// Calling on server side. Clean SyncList of clients from disconnected clients
        /// </summary>
        private void UpdateClientsList()
        {
            for (var i = 0; i < _clients.Count; i++)
            {
                if (_clients[i])
                    continue;
                _clients.RemoveAt(i);
            }
        }

        #endregion
    }
}