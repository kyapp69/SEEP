using System;
using SEEP.Network;
using UnityEngine;

namespace SEEP.Utils
{
    public class InstanceFinder : MonoBehaviour
    {
        #region PRIVATE

        private static LobbyManager _lobbyManager;

        private static GameManager _gameManager;

        #endregion

        #region PUBLIC

        public static LobbyManager LobbyManager
        {
            get
            {
                if (_lobbyManager != null) return _lobbyManager;
                
                _lobbyManager = FindFirstObjectByType<LobbyManager>();
                if (_lobbyManager)
                    return _lobbyManager;
                Logger.Warning("InstanceFinder", "LobbyManager not founded on the scene");
                return null;
            }
        }
        
        public static GameManager GameManager
        {
            get
            {
                if (_gameManager != null) return _gameManager;
                
                _gameManager = FindFirstObjectByType<GameManager>();
                if (_gameManager)
                    return _gameManager;
                Logger.Warning("InstanceFinder", "GameManager not founded on the scene");
                return null;
            }
        }
        #endregion
    }
}