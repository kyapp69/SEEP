using System.Linq;
using DavidFDev.DevConsole;
using SEEP.Utils;
using UnityEngine.UIElements;
using UnityEngine;
using Logger = SEEP.Utils.Logger;

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

        _startClient = buttonBox.Q<Button>("Client");
        _startClient.RegisterCallback<ClickEvent>(OnStartClientClicked);

        _serverList = buttonBox.Q<DropdownField>("ServerList");
        
        _spawnDrone = buttonBox.Q<Button>("SpawnDrone");
        _spawnDrone.SetEnabled(false);
        _spawnDrone.RegisterCallback<ClickEvent>(OnSpawnDroneClicked);

        _resetDrone = buttonBox.Q<Button>("ResetDrone");
        _resetDrone.SetEnabled(false);
        _resetDrone.RegisterCallback<ClickEvent>(OnResetDroneClicked);

        _spawnCube = buttonBox.Q<Button>("SpawnCube");
        _spawnCube.SetEnabled(false);
        _spawnCube.RegisterCallback<ClickEvent>(OnSpawnCubeCLicked);

        _showTriggers = buttonBox.Q<Button>("TriggerButton");
        _showTriggers.SetEnabled(false);
        _showTriggers.RegisterCallback<ClickEvent>(OnShowTriggersClicked);
    }

    public void OnNewServerFound()
    {
        _serverList.choices = InstanceFinder.NetworkController.FoundedServers;
    }

    public void OnLocalPlayerSpawned()
    {
        _showTriggers.SetEnabled(true);
    }

    public void OnLocalDroneSpawned()
    {
        _spawnDrone.SetEnabled(false);
        _resetDrone.SetEnabled(true);
        _spawnCube.SetEnabled(true);
    }

    public void OnClientStopped()
    {
        _startClient.text = "Start client";
        _startClient.RemoveFromClassList("cancel");
        
        _startHost.SetEnabled(true);
        
        _spawnDrone.SetEnabled(false);
        _resetDrone.SetEnabled(false);
        _showTriggers.SetEnabled(false);
        _spawnCube.SetEnabled(false);
    }

    public void OnClientConnected()
    {
        _startClient.text = "Stop client";
        _startClient.AddToClassList("cancel");
        
        _startHost.SetEnabled(false);

        _spawnDrone.SetEnabled(true);
    }

    public void OnHostStopped()
    {
        _startHost.text = "Start host";
        _startHost.RemoveFromClassList("cancel");
        
        _startClient.SetEnabled(true);
        
        _spawnDrone.SetEnabled(false);
        _resetDrone.SetEnabled(false);
        _showTriggers.SetEnabled(false);
        _spawnCube.SetEnabled(false);
    }

    public void OnHostConnected()
    {
        _startHost.text = "Stop host";
        _startHost.AddToClassList("cancel");
        
        _startClient.SetEnabled(false);

        _spawnDrone.SetEnabled(true);
    }

    private void OnStartHostClicked(ClickEvent evt)
    {
        InstanceFinder.NetworkController.SwitchHost(!InstanceFinder.NetworkController.IsHostActive);
    }

    private void OnStartClientClicked(ClickEvent evt)
    {
        if (_serverList.index == -1) return; 
        InstanceFinder.NetworkController.SwitchClient(!InstanceFinder.NetworkController.IsClientActive, _serverList.choices[_serverList.index]);
    }

    private void OnSpawnDroneClicked(ClickEvent evt)
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
            _showTriggers.RemoveFromClassList("cancel");
        }
        else
        {
            _showTriggers.text = "Hide triggers";
            _showTriggers.AddToClassList("cancel");
        }

        InstanceFinder.GameManager.ChangeTriggerVisible();
    }

    private void OnDisable()
    {
        _startHost.UnregisterCallback<ClickEvent>(OnStartHostClicked);
        _startClient.UnregisterCallback<ClickEvent>(OnStartClientClicked);
    }

    private void Update()
    {
        if (DevConsole.IsOpen)
        {
            if (_root.visible) return;
            _root.visible = true;
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            if (!_root.visible) return;
            _root.visible = false;
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
    }
}