using System;
using Cinemachine;
using UnityEngine;

namespace SEEP.Utils
{
    public class CameraManager : MonoBehaviour
    {
        private CinemachineVirtualCamera _hackerCamera;
        private CinemachineFreeLook _droneCamera;
        private CinemachineVirtualCamera _monitorCamera;
        
        private void Awake()
        {
            var cinemachineCameras = FindObjectsByType<CinemachineVirtualCameraBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }
    }
}