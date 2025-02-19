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
    public int playerCount;
    public int maxPlayerCount;
    
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text lobbyPlayerText;
    
    public void SetLobbyData()
    {
        lobbyNameText.text = lobbyName;
        lobbyPlayerText.text = $"{playerCount}/{maxPlayerCount}";
    }

    public void JoinLobby()
    {
        if(epicLobby != null)
            NetworkManager.singleton.GetComponent<EOSLobby>().JoinLobby(epicLobby, new []{ "LobbyName" });
    }
}
