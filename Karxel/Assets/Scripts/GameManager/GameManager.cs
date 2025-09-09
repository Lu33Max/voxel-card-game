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
    [HideInInspector] public Player localPlayer;
    [HideInInspector] public List<Player> redPlayers; // Server only
    [HideInInspector] public List<Player> bluePlayers; // Server only

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
    
    private int _redSubmit;
    private int _blueSubmit;
    
    private int _readyPlayers;

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

    [Server]
    private void HandleAllUnitsDone()
    {
        if(gameState == GameState.MovementExecution) StartAttackPhase();
        if(gameState == GameState.AttackExecution) StartMovePhase();
            
        CheckHealth?.Invoke();
    }

    private void Update()
    {
        if(!isServer)
            return;
        
        if(_timerActive)
            UpdateTimer();

        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.K))
            _defeatedKings.Add(Team.Blue);
        #endif
    }

    private void OnDisable()
    {
        Unit.OnUnitDied -= HandleUnitDied;
        UnitActionManager.Instance.OnAllUnitActionsDone -= HandleAllUnitsDone;
    }

    [Server]
    private void HandleUnitDied(UnitBehaviour behaviour, Team owningTeam)
    {
        if (behaviour is KingUnit)
            _defeatedKings.Add(owningTeam);
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
        if (bluePlayers.Count + redPlayers.Count != NetworkServer.connections.Count) 
            return;
        
        UpdateGameState(GameState.Movement);
        _timeLeft = movementTime;
        _timerActive = true;
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
            RpcUpdateActionPoints(bluePlayers.Count, redPlayers.Count);
        
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
    private void StartMovePhase()
    {
        UpdateGameState(GameState.Movement);
            
        _roundCounter++;
        RPCInvokeNewRound(_roundCounter);
            
        _bluePlayerText = $"0/{bluePlayers.Count}";
        _redPlayerText = $"0/{redPlayers.Count}";
            
        _timeLeft = movementTime;
        _timerActive = true;
            
        DiscordManager.Instance.UpdateActivity(DiscordManager.ActivityState.Game, localPlayer.team, NetworkServer.connections.Count, _roundCounter);
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

        CheckForAllSubmitted();
    }

    [Server]
    private void CheckForAllSubmitted()
    {
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
        
        switch (gameState)
        {
            case GameState.Movement:
                UpdateGameState(GameState.MovementExecution);
                break;
            case GameState.Attack:
                UpdateGameState(GameState.AttackExecution);
                break;
        }
    }

    private void StartAttackPhase()
    {
        _bluePlayerText = $"0/{bluePlayers.Count}";
        _redPlayerText = $"0/{redPlayers.Count}";

        _timeLeft = movementTime;
        _timerActive = true;
        
        UpdateGameState(GameState.Attack);
    }

    [Command(requiresAuthority = false)]
    public void CmdLeaveLobby()
    {
        var players = FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        bluePlayers.Clear();
        redPlayers.Clear();

        _blueSubmit = 0;
        _redSubmit = 0;

        foreach (var player in players)
        {
            if (player.team == Team.Blue)
            {
                bluePlayers.Add(player);
                if (player.HasSubmitted)
                    _blueSubmit++;
            }
            else if (player.team == Team.Red)
            {
                redPlayers.Add(player);
                if (player.HasSubmitted)
                    _redSubmit++;
            }
        }
        
        _bluePlayerText = $"{_blueSubmit}/{bluePlayers.Count}";
        _redPlayerText = $"{_redSubmit}/{redPlayers.Count}";

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
}