using System;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum Team
{
    Red = 0,
    Blue = 1
}

public class Player : NetworkBehaviour
{
    [SyncVar] public Team team;

    [SerializeField] private GameObject hud;
    [SerializeField] private Button turnSubmitBtn;

    public UnityEvent turnSubmitted = new();
    
    private void Start()
    {
        if(!isLocalPlayer)
            hud.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        GameManager.Instance.localPlayer = this;
        GameManager.Instance.gameStateChanged.AddListener(OnGameStateChanged);
        GameManager.Instance.CmdPlayerSpawned();

        var newTeam = NetworkServer.connections.Keys.ToList()
            .FindIndex(i => i == connectionToClient.connectionId) % 2 == 0
            ? Team.Blue
            : Team.Red;
        
        CmdUpdateTeam(newTeam);
        CmdAddToPlayerList(newTeam);
    }

    public void SubmitTurn()
    {
        turnSubmitBtn.interactable = false;
        turnSubmitted?.Invoke();
        GameManager.Instance.CmdSubmitTurn(team);
    }

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Movement:
                turnSubmitBtn.interactable = true;
                break;
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdateTeam(Team team)
    {
        this.team = team;
    }

    [Command(requiresAuthority = false)]
    private void CmdAddToPlayerList(Team team)
    {
        if (team == Team.Blue)
            GameManager.Instance.bluePlayers.Add(this);
        else if(team == Team.Red)
            GameManager.Instance.redPlayers.Add(this);
    }
}
