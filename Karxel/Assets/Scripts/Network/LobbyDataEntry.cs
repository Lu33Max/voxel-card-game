using Epic.OnlineServices.Lobby;
using UnityEngine;
using TMPro;
using Mirror;

/// <summary>
/// Data Entry of a Lobby in the Lobby List
/// </summary>
public class LobbyDataEntry : MonoBehaviour
{
    public LobbyDetails epicLobby;
    public string lobbyName;
    public TMP_Text lobbyNameText;

    public void SetLobbyData()
    {
        lobbyNameText.text = lobbyName;
    }

    public void JoinLobby()
    {
        if(epicLobby != null)
            NetworkManager.singleton.GetComponent<EOSLobby>().JoinLobby(epicLobby, new []{ "LobbyName" });
    }
}
