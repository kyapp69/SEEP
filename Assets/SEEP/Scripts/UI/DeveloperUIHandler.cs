using System.Linq;
using DavidFDev.DevConsole;
using SEEP.Utils;
using UnityEngine.UIElements;
using UnityEngine;

public class DeveloperUIHandler : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _developerMenu;
    private DropdownField _serverList;
    private Button _startHost;
    private Button _startClient;
    private Button _spawnDrone;
    private Button _resetDrone;
    private Button _spawnCube;
    private Button _showTriggers;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        _root = uiDocument.rootVisualElement;
        _developerMenu = _root.Children().First();
        var buttonBox = _developerMenu.Q<VisualElement>("ButtonBox");

        _startHost = buttonBox.Q<Button>("Host");
        _startHost.RegisterCallback<ClickEvent>(OnStartHostClicked);
        InstanceFinder.GameManager.OnHostConnected += OnHostConnected;
        InstanceFinder.GameManager.OnHostStopped += OnHostStopped;

        _startClient = buttonBox.Q<Button>("Client");
        _startClient.RegisterCallback<ClickEvent>(OnStartClientClicked);
        InstanceFinder.GameManager.OnClientConnected += OnClientConnected;
        InstanceFinder.GameManager.OnClientStopped += OnClientStopped;

        _serverList = buttonBox.Q<DropdownField>("ServerList");
        InstanceFinder.GameManager.OnNewFoundedServer += OnNewServerFound;

        _spawnDrone = buttonBox.Q<Button>("SpawnDrone");
        _spawnDrone.SetEnabled(false);
        _spawnDrone.RegisterCallback<ClickEvent>(OnSpawnDroneCLicked);
        InstanceFinder.GameManager.OnLocalDroneSpawned += OnLocalDroneSpawned;

        _resetDrone = buttonBox.Q<Button>("ResetDrone");
        _resetDrone.SetEnabled(false);
        _resetDrone.RegisterCallback<ClickEvent>(OnResetDroneClicked);

        _spawnCube = buttonBox.Q<Button>("SpawnCube");
        _spawnCube.SetEnabled(false);
        _spawnCube.RegisterCallback<ClickEvent>(OnSpawnCubeCLicked);
        InstanceFinder.GameManager.OnLocalPlayerSpawned += OnLocalPlayerSpawned;

        _showTriggers = buttonBox.Q<Button>("TriggerButton");
        _showTriggers.SetEnabled(false);
        _showTriggers.RegisterCallback<ClickEvent>(OnShowTriggersClicked);
    }

    private void OnNewServerFound()
    {
        _serverList.choices = InstanceFinder.GameManager.FoundedServers.Select(ip => ip.Address.ToString()).ToList();
    }

    private void OnLocalPlayerSpawned()
    {
        _showTriggers.SetEnabled(true);
        _spawnCube.SetEnabled(true);
    }

    private void OnLocalDroneSpawned()
    {
        _spawnDrone.SetEnabled(false);
        _resetDrone.SetEnabled(true);
    }

    private void OnClientStopped()
    {
        _startClient.text = "Start client";
        _startHost.SetEnabled(true);
    }

    private void OnClientConnected()
    {
        _startClient.text = "Stop client";
        _startHost.SetEnabled(false);

        _spawnDrone.SetEnabled(true);
    }

    private void OnHostStopped()
    {
        _startHost.text = "Start host";
        _startClient.SetEnabled(true);
    }

    private void OnHostConnected()
    {
        _startHost.text = "Stop host";
        _startClient.SetEnabled(false);

        _spawnDrone.SetEnabled(true);
    }

    private void OnStartHostClicked(ClickEvent evt)
    {
        InstanceFinder.GameManager.SwitchHost(!InstanceFinder.GameManager.IsHostActive);
    }

    private void OnStartClientClicked(ClickEvent evt)
    {
        if (_serverList.index == -1) return; 
        InstanceFinder.GameManager.SwitchClient(!InstanceFinder.GameManager.IsClientActive, _serverList.choices[_serverList.index]);
    }

    private void OnSpawnDroneCLicked(ClickEvent evt)
    {
        InstanceFinder.GameManager.LocalClient.ConsoleSpawnDrone();
    }

    private void OnResetDroneClicked(ClickEvent evt)
    {
        InstanceFinder.GameManager.LocalDrone.RequireToTeleport();
    }

    private void OnSpawnCubeCLicked(ClickEvent evt)
    {
        InstanceFinder.GameManager.LocalClient.ConsoleSpawnCube();
    }

    private void OnShowTriggersClicked(ClickEvent evt)
    {
        if (InstanceFinder.GameManager.TriggerVisible)
        {
            _showTriggers.text = "Show triggers";
            _showTriggers.RemoveFromClassList("cancelbutton");
        }
        else
        {
            _showTriggers.text = "Hide triggers";
            _showTriggers.AddToClassList("cancelbutton");
        }

        InstanceFinder.GameManager.ChangeTriggerVisible();
    }

    private void OnDisable()
    {
        _startHost.UnregisterCallback<ClickEvent>(OnStartHostClicked);
        _startClient.UnregisterCallback<ClickEvent>(OnStartClientClicked);

        if (InstanceFinder.GameManager == null) return;

        InstanceFinder.GameManager.OnHostConnected -= OnHostConnected;
        InstanceFinder.GameManager.OnHostStopped -= OnHostStopped;
        InstanceFinder.GameManager.OnClientConnected -= OnClientConnected;
        InstanceFinder.GameManager.OnClientStopped -= OnClientStopped;
    }

    private void Update()
    {
        if (DevConsole.IsOpen)
        {
            if (!_root.visible)
            {
                _root.visible = true;
                UnityEngine.Cursor.visible = true;
                UnityEngine.Cursor.lockState = CursorLockMode.None;
            }
        }
        else
        {
            if (_root.visible)
            {
                _root.visible = false;
                UnityEngine.Cursor.visible = false;
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}