using System;
using System.Collections.Generic;
using System.Net;
using FishNet.Transporting;
using SEEP.Utils;
using UnityEngine;
using UnityEngine.Events;
using Logger = SEEP.Utils.Logger;

namespace SEEP.Network
{
    public class NetworkController : MonoBehaviour
    {
        public UnityEvent OnFoundNewServer { get; private set; }
        public event Action<string> OnClientConnected;
        public event Action OnClientDisconnected;
        public event Action OnHostStarted;
        public event Action OnHostStopped;

        private NetworkDiscovery _networkDiscovery;

        private bool _isInitialized;

        public bool IsHostActive => FishNet.InstanceFinder.IsHost;

        public bool IsClientActive => FishNet.InstanceFinder.IsClientOnly;

        public List<string> FoundedServers => _isInitialized ? _networkDiscovery.FoundedServers : new List<string>();

        private void Awake()
        {
            if (FishNet.InstanceFinder.NetworkManager)
            {
                if (FishNet.InstanceFinder.NetworkManager.TryGetComponent(out _networkDiscovery))
                {
                    _isInitialized = true;
                }
                else
                {
                    _isInitialized = false;
                    Logger.Log(LoggerChannel.NetworkManager, Priority.Error, "NetworkDiscovery doesn't find. Exit...");
                }
            }
            else
            {
                _isInitialized = false;
                Logger.Log(LoggerChannel.NetworkManager, Priority.Error, "NetworkManager doesn't find. Exit...");
            }

            if (!_isInitialized) return;

            OnFoundNewServer = new UnityEvent();
        }

        private void Start()
        {
            if (!_isInitialized) return;

            FishNet.InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionChanged;
            FishNet.InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionChanged;
            _networkDiscovery.OnFoundNewServer += () => OnFoundNewServer.Invoke();
            _networkDiscovery.SearchForServers();
        }


        public void SwitchHost(bool enableHost)
        {
            if (!_isInitialized)
            {
                Logger.Log(LoggerChannel.NetworkManager, Priority.Warning,
                    "Can't call SwitchHost, because NetworkController doesn't initialized");
                return;
            }

            Logger.Log(LoggerChannel.NetworkManager, Priority.Info, $"Called Switch Host to {enableHost}");

            switch (enableHost)
            {
                case true when !IsHostActive:
                    _networkDiscovery.StopSearchingOrAdvertising();
                    FishNet.InstanceFinder.ServerManager.StartConnection();
                    FishNet.InstanceFinder.ClientManager.StartConnection("localhost");
                    OnHostStarted?.Invoke();
                    _networkDiscovery.AdvertiseServer();
                    break;
                case false when IsHostActive:
                    _networkDiscovery.StopSearchingOrAdvertising();
                    FishNet.InstanceFinder.ClientManager.StopConnection();
                    FishNet.InstanceFinder.ServerManager.StopConnection(true);
                    OnHostStopped?.Invoke();
                    _networkDiscovery.SearchForServers();
                    break;
            }
        }

        public void SwitchClient(bool enableClient, string address = "")
        {
            if (!_isInitialized)
            {
                Logger.Log(LoggerChannel.NetworkManager, Priority.Warning,
                    "Can't call SwitchClient, because NetworkController doesn't initialized");
                return;
            }

            Logger.Log(LoggerChannel.NetworkManager, Priority.Info,
                enableClient ? $"Called Switch Client to true. Server ip: {address}" : "Called Switch Client to false");

            switch (enableClient)
            {
                case true when !IsClientActive:
                    if (!IPAddress.TryParse(address, out _)) return;
                    _networkDiscovery.StopSearchingOrAdvertising();
                    FishNet.InstanceFinder.ClientManager.StartConnection(address);
                    break;
                case false when IsClientActive:
                    _networkDiscovery.StopSearchingOrAdvertising();
                    FishNet.InstanceFinder.ClientManager.StopConnection();
                    _networkDiscovery.SearchForServers();
                    break;
            }
        }

        private void OnServerConnectionChanged(ServerConnectionStateArgs obj)
        {
            /*if (obj.ConnectionState == LocalConnectionState.Stopping)
            {
                OnHostStopped?.Invoke();
            }*/
        }

        private void OnClientConnectionChanged(ClientConnectionStateArgs obj)
        {
            switch (obj.ConnectionState)
            {
                case LocalConnectionState.Started when FishNet.InstanceFinder.ServerManager.Started:
                    OnHostStarted?.Invoke();
                    break;
                case LocalConnectionState.Started:
                    OnClientConnected?.Invoke(FishNet.InstanceFinder.ClientManager.Connection.GetAddress());
                    break;
                case LocalConnectionState.Stopping when FishNet.InstanceFinder.ServerManager.Started:
                    OnHostStopped?.Invoke();
                    break;
                case LocalConnectionState.Stopping:
                    OnClientDisconnected?.Invoke();
                    break;
            }
        }
    }
}