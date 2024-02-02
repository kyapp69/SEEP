using System;
using System.Linq;
using Cinemachine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using SEEP.Network.Controllers;
using SEEP.Utils;
using UnityEngine;
using Channel = FishNet.Transporting.Channel;
using Logger = SEEP.Utils.Logger;

namespace SEEP.Network
{
    public class ClientController : NetworkBehaviour
    {
        [SerializeField] private GameObject dronePrefab;
        [SerializeField] private GameObject cubePrefab;

        #region PUBLIC VARIABLES

        [field: SyncVar(ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly,
            OnChange = nameof(OnChangeNickname), Channel = Channel.Reliable)]
        public string Nickname { get; private set; }

        [field: SyncVar(ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly,
            OnChange = nameof(OnChangeRole))]
        public ClientRole Role { get; private set; }

        public enum ClientRole
        {
            None,
            Hacker,
            Engineer
        }

        #endregion

        #region MONOBEHAVIOUR

        public override string ToString()
        {
            return $"Player (ID: {OwnerId}, Nick: {Nickname})";
        }

        #endregion

        #region PRIVATE VARIABLES

        private bool _isInitialized;
        private DroneController _droneController;

        #endregion

        #region METHODS

        private void ChangeNickname(string newNickname)
        {
            if (IsClient && IsOwner && ClientManager.Connection.IsActive) CmdChangeNickname(newNickname);
        }

        private void RequestToSpawnObject(GameObject gameObject, Vector3 pos)
        {
            if (IsClient && IsOwner && ClientManager.Connection.IsActive)
                CmdSpawnObject(pos, Quaternion.identity, Owner, gameObject);
        }

        #endregion

        #region EVENTS

        private void OnClientConnected()
        {
            CmdRegisterClient(Environment.UserName);
            //ChangeNickname(Environment.UserName);
            InstanceFinder.GameManager.RegisterClient(this);
        }

        private void OnChangeNickname(string prevName, string nextName, bool asServer)
        {
            if (!asServer) return;
            Logger.Log(LoggerChannel.ClientController, Priority.Error, $"{this} changed nick from {prevName} to {nextName}");
        }

        private void OnChangeRole(ClientRole prevRole, ClientRole newRole, bool asServer)
        {
            if (asServer)
            {
                if(newRole == ClientRole.Engineer)
                    RequestToSpawnObject(dronePrefab, new Vector3(0, 1, 0));
                if (newRole == ClientRole.Hacker)
                {
                    
                }
            }
            
            if (!asServer) return;
            Logger.Log(LoggerChannel.ClientController, Priority.Error, $"{this} changed role from {prevRole} to {newRole}");
        }

        #endregion

        #region NETWORK

        // ReSharper disable Unity.PerformanceAnalysis
        public override void OnStartClient()
        {
            if (!IsOwner || _isInitialized) return;

            base.OnStartClient();
            _isInitialized = true;
            OnClientConnected();
        }

        [ServerRpc]
        private void CmdChangeNickname(string newNickname)
        {
            Nickname = newNickname;
        }

        [ServerRpc]
        private void CmdRegisterClient(string nickname)
        {
            CmdChangeNickname(nickname);
            InstanceFinder.LobbyManager.RegisterPlayer(this);
        }

        [Server]
        public void SetRole(ClientRole newRole)
        {
            Role = newRole;
        }

        [ServerRpc]
        private void CmdSpawnObject(Vector3 pos, Quaternion rot, NetworkConnection conn, GameObject gameObject)
        {
            var clone = Instantiate(gameObject, pos, rot);
            ServerManager.Spawn(clone, conn);
            if (gameObject == dronePrefab)
                RpcRegisterDrone(conn, clone);
        }

        [TargetRpc]
        private void RpcRegisterDrone(NetworkConnection conn, GameObject drone)
        {
            _droneController = drone.GetComponent<DroneController>();
            InstanceFinder.GameManager.RegisterDrone(_droneController);
        }

        #endregion

#if DEBUG

        #region DEV-COMMANDS

        public void ConsoleChangeNickname(string newNickname)
        {
            ChangeNickname(newNickname);
        }

        public void ConsoleSpawnDrone()
        {
            Logger.Log(LoggerChannel.ClientController, Priority.Warning,"Currently ConsoleSpawnDrone not used!");
            //RequestToSpawnObject(dronePrefab, new Vector3(0, 1, 0));
        }

        public void ConsoleSpawnCube()
        {
            if (_droneController == null) return;
            var position = _droneController.transform.position;
            RequestToSpawnObject(cubePrefab, new Vector3(position.x, position.y + 3, position.z));
        }

        #endregion

#endif
    }
}