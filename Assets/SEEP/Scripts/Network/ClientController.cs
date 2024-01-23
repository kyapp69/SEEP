using System.Linq;
using Cinemachine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using SEEP.Network.Controllers;
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
        private CinemachineFreeLook _camera;

        #endregion

        #region METHODS

        private void ChangeNickname(string newNickname)
        {
            if (IsClient && IsOwner && ClientManager.Connection.IsActive) CmdChangeNickname(newNickname);
        }

        private void RequestToSpawnObject(GameObject gameObject)
        {
            if (IsClient && IsOwner && ClientManager.Connection.IsActive)
                CmdSpawnObject(new Vector3(0, 1, 0), Quaternion.identity, Owner, gameObject);
        }

        #endregion

        #region EVENTS

        private void OnClientConnected()
        {
            ChangeNickname("test");
            _camera = FindObjectOfType<CinemachineFreeLook>();
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
            RpcRegisterCamera(conn);
        }

        [TargetRpc]
        private void RpcRegisterCamera(NetworkConnection conn)
        {
            var drones = FindObjectsByType<DroneController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var targetDrone = drones.First(x => x.Owner == Owner).transform.GetChild(0);
            _camera.LookAt = targetDrone;
            _camera.Follow = targetDrone;
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
            RequestToSpawnObject(dronePrefab);
        }
        
        public void ConsoleSpawnCube()
        {
            RequestToSpawnObject(cubePrefab);
        }

        #endregion

#endif
    }
}