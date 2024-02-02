using System;
using Cinemachine;
using DavidFDev.DevConsole;
using UnityEngine;

namespace SEEP.Utils
{
    [RequireComponent(typeof(CinemachineInputProvider))]
    public class CinemachineUtils : MonoBehaviour
    {
        private CinemachineInputProvider _inputProvider;
        private CinemachineVirtualCameraBase _camera;

        private bool _isInitialized;

        private void Awake()
        {
            _inputProvider = GetComponent<CinemachineInputProvider>();
            if (!TryGetComponent(out _camera))
            {
                Logger.Log(LoggerChannel.CameraManager, Priority.Error, $"Can't initialize {GetType()}, because on {gameObject.name} can't find any " +
                                                                  $"component inherited from CinemachineVirtualCameraBase. Maybe you forgot?");
                _isInitialized = false;
                return;
            }
            
            //if(InstanceFinder)

            _isInitialized = true;
        }

        private void Update()
        {
        }

        public void OnDroneSpawned()
        {
            _camera.enabled = true;
            var visualChild = InstanceFinder.GameManager.LocalDrone.transform.GetChild(0);
            _camera.Follow = visualChild;
            _camera.LookAt = visualChild;
        }
    }

    public enum CameraType
    {
        None,
        DroneCamera,
        HackerCamera,
        MonitorCamera
    }
}