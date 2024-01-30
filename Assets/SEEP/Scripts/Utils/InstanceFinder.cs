using SEEP.Network;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SEEP.Utils
{
    public class InstanceFinder : MonoBehaviour
    {
        #region PRIVATE

        private static LobbyManager _lobbyManager;

        private static GameManager _gameManager;

        private static NetworkController _networkController;

        #endregion

        #region PUBLIC

        public static LobbyManager LobbyManager
        {
            get => FindObject(ref _lobbyManager);
            private set => LobbyManager = value;
        }
        public static GameManager GameManager
        {
            get => FindObject(ref _gameManager);
            private set => GameManager = value;
        }
        
        public static NetworkController NetworkController
        {
            get => FindObject(ref _networkController);
            private set => NetworkController = value;
        }

        #endregion
        
        private static T FindObject<T>(ref T outVar) where T : Object
        {
            if (outVar != null) return outVar;
                
            outVar = FindFirstObjectByType<T>();
            if (outVar)
                return outVar;
            Logger.Warning("InstanceFinder", $"{outVar.GetType()} not founded on the scene");
            return null;
        }
    }
}