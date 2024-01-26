using DavidFDev.DevConsole;
using FishNet;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public delegate void GameManagerEvent();
#if DEBUG
    public event GameManagerEvent OnTriggerChangeVisibility;
#endif

    private void Start()
    {
#if DEBUG
        AddDevCommands();
#endif
    }

#if DEBUG

    private void AddDevCommands()
    {
        DevConsole.AddCommand(Command.Create<string>(
            name: "connect",
            helpText: "Connect to server",
            aliases: "server",
            p1: Parameter.Create(
                name: "ip",
                helpText: "IP address"
            ),
            callback: address => { InstanceFinder.ClientManager.StartConnection(address); }));

        DevConsole.AddCommand(Command.Create(
            name: "server",
            aliases: "startserver",
            helpText: "Start server",
            callback: () => InstanceFinder.ServerManager.StartConnection()
        ));
        DevConsole.AddCommand(Command.Create(
            name: "host",
            aliases: "hst",
            helpText: "Start server and connect locally",
            callback: () =>
            {
                InstanceFinder.ServerManager.StartConnection();
                InstanceFinder.ClientManager.StartConnection("localhost");
            }));
        DevConsole.AddCommand(Command.Create(
            name: "client",
            aliases: "cln",
            helpText: "Connect client locally",
            callback: () => { InstanceFinder.ClientManager.StartConnection("localhost"); }));
        DevConsole.AddCommand(Command.Create(
            name: "triggers_show",
            aliases: "trg",
            helpText: "Switch visibility of triggers",
            callback: () => { OnTriggerChangeVisibility?.Invoke(); }));
    }
#endif
}