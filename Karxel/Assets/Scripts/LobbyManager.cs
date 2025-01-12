using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    [SerializeField] private Button redJoinBtn;
    [SerializeField] private Button blueJoinBtn;
    [SerializeField] private Button readyBtn;
    [SerializeField] private Button startBtn;
    
    [SerializeField] private TextMeshProUGUI redCounter;
    [SerializeField] private TextMeshProUGUI blueCounter;
    
    private NetworkRoom _roomManager;
    [SyncVar(hook = nameof(OnRedCountUpdated))] private int _redCount;
    [SyncVar(hook = nameof(OnBlueCountUpdated))] private int _blueCount;

    private void Awake()
    {
        if (_roomManager == null)
            _roomManager = FindObjectOfType<NetworkRoom>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        
        startBtn.gameObject.SetActive(true);
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
        
        if(localPlayer.team != Team.None)
            CmdUpdatePlayerCount(_redCount + 1, _blueCount - 1);
        else
            CmdUpdatePlayerCount(_redCount + 1, _blueCount);
        
        localPlayer.SetTeam(Team.Red);

        redJoinBtn.interactable = false;
        blueJoinBtn.interactable = true;
    }

    public void OnSelectTeamBlue()
    {
        NetworkRoomPlayerScript localPlayer = NetworkClient.localPlayer.GetComponent<NetworkRoomPlayerScript>();
        
        if(localPlayer.team != Team.None)
            CmdUpdatePlayerCount(_redCount - 1, _blueCount + 1);
        else
            CmdUpdatePlayerCount(_redCount, _blueCount + 1);
        
        localPlayer.SetTeam(Team.Blue);
        
        redJoinBtn.interactable = true;
        blueJoinBtn.interactable = false;
    }
    
    public void OnReadyButtonPressed()
    {
        NetworkRoomPlayerScript localPlayer = NetworkClient.localPlayer.GetComponent<NetworkRoomPlayerScript>();

        if (localPlayer.team == Team.None)
            return;

        localPlayer.SetReady(true);
        readyBtn.interactable = false;
    }
    
    public void UpdatePlayerList()
    {
        foreach (NetworkRoomPlayer player in _roomManager.roomSlots)
        {
            var customPlayer = (NetworkRoomPlayerScript) player;
            // Update die Spielerliste mit customPlayer.team und customPlayer.isReady
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdatePlayerCount(int redCount, int blueCount)
    {
        _redCount = redCount;
        _blueCount = blueCount;
    }

    private void OnBlueCountUpdated(int old, int newCount)
    {
        blueCounter.text = newCount.ToString();
    }
    
    private void OnRedCountUpdated(int old, int newCount)
    {
        redCounter.text = newCount.ToString();
    }
}
