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
            ChangeNickname(Environment.UserName);
            InstanceFinder.GameManager.RegisterClient(this);
        }

        private void OnChangeNickname(string prevName, string nextName, bool asServer)
        {
            if (!asServer) return;
            Logger.Log(this, $"{this} changed nick from {prevName} to {nextName}");
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
            RequestToSpawnObject(dronePrefab, new Vector3(0, 1, 0));
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