using System;
using Cinemachine;
using FishNet.Object;
using SEEP.UI;
using SEEP.Utils;
using UnityEngine;

namespace SEEP.Offline.Controllers
{
    [RequireComponent(typeof(HackerUI))]
    public class HackerController : MonoBehaviour
    {
        [SerializeField] private float interactableDistance = 5f;
        [SerializeField] private CinemachineVirtualCamera hackerCamera;

        private Camera _physicalCamera;
        private CinemachineVirtualCamera _monitorCamera;

        private HackerUI _uiController;
        private HackerMonitorUI _hackerMonitorUI;

        private bool _inMonitor;

        private void Awake()
        {
            _physicalCamera = Camera.main;
            _uiController = GetComponent<HackerUI>();
        }

        private void Update()
        {
            if (!_inMonitor)
            {
                // ReSharper disable PossibleLossOfFraction
                var cameraRay = _physicalCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
                if (!Physics.Raycast(cameraRay, out var hit, interactableDistance)) return;

                //Check for monitor
                if (!hit.transform.TryGetComponent(out _hackerMonitorUI))
                {
                    _uiController.SwitchText(false);
                    return;
                }
            }

            _uiController.SwitchText(!_inMonitor);

            if (!Input.GetKeyDown(KeyCode.E)) return;
            
            if (!_inMonitor)
            {
                _monitorCamera = _hackerMonitorUI.transform.GetChild(0).GetComponent<CinemachineVirtualCamera>();
                _monitorCamera.Priority = 10;
                hackerCamera.Priority = 0;
                _uiController.SwitchPointer(false);
                _hackerMonitorUI.SwitchPointer(true);
                InstanceFinder.GameManager.ShowCursor(false, CursorLockMode.Confined);
                _inMonitor = true;
            }
            else
            {
                _hackerMonitorUI.SwitchPointer(false);
                _uiController.SwitchPointer(true);
                _monitorCamera.Priority = 0;
                hackerCamera.Priority = 10;
                InstanceFinder.GameManager.ShowCursor(false, CursorLockMode.Locked);
                _inMonitor = false;
            }
        }
    }
}