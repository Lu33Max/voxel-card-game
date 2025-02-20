using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    Movement,
    Attack,
    MovementExecution,
    AttackExecution,
    PreStart,
    Win,
    Empty
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>SERVER ONLY<br/>List of all moves to execute when round finishes</summary>
    public Dictionary<Vector2Int, List<MoveCommand>> MoveIntents = new();
    /// <summary>SERVER ONLY<br/>List of all attacks to execute when round finishes</summary>
    public Dictionary<Vector2Int, List<Attack>> AttackIntents = new();

    public static UnityEvent PlayersReady = new();
    public static UnityEvent RoundTimerUp = new();
    public static UnityEvent<Attack> AttackExecuted = new();
    public static UnityEvent CheckHealth = new();
    public static UnityEvent<int> NewRound = new();
    
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
    [SerializeField] private AudioClip timerRegular;
    [SerializeField] private AudioClip timerEnding;

    [Header("Phase Display")]
    [SerializeField] private List<Image> phaseDisplays;
    [SerializeField] private Sprite moveSprite;
    [SerializeField] private Sprite attackSprite;

    [Header("Game Over")] 
    [SerializeField] private GameObject gameOverScreen;

    [SyncVar(hook = nameof(OnUpdateRedText))] private string _redPlayerText;
    [SyncVar(hook = nameof(OnUpdateBlueText))] private string _bluePlayerText;
    
    private int _roundCounter = 1;
    
    private int _redSubmit;
    private int _blueSubmit;

    private int _unreadiedPlayers;
    private int _playersToUnready;
    
    private int _readyPlayers;
    private int _unitsToMove;
    private int _unitsDoneMoving;
    
    private int _attackRound;
    private int _unitsToAttack;
    private int _unitsDoneAttacking;

    private HashSet<Team> _defeatedKings = new();
    private AudioSource _timerAudio;

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
        _timerAudio = gameObject.AddComponent<AudioSource>();
        _timerAudio.playOnAwake = false;
    }

    private void Start()
    {
        AudioManager.Instance.PlayMusic(AudioManager.Instance.CombatMusic);
        DiscordManager.Instance.UpdateActivity(DiscordManager.ActivityState.Game, localPlayer.team, NetworkServer.connections.Count, 1);
    }

    private void Update()
    {
        if(!isServer)
            return;
        
        if(_timerActive)
            UpdateTimer();

        if (Input.GetKeyDown(KeyCode.K))
            _defeatedKings.Add(Team.Blue);
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
    public void RegisterMoveIntent(Vector2Int unitPos, MoveCommand moveCommand)
    {
        if (MoveIntents.TryGetValue(unitPos, out _))
            MoveIntents[unitPos].Add(moveCommand);
        else
            MoveIntents.Add(unitPos, new List<MoveCommand>{ moveCommand });
    }

    [Server]
    public void RegisterAttackIntent(Vector2Int unitPos, Attack newAttack, Team team)
    {
        if (AttackIntents.TryGetValue(unitPos, out _))
            AttackIntents[unitPos].Add(newAttack);
        else
            AttackIntents.Add(unitPos, new List<Attack>{ newAttack });
        
        foreach (var tile in newAttack.Tiles)
        {
            MarkerManager.Instance.RPCAddMarker(tile, new MarkerData
            {
                Type = MarkerType.Attack,
                Priority = 1,
                MarkerColor = Color.red,
                Visibility = team == Team.Blue ? "Blue" : "Red"
            });
        }
    }

    [Server]
    public void UnitDefeated(Vector2Int unitPos)
    {
        AttackIntents.Remove(unitPos);
    }

    [Server]
    public void KingDefeated(Team team)
    {
        _defeatedKings.Add(team);
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
        _unitsToMove = actualMoves.Count * NetworkServer.connections.Count;
        MoveIntents.Clear();

        if (_unitsToMove > 0)
            UpdateGameState(GameState.MovementExecution);
        else
            StartAttackPhase();
    }

    [Server]
    private void ExecuteAttackIntents()
    {
        UpdateGameState(GameState.AttackExecution);
        _attackRound = 0;
        
        // Hide all previous local tiles
        MarkerManager.Instance.RPCClearAllMarkers();
        
        ExecuteCurrentAttackRound();
    }

    [Server]
    private void ExecuteCurrentAttackRound()
    {
        // Hide all previous' rounds attack tiles
        foreach (var tile in from attacks in AttackIntents.Values from attack in attacks from tile in attack.Tiles select tile)
        {
            MarkerManager.Instance.RPCRemoveMarker(tile, MarkerType.Attack, "All");
        }
        
        var attacksToExecute = AttackIntents.Where(a => a.Value.Count > _attackRound).ToList();

        if (_defeatedKings.Count > 0)
        {
            GameOver();
            return;
        }
        
        if (!attacksToExecute.Any())
        {
            RPCPlayRoundChangeSound();
            UpdateGameState(GameState.Movement);
            
            _roundCounter++;
            RPCInvokeNewRound(_roundCounter);
            
            _bluePlayerText = $"0/{bluePlayers.Count}";
            _redPlayerText = $"0/{redPlayers.Count}";
            
            _timeLeft = movementTime;
            _timerActive = true;
            
            DiscordManager.Instance.UpdateActivity(DiscordManager.ActivityState.Game, localPlayer.team, NetworkServer.connections.Count, _roundCounter);
            
            foreach (var unit in AttackIntents
                         .Select(origIntent => GridManager.Instance.GetTileAtGridPosition(origIntent.Key).Unit)
                         .Where(unit => unit != null))
            {
                unit.RPCCleanUp();
            }
            
            AttackIntents.Clear();
        } 
        
        foreach (var attackIntent in attacksToExecute)
        {
            var currentAttack = attackIntent.Value[_attackRound];
            
            foreach (var tile in currentAttack.Tiles)
            {
                MarkerManager.Instance.RPCAddMarker(tile, new MarkerData
                {
                    Type = MarkerType.Attack,
                    MarkerColor = Color.red,
                    Priority = 1,
                    Visibility = "All"
                });
            }
            
            AttackExecuted?.Invoke(currentAttack);

            _unitsToAttack = attacksToExecute.Count * NetworkServer.connections.Count;
            
            var unit = GridManager.Instance.GetTileAtGridPosition(attackIntent.Key).Unit;
            if (unit != null)
                unit.RPCExecuteAttack(currentAttack);
        }
    }

    [Server]
    private void UpdateGameState(GameState newState)
    {
        gameState = newState;
        
        if(gameState is GameState.Movement or GameState.Attack)
            ActionLogger.Instance.LogAction("server", "server", "phaseSwitch", $"[{_roundCounter + (gameState == GameState.Movement ? 1 : 0)}]", null, null, null, null);
        
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
        
        // Logging
        ActionLogger.Instance.LogAction("server", "server", "timeUp", null, null, null, null, null);
        
        RPCInvokeTimerUp();
    }

    [Server]
    private void GameOver()
    {
        UpdateGameState(GameState.Win);

        var screenText = _defeatedKings.Count > 1 ? "The game ends in a tie" :
            _defeatedKings.First() == Team.Blue ? "Red team has won!" : "Blue team has won!";
        RPCGameOver(screenText);
    }

    private void UpdateTimerText(float old, float newTime)
    {
        var totalSeconds = Mathf.FloorToInt(newTime);
        var minuteDisplay = Mathf.FloorToInt(totalSeconds / 60f);
        var secondDisplay = (totalSeconds - minuteDisplay * 60).ToString().PadLeft(2, '0');
        var newText = $"{minuteDisplay}:{secondDisplay}";

        if (_timeLeft <= submitTime + 1 && _timeLeft > 0 && timerText.text != newText)
        {
            var pitch = 1 + (submitTime - totalSeconds) * 0.02f;
            AudioManager.PlaySfx(_timerAudio, _timeLeft >= 4 ? timerRegular : timerEnding, pitch, pitch);
        }
        
        timerText.color = _timeLeft > submitTime + 1 ? Color.white : Color.red;
        timerText.text = newText;
    }

    private void OnUpdateRedText(string old, string newText)
    {
        redReadyUIText.text = _redPlayerText;
    }
    
    private void OnUpdateBlueText(string old, string newText)
    {
        blueReadyUIText.text = _bluePlayerText;
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
        
        // Logging
        ActionLogger.Instance.LogAction("server", "server", "allSubmit", $"[{_timeLeft}]", null, null, null, null);
        
        switch (gameState)
        {
            case GameState.Movement:
                ExecuteMoveIntents();
                break;
            case GameState.Attack:
                ExecuteAttackIntents();
                break;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUnitMovementDone()
    {
        _unitsDoneMoving++;

        if (_unitsDoneMoving != _unitsToMove) 
            return;
        
        StartAttackPhase();
    }
    
    public void ReturnToLobby()
    {
        if(!isServer)
            return;
        
        NetworkManager.singleton.ServerChangeScene("Lobby");
    }

    private void StartAttackPhase()
    {
        _unitsDoneMoving = 0;
        _unitsToMove = 0;

        _bluePlayerText = $"0/{bluePlayers.Count}";
        _redPlayerText = $"0/{redPlayers.Count}";

        _timeLeft = movementTime;
        _timerActive = true;

        RPCPlayRoundChangeSound();
        UpdateGameState(GameState.Attack);
    }

    [Command(requiresAuthority = false)]
    public void CmdUnitAttackDone()
    {
        _unitsDoneAttacking++;
        
        if(_unitsDoneAttacking != _unitsToAttack)
            return;

        _unitsDoneAttacking = 0;
        _unitsToAttack = 0;
        _attackRound++;
        
        CheckHealth?.Invoke();
        ExecuteCurrentAttackRound();
    }

    [ClientRpc]
    private void RPCInvokeStateUpdate(GameState newState)
    {
        foreach (var display in phaseDisplays)
        {
            display.sprite = newState switch
            {
                GameState.Attack => attackSprite,
                GameState.Movement => moveSprite,
                _ => display.sprite
            };
        }
        
        gameStateChanged?.Invoke(newState);
    }

    [ClientRpc]
    private void RPCInvokePlayersReady()
    {
        // Mirror does not dispose of old RoomPlayers and only replaces their reference
        var roomPlayers = FindObjectsOfType<CustomRoomPlayer>();
        foreach (var player in roomPlayers)
        {
            Destroy(player.gameObject);
        }
        
        PlayersReady?.Invoke();
    }

    [ClientRpc]
    private void RPCInvokeTimerUp()
    {
        RoundTimerUp?.Invoke();
    }
    
    [ClientRpc]
    private void RPCGameOver(string text)
    {
        gameOverScreen.SetActive(true);
        gameOverScreen.GetComponent<TextMeshProUGUI>().text = text;
        
        if(!isServer)
            return;
        
        for(int i = 0; i < gameOverScreen.transform.childCount; i++)
            gameOverScreen.transform.GetChild(i).gameObject.SetActive(true);
    }
    
    [ClientRpc]
    private void RPCInvokeNewRound(int count)
    {
        NewRound?.Invoke(count);
    }

    [ClientRpc]
    private void RPCPlayRoundChangeSound()
    {
        AudioManager.PlaySfx(_timerAudio, timerEnding, 1.2f, 1.2f);
        AudioManager.PlaySfx(_timerAudio, timerEnding, 1.22f, 1.22f);
    }

    [Command(requiresAuthority = false)]
    public void CmdLogAction(string playerId, string team, string actionType, [CanBeNull] string actionValues,
        [CanBeNull] string target, [CanBeNull] string unitId, [CanBeNull] string unitName, [CanBeNull] string startPos)
    {
        ActionLogger.Instance.LogAction(playerId, team, actionType, actionValues, target, unitId, unitName, startPos);
    }
}