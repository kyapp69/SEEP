using System;
using System.Linq;
using System.Net;
using DavidFDev.DevConsole;
using SEEP.Network;
using SEEP.Network.Controllers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEEP.Utils
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private UnityEvent onLocalPlayerSpawned;
        [SerializeField] private UnityEvent onLocalDroneSpawned;
        [SerializeField] private UnityEvent onLocalHostStarted;
        [SerializeField] private UnityEvent onLocalHostStopped;

        public ClientController LocalClient => _clientController;

        public DroneController LocalDrone => _droneController;

#if DEBUG
        public Action OnTriggerChangeVisibility;

        public bool TriggerVisible { get; private set; }

#endif

        private ClientController _clientController;

        private DroneController _droneController;

        private bool _isOnline;

        private void Start()
        {
#if DEBUG
            AddDevCommands();
#endif
            var networkController = InstanceFinder.NetworkController;
            if (networkController == null)
            {
                Logger.Log(LoggerChannel.GameManager, Priority.Warning, "NetworkController not found. GameManager will work in offline mode");
                _isOnline = false;
                return;
            }

            _isOnline = true;
            InstanceFinder.NetworkController.OnClientConnected += OnClientConnected;
            InstanceFinder.NetworkController.OnClientDisconnected += OnClientDisconnected;
            InstanceFinder.NetworkController.OnHostStarted += OnHostStarted;
            InstanceFinder.NetworkController.OnHostStopped += OnHostStopped;
        }

        private void OnHostStopped()
        {
            _clientController = null;
            _droneController = null;
            onLocalHostStopped.Invoke();
        }

        private void OnHostStarted()
        {
            onLocalHostStarted.Invoke();
        }

        private void OnClientConnected(string obj)
        {
        }

        private void OnClientDisconnected()
        {
            _clientController = null;
            _droneController = null;
        }

        public void RegisterDrone(DroneController droneController)
        {
            if (!CheckOnline()) return;
            
            if (droneController == null) return;
            _droneController = droneController;
            onLocalDroneSpawned.Invoke();
        }

        public void RegisterClient(ClientController clientController)
        {
            if (!CheckOnline()) return;

            if (clientController == null) return;
            _clientController = clientController;
            onLocalPlayerSpawned.Invoke();
        }

        public void ShowCursor(bool visible, CursorLockMode lockMode)
        {
            Cursor.lockState = lockMode;
            Cursor.visible = visible;
        }

        private bool CheckOnline()
        {
            if (!_isOnline)
                Logger.Log(LoggerChannel.GameManager, Priority.Warning, "Can't call network method, when GameManager is offline");
            return _isOnline;
        }

#if DEBUG
        public void ChangeTriggerVisible()
        {
            TriggerVisible = !TriggerVisible;
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
                callback: address => { FishNet.InstanceFinder.ClientManager.StartConnection(address); }));

            DevConsole.AddCommand(Command.Create(
                name: "server",
                aliases: "startserver",
                helpText: "Start server",
                callback: () => FishNet.InstanceFinder.ServerManager.StartConnection()
            ));
            DevConsole.AddCommand(Command.Create(
                name: "host",
                aliases: "hst",
                helpText: "Start server and connect locally",
                callback: () =>
                {
                    FishNet.InstanceFinder.ServerManager.StartConnection();
                    FishNet.InstanceFinder.ClientManager.StartConnection("localhost");
                }));
            DevConsole.AddCommand(Command.Create(
                name: "client",
                aliases: "cln",
                helpText: "Connect client locally",
                callback: () => { FishNet.InstanceFinder.ClientManager.StartConnection("localhost"); }));
            DevConsole.AddCommand(Command.Create(
                name: "triggers_show",
                aliases: "trg",
                helpText: "Switch visibility of triggers",
                callback: () => { OnTriggerChangeVisibility?.Invoke(); }));
        }
#endif
    }
}