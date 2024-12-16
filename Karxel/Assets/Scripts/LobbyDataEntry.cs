using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using TMPro;
using Mirror;

/// <summary>
/// Data Entry of a Lobby in the Lobby List
/// </summary>
public class LobbyDataEntry : MonoBehaviour
{
    public CSteamID lobbyID;
    public string lobbyName;
    public TMP_Text lobbyNameText;

    public void SetLobbyData()
    {
        lobbyNameText.text = lobbyName;
    }

    public void JoinLobby()
    {
        NetworkManager.singleton.GetComponent<SteamLobby>().JoinLobby(lobbyID);
    }
}
