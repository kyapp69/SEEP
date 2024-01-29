using System;
using Cinemachine;
using DavidFDev.DevConsole;
using UnityEngine;

namespace SEEP.Utils
{
    public class CinemachineUtils : MonoBehaviour
    {
        private CinemachineInputProvider _inputProvider;
        private CinemachineFreeLook _camera;

        private void Awake()
        {
            _inputProvider = GetComponent<CinemachineInputProvider>();
            _camera = GetComponent<CinemachineFreeLook>();
            InstanceFinder.GameManager.OnLocalDroneSpawned += () =>
            {
                _camera.enabled = true;
                var visualChild = InstanceFinder.GameManager.LocalDrone.transform.GetChild(0);
                _camera.Follow = visualChild;
                _camera.LookAt = visualChild;
            };
            InstanceFinder.GameManager.OnClientStopped += () =>
            {
                if (_camera == null) return;
                _camera.enabled = false;
                _camera.Follow = null;
                _camera.LookAt = null;
            };
        }

        private void Update()
        {
            var inputEnabled = _inputProvider.enabled;
            _inputProvider.enabled = DevConsole.IsOpen switch
            {
                true when inputEnabled => false,
                false when !inputEnabled => true,
                _ => inputEnabled
            };
        }
    }
}