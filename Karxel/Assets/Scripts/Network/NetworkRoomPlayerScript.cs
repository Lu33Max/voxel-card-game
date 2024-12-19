using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Custom Room Player for Lobby
/// Mirror overrides for custom functionality
/// Joining and Leaving Lobby and Ready State
/// </summary>
public class NetworkRoomPlayerScript : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    void OnNameChanged(string _Old, string _New)
    {
        gameObject.name = playerName;
    }

    public override void OnStartClient()
    {

    }

    public override void OnClientEnterRoom()
    {

    }

    public override void OnClientExitRoom()
    {

    }

    public override void OnStartLocalPlayer()
    {

    }

    [Command]
    void CMDChangePlayerName(string name)
    {
        playerName = name;
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {

    }
}