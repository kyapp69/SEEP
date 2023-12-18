using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Logger = SEEP.Utils.Logger;

namespace SEEP.Network
{
    public class ClientController : NetworkBehaviour
    {
        #region PRIVATE VARIABLES

        [SyncVar(ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly,
            OnChange = nameof(OnChangeNickname), Channel = Channel.Reliable)]
        private string _nickname;

        private bool _isInitialized;
        #endregion

        #region PUBLIC VARIABLES

        public string Nickname
        {
            get { return _nickname; }
        }

        #endregion

        #region MONOBEHAVIOUR

        public override string ToString()
        {
            return $"Player (ID: {OwnerId}, Nick: {_nickname})";
        }

        #endregion

        #region METHODS

        private void ChangeNickname(string newNickname)
        {
            if (IsClient && IsOwner && ClientManager.Connection.IsActive)
            {
                CmdChangeNickname(newNickname);
            }
        }

        #endregion

        #region EVENTS

        private void OnClientConnected()
        {
            ChangeNickname("test");
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
            _nickname = newNickname;
        }

        #endregion

#if DEBUG

        #region DEV-COMMANDS

        public void ConsoleChangeNickname(string newNickname)
        {
            ChangeNickname(newNickname);
        }

        #endregion

#endif
    }
}