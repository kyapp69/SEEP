using DavidFDev.DevConsole;
using FishNet;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
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
    }
}