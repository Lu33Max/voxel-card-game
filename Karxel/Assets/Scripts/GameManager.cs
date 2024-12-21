using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public enum GameState
{
    Movement,
    Attack,
    MovementExecution,
    AttackExecution,
    PreStart
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>SERVER ONLY<br/>List of all moves to execute when round finishes</summary>
    public Dictionary<Vector2Int, List<MoveCommand>> MoveIntents = new();

    public static UnityEvent PlayersReady = new();
    public static UnityEvent RoundTimerUp = new();
    [HideInInspector] public UnityEvent<GameState> gameStateChanged = new();

    [HideInInspector, SyncVar] public GameState gameState = GameState.PreStart;
    [HideInInspector] public Player localPlayer;
    [HideInInspector] public List<Player> redPlayers; // Server only
    [HideInInspector] public List<Player> bluePlayers; // Server only

    [Header("Team Information")]
    [SerializeField] private TextMeshProUGUI redReadyUIText;
    [SerializeField] private TextMeshProUGUI blueReadyUIText;
    
    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private int movementTime;
    [SerializeField] private int submitTime;

    [SyncVar(hook = nameof(OnUpdateRedText))] private string _redPlayerText;
    [SyncVar(hook = nameof(OnUpdateBlueText))] private string _bluePlayerText;
    
    private int _redSubmit;
    private int _blueSubmit;
    
    private int _readyPlayers;
    private int _unitsToMove;
    private int _unitsDoneMoving;

    private bool _timerActive;
    [SyncVar(hook = nameof(UpdateTimerText))] private float _timeLeft;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    private void Update()
    {
        if(!isServer)
            return;
        
        if(_timerActive)
            UpdateTimer();
    }

    [Server]
    public void AddPlayerToTeam(Team team, Player newPlayer)
    {
        switch (team)
        {
            case Team.Blue:
                bluePlayers.Add(newPlayer);
                _bluePlayerText = $"0/{bluePlayers.Count}";
                break;
            case Team.Red:
                redPlayers.Add(newPlayer);
                _redPlayerText = $"0/{redPlayers.Count}";
                break;
        }

        // Start the movement phase once all players are ready
        if (bluePlayers.Count + redPlayers.Count == NetworkServer.connections.Count)
        {
            UpdateGameState(GameState.Movement);
            _timeLeft = movementTime;
            _timerActive = true;
        }
    }

    [Server]
    private void ExecuteMoveIntents()
    {
        // Combine all moveCommands for every unit
        Dictionary<Vector2Int, MoveCommand> intendedMoves =
            MoveIntents.ToDictionary(intent => intent.Key, intent => new MoveCommand
            {
                TargetPosition = intent.Value.Last().TargetPosition,
                Path = intent.Value.SelectMany((m, index) => m.Path.Concat(index < intent.Value.Count - 1
                        ? new[] { m.TargetPosition }
                        : Enumerable.Empty<Vector2Int>()))
                    .ToList()
            });

        // Static units are all units minus the ones with move intents
        List<Vector2Int> staticUnits = GridManager.Instance.GetAllUnitTiles().Except(intendedMoves.Keys).ToList();
        Dictionary<Vector2Int, MoveCommand> actualMoves = new();

        int i = 0;
        while (intendedMoves.Count > 0)
        {
            // Remove all moves that have already ended
            foreach (var move in intendedMoves.ToList().Where(move => move.Value.Path.Count < i))
            {
                staticUnits.Add(move.Value.TargetPosition);
                intendedMoves.Remove(move.Key);
            }

            // Needs to be extra loop so that all units that would need to stop because of a collision with other
            // moving units can get to the static units
            foreach (var move in intendedMoves.ToList())
            {
                var currentTarget = move.Value.Path.Count == i ? move.Value.TargetPosition : move.Value.Path[i];

                var unitsWithSameIntent = intendedMoves.Where(m =>
                    m.Key != move.Key && m.Value.Path.Count >= i && (m.Value.Path.Count == i
                        ? m.Value.TargetPosition == currentTarget
                        : m.Value.Path[i] == currentTarget)).ToList();
                
                // If no other unit wants to move to this tile in the turn add the tile to the actual path
                if (unitsWithSameIntent.Count == 0)
                    continue;

                // If x units want to move to the same tile
                intendedMoves.Remove(move.Key);
                staticUnits.Add(i > 0 ? actualMoves[move.Key].TargetPosition : move.Key);

                foreach (var otherMove in unitsWithSameIntent)
                {
                    intendedMoves.Remove(otherMove.Key);
                    staticUnits.Add(i > 0 ? actualMoves[otherMove.Key].TargetPosition : otherMove.Key);
                }
                
            }

            bool addedStatics = true;

            while (addedStatics)
            {
                addedStatics = false;

                // Needs to be in separate loop in front so that all blocked units are already added to the static units
                foreach (var move in intendedMoves.ToList())
                {
                    var currentTarget = move.Value.Path.Count == i ? move.Value.TargetPosition : move.Value.Path[i];

                    // If a static unit is the next position, stop the movement and add self as static unit
                    if (!staticUnits.Contains(currentTarget))
                        continue;
                    
                    intendedMoves.Remove(move.Key);
                    staticUnits.Add(i > 0 ? actualMoves[move.Key].TargetPosition : move.Key);
                    addedStatics = true;
                }
            }

            // All the moves that will neither interfere with other intents nor run into static pieces
            foreach (var move in intendedMoves.ToList())
            {
                var currentTarget = move.Value.Path.Count == i ? move.Value.TargetPosition : move.Value.Path[i];
                
                if (i == 0)
                    actualMoves.Add(move.Key, new MoveCommand { TargetPosition = currentTarget, Path = new() });
                else
                    actualMoves[move.Key] = new MoveCommand
                        { TargetPosition = currentTarget, Path = move.Value.Path.GetRange(0, i) };
            }

            i++;
        }

        // Execute all build moves
        foreach (var intent in actualMoves)
        {
            var unit = GridManager.Instance.GetTileAtGridPosition(intent.Key).Unit;

            if (unit == null)
                continue;

            unit.RPCStep(intent.Value);
        }

        // Do the cleanup function for all units that had intents registered, even if they didn't move
        foreach (var unit in MoveIntents
                     .Select(origIntent => GridManager.Instance.GetTileAtGridPosition(origIntent.Key).Unit)
                     .Where(unit => unit != null))
        {
            unit.RPCCleanUp();
        }

        // Since movement is done clientside, each unit on each client will call the command
        _unitsToMove = actualMoves.Count;
        MoveIntents.Clear();

        if (_unitsToMove > 0)
        {
            UpdateGameState(GameState.MovementExecution);
        }
        else
        {
            UpdateGameState(GameState.Movement);
            _bluePlayerText = $"0/{bluePlayers.Count}";
            _redPlayerText = $"0/{redPlayers.Count}";
        }
    }

    [Server]
    private void UpdateGameState(GameState newState)
    {
        gameState = newState;
        RPCInvokeStateUpdate(newState);
    }

    [Server]
    private void UpdateTimer()
    {
        _timeLeft -= Time.deltaTime;
        
        if(_timeLeft > 0)
            return;

        _timeLeft = 0;
        _timerActive = false;
        RPCInvokeTimerUp();
    }

    private void UpdateTimerText(float old, float newTime)
    {
        var totalSeconds = Mathf.FloorToInt(newTime);
        var minuteDisplay = Mathf.FloorToInt(totalSeconds / 60f);
        var secondDisplay = (totalSeconds - minuteDisplay * 60).ToString().PadLeft(2, '0');

        timerText.text = $"{minuteDisplay}:{secondDisplay}";
    }

    private void OnUpdateRedText(string old, string newText)
    {
        redReadyUIText.text = newText;
    }
    
    private void OnUpdateBlueText(string old, string newText)
    {
        blueReadyUIText.text = newText;
    }
    
    [Command(requiresAuthority = false)]
    public void CmdPlayerSpawned()
    {
        _readyPlayers++;
        
        if(_readyPlayers == NetworkServer.connections.Count)
            RPCInvokePlayersReady();
    }
    
    [Command(requiresAuthority = false)]
    public void CmdSubmitTurn(Team team)
    {
        if (team == Team.Blue)
        {
            _blueSubmit++;
            _bluePlayerText = $"{_blueSubmit}/{bluePlayers.Count}";
        }
        else if (team == Team.Red)
        {
            _redSubmit++;
            _redPlayerText = $"{_redSubmit}/{redPlayers.Count}";
        }

        if (_blueSubmit != bluePlayers.Count || _redSubmit != redPlayers.Count)
        {
            // If all players of one team have submitted, reduce the remaining round time
            if ((_blueSubmit == bluePlayers.Count || _redSubmit == redPlayers.Count) && _timeLeft > submitTime)
                _timeLeft = submitTime;
            
            return;
        }

        _timerActive = false;
        _blueSubmit = 0;
        _redSubmit = 0;
        ExecuteMoveIntents();
    }

    [Command(requiresAuthority = false)]
    public void CmdUnitMovementDone()
    {
        _unitsDoneMoving++;

        if (_unitsDoneMoving != _unitsToMove) 
            return;
        
        _unitsDoneMoving = 0;
        _unitsToMove = 0;
        
        _bluePlayerText = $"0/{bluePlayers.Count}";
        _redPlayerText = $"0/{redPlayers.Count}";

        _timeLeft = movementTime;
        _timerActive = true;
        
        UpdateGameState(GameState.Movement);
    }

    [ClientRpc]
    private void RPCInvokeStateUpdate(GameState newState)
    {
        gameStateChanged?.Invoke(newState);
    }

    [ClientRpc]
    private void RPCInvokePlayersReady()
    {
        PlayersReady?.Invoke();
    }

    [ClientRpc]
    private void RPCInvokeTimerUp()
    {
        RoundTimerUp?.Invoke();
    }
}