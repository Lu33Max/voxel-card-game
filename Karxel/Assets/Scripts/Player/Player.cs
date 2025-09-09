using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum Team
{
    Red = 0,
    Blue = 1,
    None = 2
}

public class Player : NetworkBehaviour
{
    public static Player LocalPlayer { get; private set; }
    
    [SyncVar] public Team team;

    [SerializeField] private GameObject hud = null!;
    [SerializeField] private Button turnSubmitBtn = null!;

    [HideInInspector] public UnityEvent turnSubmitted = new();

    [field: SyncVar] public bool HasSubmitted { get; private set; }

    private void Start()
    {
        if(!isLocalPlayer)
            hud.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // Cursed Setup since Singleton Pattern inside Awake would always lead to the host being registered as Instance
        GetComponentInChildren<HandManager>().Initialize();
        GetComponentInChildren<CardManager>().Initialize();
        GetComponentInChildren<ActionPointManager>().Initialize();
        
        LocalPlayer = this;
        turnSubmitBtn.interactable = false;
        
        GameManager.Instance.GameStateChanged += OnGameStateChanged;
        GameManager.Instance.RoundTimerUp += SubmitTurn;
        DiscordManager.Instance.UpdateActivity(DiscordManager.ActivityState.Game, team, NetworkServer.connections.Count, 1);

        CmdAddToPlayerList();
    }
    
    private void OnDisable()
    {
        // Only execute if leaving during play mode
        if(GameManager.Instance == null)
            return;

        GameManager.Instance.RoundTimerUp -= SubmitTurn;
        GameManager.Instance.GameStateChanged -= OnGameStateChanged;
        GameManager.Instance.CmdLeaveLobby(netIdentity.netId);
    }

    public void SubmitTurn()
    {
        if(HasSubmitted) return;
        
        turnSubmitBtn.interactable = false;
        turnSubmitted?.Invoke();

        CmdUpdateSubmittedStatus(true);
    }

    [Server]
    public void UnreadyPlayer()
    {
        HasSubmitted = false;
    }
    
    [Command(requiresAuthority = false)]
    private void CmdUpdateSubmittedStatus(bool newState)
    {
        HasSubmitted = newState;
        GameManager.Instance.SubmitTurn();
    }

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Movement:
            case GameState.Attack:
                turnSubmitBtn.interactable = true;
                break;
        }
    }

    [Command(requiresAuthority = false)]
    // ReSharper disable once MemberCanBeMadeStatic.Local
    private void CmdAddToPlayerList()
    {
        GameManager.Instance.PlayerHasSpawned();
    }
}
