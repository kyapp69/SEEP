using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DavidFDev.DevConsole;
using FishNet.Transporting;
using SEEP.Network;
using SEEP.Network.Controllers;
using SEEP.Utils;
using UnityEngine;
using InstanceFinder = FishNet.InstanceFinder;

namespace SEEP
{
    public class GameManager : MonoBehaviour
    {
        public bool IsHostActive => InstanceFinder.IsHost;

        public bool IsClientActive => InstanceFinder.IsClientOnly;

        public delegate void GameManagerEvent();

        public event GameManagerEvent OnClientConnected;

        public event GameManagerEvent OnClientStopped;

        public event GameManagerEvent OnHostConnected;

        public event GameManagerEvent OnHostStopped;

        public event GameManagerEvent OnLocalPlayerSpawned;

        public event GameManagerEvent OnLocalDroneSpawned;

        public event GameManagerEvent OnNewFoundedServer;

        public ClientController LocalClient => _clientController;

        public DroneController LocalDrone => _droneController;

#if DEBUG
        public event GameManagerEvent OnTriggerChangeVisibility;

        public bool TriggerVisible => _triggerVisible;

        public List<IPEndPoint> FoundedServers => _networkDiscovery.FoundedServer;

        private bool _triggerVisible;
#endif

        private ClientController _clientController;

        private DroneController _droneController;

        private NetworkDiscovery _networkDiscovery;

        private void Start()
        {
#if DEBUG
            AddDevCommands();
#endif
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionChanged;
            InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionChanged;
            _networkDiscovery = InstanceFinder.NetworkManager.GetComponent<NetworkDiscovery>();
            _networkDiscovery.ServerFoundCallback += OnFoundServer;
            _networkDiscovery.SearchForServers();
        }

        private void OnFoundServer(IPEndPoint obj)
        {
            OnNewFoundedServer?.Invoke();
        }

        private void OnServerConnectionChanged(ServerConnectionStateArgs obj)
        {
            if (obj.ConnectionState == LocalConnectionState.Stopping)
            {
                _clientController = null;
                _droneController = null;
                OnHostStopped?.Invoke();
            }
        }

        private void OnClientConnectionChanged(ClientConnectionStateArgs obj)
        {
            switch (obj.ConnectionState)
            {
                case LocalConnectionState.Started when InstanceFinder.ServerManager.Started:
                    OnHostConnected?.Invoke();
                    break;
                case LocalConnectionState.Started:
                    OnClientConnected?.Invoke();
                    break;
                case LocalConnectionState.Stopping:
                    OnClientStopped?.Invoke();
                    _clientController = null;
                    _droneController = null;
                    break;
            }
        }
        
        public void SwitchHost(bool enableHost)
        {
            switch (enableHost)
            {
                case true when !IsHostActive:
                    _networkDiscovery.StopSearchingOrAdvertising();
                    InstanceFinder.ServerManager.StartConnection();
                    InstanceFinder.ClientManager.StartConnection("localhost");
                    _networkDiscovery.AdvertiseServer();
                    break;
                case false when IsHostActive:
                    _networkDiscovery.StopSearchingOrAdvertising();
                    InstanceFinder.ClientManager.StopConnection();
                    InstanceFinder.ServerManager.StopConnection(true);
                    _networkDiscovery.SearchForServers();
                    break;
            }
        }

        public void SwitchClient(bool enableClient, string address = "")
        {
            switch (enableClient)
            {
                case true when !IsClientActive:

                    if (address.Trim() == "") return;
                    _networkDiscovery.StopSearchingOrAdvertising();
                    InstanceFinder.ClientManager.StartConnection(address);
                    break;
                case false when IsClientActive:
                    _networkDiscovery.StopSearchingOrAdvertising();
                    InstanceFinder.ClientManager.StopConnection();
                    _networkDiscovery.SearchForServers();
                    break;
            }
        }

        public void RegisterPlayer(ClientController clientController)
        {
            if (clientController == null) return;
            _clientController = clientController;
            OnLocalPlayerSpawned?.Invoke();
        }

        public void RegisterDrone(DroneController droneController)
        {
            if (droneController == null) return;
            _droneController = droneController;
            OnLocalDroneSpawned?.Invoke();
        }

#if DEBUG
        public void ChangeTriggerVisible()
        {
            _triggerVisible = !_triggerVisible;
            OnTriggerChangeVisibility?.Invoke();
        }
        
        private void AddDevCommands()
        {
            DevConsole.AddCommand(Command.Create<string>(
                name: "connect",
                helpText: "Connect to server",
                aliases: "server",
                p1: Parameter.Create(
                    name: "ip",
                    helpText: "IP address"
                ),
                callback: address => { InstanceFinder.ClientManager.StartConnection(address); }));

            DevConsole.AddCommand(Command.Create(
                name: "server",
                aliases: "startserver",
                helpText: "Start server",
                callback: () => InstanceFinder.ServerManager.StartConnection()
            ));
            DevConsole.AddCommand(Command.Create(
                name: "host",
                aliases: "hst",
                helpText: "Start server and connect locally",
                callback: () =>
                {
                    InstanceFinder.ServerManager.StartConnection();
                    InstanceFinder.ClientManager.StartConnection("localhost");
                }));
            DevConsole.AddCommand(Command.Create(
                name: "client",
                aliases: "cln",
                helpText: "Connect client locally",
                callback: () => { InstanceFinder.ClientManager.StartConnection("localhost"); }));
            DevConsole.AddCommand(Command.Create(
                name: "triggers_show",
                aliases: "trg",
                helpText: "Switch visibility of triggers",
                callback: () => { OnTriggerChangeVisibility?.Invoke(); }));
        }
#endif
    }
}