using System.Collections;
using System.Collections.Generic;
using DavidFDev.DevConsole;
using FishNet;
using FishNet.Managing.Server;
using FishNet.Transporting;
using NUnit.Framework;
using SEEP;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class GameManagerTest
{
    [SetUp]
    public void Setup()
    {
        //Arrange
        var gameObject = new GameObject();

        //Act
        gameObject.AddComponent<GameManager>();
    }
    
    //Check if dev command "server" (starting server) exist in console
    [UnityTest]
    public IEnumerator DevCommandServerExist()
    {
        yield return null;

        //Assert
        Assert.True(DevConsole.GetCommand("server", out _));
    }

    //Check if dev command "connect" (connect to server) exist in console
    [UnityTest]
    public IEnumerator DevCommandConnectExist()
    {
        yield return null;

        //Assert
        Assert.True(DevConsole.GetCommand("connect", out _));
    }

    //Check if dev command "connect" (connect to server) exist in console
    [UnityTest]
    [Timeout(20000)]
    public IEnumerator StartServerAndConnectLocally()
    {
        //Arrange
        var networkManagerObject =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SEEP/Prefabs/NetworkManager.prefab");

        //Act
        GameObject.Instantiate(networkManagerObject);
        yield return null;
        if (!DevConsole.GetCommand("server", out _))
            Assert.Fail("Developer command 'server' doesn't exist in dev console");
        if (!DevConsole.GetCommand("connect", out _))
            Assert.Fail("Developer command 'connect' doesn't exist in dev console");
        var server = InstanceFinder.ServerManager;
        var client = InstanceFinder.ClientManager;
        
        if (!DevConsole.RunCommand("server"))
            Assert.Fail("Developer command 'server' failed in start");

        while (!server.Started)
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }
        
        if (!DevConsole.RunCommand("connect localhost"))
            Assert.Fail("Developer command 'connect' failed in start");

        while (!client.Started)
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }
        
        //Assert
        Assert.Pass();
    }
}