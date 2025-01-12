using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

/// <summary>
/// Own NetworkManager
/// Overrides Mirror Methods with custom functionality
/// Methods for Joining Players as well as leaving ones and Ready states
/// </summary>
public class NetworkRoom : NetworkRoomManager
{
    public override void OnClientConnect()
    {
        base.OnClientConnect();

        if (!NetworkClient.ready)
            NetworkClient.Ready();

        NetworkClient.AddPlayer();
    }

    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
    {

        gamePlayer.name = "HALLO_ICH_BIN_SUPER";

        return true;
    }

    public override void OnRoomServerPlayersReady()
    {
        //LoadGame
        ServerChangeScene(GameplayScene);
    }

    public override void OnRoomServerPlayersNotReady()
    {
        
    }
}
