using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private NetworkRoom _roomManager;

    private void Awake()
    {
        if (_roomManager == null)
        {
            _roomManager = FindObjectOfType<NetworkRoom>();
        }

        if (_roomManager == null)
        {
            Debug.LogError("CustomRoomManager konnte nicht gefunden werden!");
        }
    }
    
    public void StartGame()
    {
        if (_roomManager.allPlayersReady)
        {
            Debug.Log("Spiel startet!");
            _roomManager.ServerChangeScene(_roomManager.GameplayScene);
        }
        else
        {
            Debug.LogWarning("Nicht alle Spieler sind bereit!");
        }
    }
    
    public void OnSelectTeamRed()
    {
        NetworkRoomPlayerScript localPlayer = NetworkClient.localPlayer.GetComponent<NetworkRoomPlayerScript>();
        localPlayer.SetTeam(Team.Red);
    }

    public void OnSelectTeamBlue()
    {
        NetworkRoomPlayerScript localPlayer = NetworkClient.localPlayer.GetComponent<NetworkRoomPlayerScript>();
        localPlayer.SetTeam(Team.Blue);
    }
    
    public void OnReadyButtonPressed()
    {
        NetworkRoomPlayerScript localPlayer = NetworkClient.localPlayer.GetComponent<NetworkRoomPlayerScript>();
        
        if(localPlayer.team != Team.None)
            localPlayer.SetReady(true);
    }
    
    public void UpdatePlayerList()
    {
        foreach (NetworkRoomPlayer player in _roomManager.roomSlots)
        {
            var customPlayer = (NetworkRoomPlayerScript) player;
            // Update die Spielerliste mit customPlayer.team und customPlayer.isReady
        }
    }
}
