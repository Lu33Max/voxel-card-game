using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using JetBrains.Annotations;
using UnityEngine;
using Steamworks;
using TMPro;
using Mirror;
using UnityEngine.Serialization;

/// <summary>
/// Data Entry of a Lobby in the Lobby List
/// </summary>
public class LobbyDataEntry : MonoBehaviour
{
    public CSteamID? steamLobbyID;
    public LobbyDetails epicLobby;
    public string lobbyName;
    public TMP_Text lobbyNameText;

    public void SetLobbyData()
    {
        lobbyNameText.text = lobbyName;
    }

    public void JoinLobby()
    {
        if(steamLobbyID.HasValue)
            NetworkManager.singleton.GetComponent<SteamLobby>().JoinLobby(steamLobbyID.Value);
        
        else if(epicLobby != null)
            NetworkManager.singleton.GetComponent<EOSLobby>().JoinLobby(epicLobby, new []{ "LobbyName" });
    }
}
