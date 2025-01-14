using DavidFDev.DevConsole;
using SEEP.Network;
using UnityEditor;
using UnityEngine;

namespace SEEP.Utils
{
    public class ConsoleHelper : MonoBehaviour
    {
        private ClientController _clientController;

        private void Start()
        {
            _clientController = GetComponent<ClientController>();
            InitializeCommonCommands();
            InitializeDevCommands();
        }

        private void InitializeCommonCommands()
        {
            DevConsole.AddCommand(Command.Create(
                name: "players",
                aliases: "playerlist",
                helpText: "Display list of players",
                callback: () =>
                {
                    var clients = InstanceFinder.LobbyManager.Clients;
                    if (clients.Count <= 0)
                    {
                        DevConsole.LogError("Clients doesn't found. It a bug :(");
                    }
                    else
                    {
                        DevConsole.LogCollection(clients);
                    }
                }));
        }

        private void InitializeDevCommands()
        {
#if DEBUG
            DevConsole.AddCommand(Command.Create<string>(
                name: "changenickname",
                aliases: "nick,changenick",
                helpText: "Change nickname",
                p1: Parameter.Create(
                    name: "nickname",
                    helpText: "New nickname"
                ),
                callback: _clientController.ConsoleChangeNickname
            ));

            DevConsole.AddCommand(Command.Create(
                name: "spawndrone",
                aliases: "drone",
                helpText: "Spawn drone to your player",
                callback: _clientController.ConsoleSpawnDrone
            ));
            DevConsole.AddCommand(Command.Create(
                name: "cube",
                aliases: "spawncube",
                helpText: "Spawn cube to your player",
                callback: _clientController.ConsoleSpawnCube
            ));
#endif
        }
    }
}