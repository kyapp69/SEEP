using System;
using SEEP.Network;
using UnityEngine;

namespace SEEP.Utils
{
    public class InstanceFinder : MonoBehaviour
    {
        #region PRIVATE

        private static LobbyManager _lobbyManager;

        #endregion

        #region PUBLIC

        public static LobbyManager LobbyManager
        {
            get
            {
                if (_lobbyManager != null) return _lobbyManager;
                
                _lobbyManager = FindObjectOfType<LobbyManager>();
                if (_lobbyManager)
                    return _lobbyManager;
                Logger.Error("InstanceFinder", "LobbyManager not founded on the scene");
                throw new NullReferenceException("LobbyManager not founded on the scene");
            }
        }
        #endregion
    }
}