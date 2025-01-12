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
        gamePlayer.GetComponent<Player>().team = roomPlayer.GetComponent<NetworkRoomPlayerScript>().team;
        return true;
    }

    public override void OnRoomServerPlayersReady()
    {
        Debug.Log("Alle Spieler sind bereit, aber der Host muss das Spiel starten.");
    }

    public override void OnRoomServerPlayersNotReady()
    {
        
    }
    
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null)
            {
                Transform startPosition = GetStartPosition();

                if (startPosition == null) 
                    continue;
                
                GameObject player = Instantiate(playerPrefab, startPosition.position, startPosition.rotation);
                NetworkServer.ReplacePlayerForConnection(conn, player, true);
            }
            else
            {
                Debug.Log($"Player already exists for connection {conn.connectionId}");
            }
        }
    }
}
