using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public enum GameState
{
    Movement,
    Attack,
    MovementExecution,
    AttackExecution
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>SERVER ONLY<br/>List of all moves to execute when round finishes</summary>
    public Dictionary<Vector2Int, List<MoveCommand>> MoveIntents = new();

    public UnityEvent<GameState> GameStateChanged;
    
    [HideInInspector] public Player localPlayer;
    [HideInInspector] public List<Player> redPlayers;   // Server only
    [HideInInspector] public List<Player> bluePlayers;  // Server only

    private int _redSubmit;
    private int _blueSubmit;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Command(requiresAuthority = false)]
    public void CmdSubmitTurn(Team team)
    {
        if (team == Team.Blue)
            _blueSubmit++;
        else if(team == Team.Red)
            _redSubmit++;
        
        if (_blueSubmit == bluePlayers.Count && _redSubmit == redPlayers.Count)
        {
            ExecuteMoveIntents();
            _blueSubmit = 0;
            _redSubmit = 0;
        }
    }

    [Server]
    private void ExecuteMoveIntents()
    {
        foreach (var intent in MoveIntents)
        {
            var unit = GridManager.Instance.GetTileAtGridPosition(intent.Key).Unit;
                
            if(unit == null || intent.Value.Count == 0)
                continue;
            
            unit.RPCStep(intent.Value[0]);
        }
        
        MoveIntents.Clear();
        RPCInvokeStateUpdate(GameState.Movement);
    }

    [ClientRpc]
    private void RPCInvokeStateUpdate(GameState newState)
    {
        GameStateChanged?.Invoke(newState);
    }
}
