using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;

public enum GameState
{
    Movement,
    Attack,
    MovementExecution,
    AttackExecution,
    PreStart,
    Win,
}

// TODO: Turn GameManager into server-only component
public class GameManager : NetworkSingleton<GameManager>
{
    public event Action? PlayersReady;
    public event Action? RoundTimerUp;
    public event Action? CheckHealth;
    public event Action<int>? NewRound;
    /// <summary> Called every frame the remaining time gets updated </summary>
    public event Action<float>? TimerUpdated;
    /// <summary> Called whenever the GameState gets updated, sending the new status with it </summary>
    public event Action<GameState>? GameStateChanged; 
    /// <summary> Called on every new round, sending the number of blue and red players </summary>
    public event Action<int, int>? UpdateActionPoints;

    [HideInInspector, SyncVar] public GameState gameState = GameState.PreStart;

    private Dictionary<Team, List<NetworkConnectionToClient>> _playerConnections = new();
    private Dictionary<Team, List<Player>> _players = new();
    
    private List<Player> BluePlayers => _players.TryGetValue(Team.Blue, out var bluePlayers) ? bluePlayers : new();
    private List<Player> RedPlayers => _players.TryGetValue(Team.Red, out var redPlayers) ? redPlayers : new();

    private int SubmittedBluePlayers => BluePlayers.Count(p => p.HasSubmitted);
    private int SubmittedRedPlayers => RedPlayers.Count(p => p.HasSubmitted);
    
    [Header("Team Information")]
    [SerializeField] private TextMeshProUGUI redReadyUIText;
    [SerializeField] private TextMeshProUGUI blueReadyUIText;
    
    [Header("Timer")]
    [SerializeField] private int movementTime;
    [SerializeField] private int submitTime;

    [Header("Game Over")] 
    [SerializeField] private GameObject gameOverScreen;

    [SyncVar(hook = nameof(OnUpdateRedText))] private string _redPlayerText;
    [SyncVar(hook = nameof(OnUpdateBlueText))] private string _bluePlayerText;
    
    private int _roundCounter = 1;
    
    private int _readyPlayers;
    private int _spawnedPlayers;
    private int _submittedPlayers;

    private readonly HashSet<Team> _defeatedKings = new();
    private AudioSource _timerAudio;

    private bool _timerActive;
    [SyncVar(hook = nameof(OnTimeLeftUpdated))] private float _timeLeft;

    protected override void Awake()
    {
        base.Awake();
        _timerAudio = gameObject.AddComponent<AudioSource>();
        _timerAudio.playOnAwake = false;
    }

    private void Start()
    {
        AudioManager.Instance.PlayMusic(AudioManager.Instance.CombatMusic);
        
        if(!isServer) return;

        Unit.OnUnitDied += HandleUnitDied;
        UnitActionManager.Instance.OnAllUnitActionsDone += HandleAllUnitsDone;
    }
    
    private void Update()
    {
        if(!isServer || !_timerActive) return;
        
        UpdateTimer();
        if(_submittedPlayers > 0) CheckForAllSubmitted();
    }
    
    private void OnDisable()
    {
        if(!isServer) return;
        
        Unit.OnUnitDied -= HandleUnitDied;
        UnitActionManager.Instance.OnAllUnitActionsDone -= HandleAllUnitsDone;
    }

    /// <summary>
    ///     Called whenever a <see cref="Player"/> has executed their LocalPlayerStart method. Tracks all player instances
    ///     that take part in the current round and starts the first play phase upon successful spawning
    /// </summary>
    [Server]
    public void PlayerHasSpawned()
    {
        _spawnedPlayers++;
        if(_spawnedPlayers != NetworkServer.connections.Count) return;
        
        // Save references to all players currently participating in the match
        foreach(var conn in NetworkServer.connections.Values)
        {
            var player = conn.identity.GetComponent<Player>();
            
            if(!_playerConnections.TryAdd(player.team, new() { conn }))
                _playerConnections[player.team].Add(conn);
           
            if(!_players.TryAdd(player.team, new() { player }))
                _players[player.team].Add(player);
        }
        
        _bluePlayerText = _players.TryGetValue(Team.Blue, out var bluePlayers) ? $"0/{bluePlayers.Count}" : "0/0";
        _redPlayerText = _players.TryGetValue(Team.Red, out var redPlayers) ? $"0/{redPlayers.Count}" : "0/0";

        _timeLeft = movementTime + 1;
        _timerActive = true;
        
        RPCInvokePlayersReady();
        UpdateGameState(GameState.Movement);
    }

    [Server]
    private void HandleAllUnitsDone()
    {
        if(gameState == GameState.MovementExecution) StartAttackPhase();
        else if(gameState == GameState.AttackExecution) StartMovePhase();
            
        CheckHealth?.Invoke();
    }

    [Server]
    private void HandleUnitDied(UnitBehaviour behaviour, Team owningTeam)
    {
        if (behaviour is KingUnit)
            _defeatedKings.Add(owningTeam);
    }

    [Server]
    private void UpdateGameState(GameState newState)
    {
        if (_defeatedKings.Count > 0)
        {
            GameOver();
            newState = GameState.Win;
        }
        
        gameState = newState;
        
        if (gameState is GameState.Movement)
            RpcUpdateActionPoints(BluePlayers.Count, RedPlayers.Count);
        
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

    [Server]
    private void StartMovePhase()
    {
        _roundCounter++;
            
        _bluePlayerText = $"0/{BluePlayers.Count}";
        _redPlayerText = $"0/{RedPlayers.Count}";
            
        _timeLeft = movementTime;
        _timerActive = true;
            
        DiscordManager.Instance.UpdateActivity(DiscordManager.ActivityState.Game, Player.LocalPlayer.team, NetworkServer.connections.Count, _roundCounter);
        UpdateGameState(GameState.Movement);
        RPCInvokeNewRound(_roundCounter);
    }

    [Server]
    private void GameOver()
    {
        var screenText = _defeatedKings.Count > 1 ? "The game ends in a tie" :
            _defeatedKings.First() == Team.Blue ? "Red team has won!" : "Blue team has won!";
        RPCGameOver(screenText);
    }
    
    public void ReturnToLobby()
    {
        if(!isServer)
            return;
        
        NetworkManager.singleton.ServerChangeScene("Lobby");
    }

    private void OnTimeLeftUpdated(float _, float newTime)
    {
        TimerUpdated?.Invoke(newTime);
    }

    private void OnUpdateRedText(string old, string newText)
    {
        redReadyUIText.text = _redPlayerText;
    }
    
    private void OnUpdateBlueText(string old, string newText)
    {
        blueReadyUIText.text = _bluePlayerText;
    }
    
    /// <summary>
    ///     Called from <see cref="Player"/> whenever a turn has been submitted. Updates display and calls to check
    ///     if all players have already submitted
    /// </summary>
    [Server]
    public void SubmitTurn()
    {
        _bluePlayerText = $"{SubmittedBluePlayers}/{BluePlayers.Count}";
        _redPlayerText = $"{SubmittedRedPlayers}/{RedPlayers.Count}";

        _submittedPlayers++;
        CheckForAllSubmitted();
    }

    /// <summary>
    ///     Checks the number of submitted players per team. If all players of one team submitted the timer is reduced
    ///     to <see cref="submitTime"/>. If every player submitted change to the execution gamestate
    /// </summary>
    [Server]
    private void CheckForAllSubmitted()
    {
        if (((SubmittedBluePlayers == BluePlayers.Count && BluePlayers.Count > 0) || 
             (SubmittedRedPlayers == RedPlayers.Count && RedPlayers.Count > 0)) && _timeLeft > submitTime)
        {
            _timeLeft = submitTime;
        }

        if (SubmittedBluePlayers != BluePlayers.Count || SubmittedRedPlayers != RedPlayers.Count) return;
        
        _timerActive = false;
        _submittedPlayers = 0;

        foreach (var player in _players.Values.SelectMany(p => p))
            player.UnreadyPlayer();
        
        UpdateGameState(gameState == GameState.Attack ? GameState.AttackExecution : GameState.MovementExecution);
    }

    private void StartAttackPhase()
    {
        _bluePlayerText = $"0/{BluePlayers.Count}";
        _redPlayerText = $"0/{RedPlayers.Count}";

        _timeLeft = movementTime;
        _timerActive = true;
        
        UpdateGameState(GameState.Attack);
    }

    [Command(requiresAuthority = false)]
    public void CmdLeaveLobby(uint leavingNetId)
    {
        if (BluePlayers.Exists(p => p.netId == leavingNetId))
        {
            _players[Team.Blue].RemoveAll(p => p.netId == leavingNetId);
            _playerConnections[Team.Blue].RemoveAll(p => p.identity.netId == leavingNetId);
        }
        else if (RedPlayers.Exists(p => p.netId == leavingNetId))
        {
            _players[Team.Red].RemoveAll(p => p.netId == leavingNetId);
            _playerConnections[Team.Red].RemoveAll(p => p.identity.netId == leavingNetId);
        }

        _bluePlayerText = $"{SubmittedBluePlayers}/{BluePlayers.Count}";
        _redPlayerText = $"{SubmittedRedPlayers}/{RedPlayers.Count}";

        if (!UnitActionManager.Instance.AllUnitActionsDone()) return;
        
        if(gameState == GameState.MovementExecution) StartAttackPhase();
        if(gameState == GameState.AttackExecution) StartMovePhase();
            
        CheckHealth?.Invoke();
    }

    [ClientRpc]
    private void RpcUpdateActionPoints(int blueCount, int redCount)
    {
        UpdateActionPoints?.Invoke(blueCount, redCount);
    }

    [ClientRpc]
    private void RPCInvokeStateUpdate(GameState newState)
    {
        GameStateChanged?.Invoke(newState);
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
        
        for(var i = 0; i < gameOverScreen.transform.childCount; i++)
            gameOverScreen.transform.GetChild(i).gameObject.SetActive(true);
    }
    
    [ClientRpc]
    private void RPCInvokeNewRound(int count)
    {
        NewRound?.Invoke(count);
    }

    [Server]
    public void CallRpcOnTeam(Action<NetworkConnectionToClient> task, Team team, uint? senderId = null)
    {
        var connections = _playerConnections.TryGetValue(team, out var connection)
            ? connection.Where(c => senderId == null || c.identity.netId != senderId).ToList()
            : new();

        foreach (var conn in connections)
            task(conn);
    }
}