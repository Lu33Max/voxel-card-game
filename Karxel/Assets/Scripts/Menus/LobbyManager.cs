using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    [Header("Join and Team")]
    [SerializeField] private Button redJoinBtn;
    [SerializeField] private Button blueJoinBtn;
    [SerializeField] private Button readyBtn;
    [SerializeField] private Button startBtn;
    [SerializeField] private TextMeshProUGUI redCounter;
    [SerializeField] private TextMeshProUGUI blueCounter;
    
    [Header("No Team Playerlist")]
    [SerializeField] private GameObject playerNameCard;
    [SerializeField] private Transform noTeamList1;
    [SerializeField] private Transform noTeamList2;
    
    [Header("Team Playerlists")]
    [SerializeField] private GameObject playerTeamCard;
    [SerializeField] private Transform blueTeamList;
    [SerializeField] private Transform redTeamList;
    
    private NetworkRoom _roomManager;

    private List<NetworkRoomPlayerScript> _bluePlayers = new();
    private List<NetworkRoomPlayerScript> _redPlayers = new();
    
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

    public void SetupOnConnect(NetworkRoomPlayerScript localPlayer)
    {
        redJoinBtn.interactable = !localPlayer.isReady;
        blueJoinBtn.interactable = !localPlayer.isReady;
        
        CmdUpdatePlayerList();
    }

    public void StartGame()
    {
        if (_roomManager.allPlayersReady)
            _roomManager.ServerChangeScene(_roomManager.GameplayScene);
        else
            Debug.LogWarning("Nicht alle Spieler sind bereit!");
    }
    
    public void OnSelectTeamRed()
    {
        NetworkRoomPlayerScript localPlayer = NetworkClient.localPlayer.GetComponent<NetworkRoomPlayerScript>();
        
        if(localPlayer.isReady)
            return;
        
        localPlayer.SetTeam(Team.Red);

        redJoinBtn.interactable = false;
        blueJoinBtn.interactable = true;
        readyBtn.interactable = true;
    }

    public void OnSelectTeamBlue()
    {
        NetworkRoomPlayerScript localPlayer = NetworkClient.localPlayer.GetComponent<NetworkRoomPlayerScript>();
        
        if(localPlayer.isReady)
            return;
        
        localPlayer.SetTeam(Team.Blue);
        
        redJoinBtn.interactable = true;
        blueJoinBtn.interactable = false;
        readyBtn.interactable = true;
    }
    
    public void OnReadyButtonPressed()
    {
        NetworkRoomPlayerScript localPlayer = NetworkClient.localPlayer.GetComponent<NetworkRoomPlayerScript>();

        if (localPlayer.team == Team.None)
            return;
        
        redJoinBtn.interactable = localPlayer.isReady && localPlayer.team != Team.Red;
        blueJoinBtn.interactable = localPlayer.isReady && localPlayer.team != Team.Blue;
        
        localPlayer.SetReady(!localPlayer.isReady);
        readyBtn.GetComponentInChildren<TextMeshProUGUI>().text = localPlayer.isReady ? "Ready" : "Unready";
    }

    
    [Client]
    public void UpdatePlayerList()
    {
        _bluePlayers.Clear();
        _redPlayers.Clear();
        
        RemoveAllChildren(noTeamList1);
        RemoveAllChildren(noTeamList2);
        RemoveAllChildren(blueTeamList);
        RemoveAllChildren(redTeamList);
        
        foreach (var customPlayer in _roomManager.roomSlots.Cast<NetworkRoomPlayerScript>())
        {
            switch (customPlayer.team)
            {
                case Team.Blue:
                    _bluePlayers.Add(customPlayer);
                    var newCardBlue = Instantiate(playerTeamCard, blueTeamList);
                    newCardBlue.GetComponent<PlayerTeamCard>().Initialize(customPlayer.playerName, customPlayer.isReady);
                    break;
                case Team.Red:
                    _redPlayers.Add(customPlayer);
                    var newCardRed = Instantiate(playerTeamCard, redTeamList);
                    newCardRed.GetComponent<PlayerTeamCard>().Initialize(customPlayer.playerName, customPlayer.isReady);
                    break;
                case Team.None:
                    var parent = noTeamList1.childCount < 4 ? noTeamList1 : noTeamList2;
                    var newText = Instantiate(playerNameCard, parent);
                    newText.GetComponent<TextMeshProUGUI>().text = customPlayer.playerName;
                    break;
            }

            blueCounter.text = _bluePlayers.Count.ToString();
            redCounter.text = _redPlayers.Count.ToString();
        }
    }

    private void RemoveAllChildren(Transform tform)
    {
        for (var i = 0; i < tform.childCount; i++)
        {
            Destroy(tform.GetChild(i).gameObject);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdatePlayerList()
    {
        RpcUpdatePlayerList();
    }

    [ClientRpc]
    private void RpcUpdatePlayerList()
    {
        UpdatePlayerList();
    }
}
