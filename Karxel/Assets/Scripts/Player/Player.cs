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
    [SyncVar] public Team team;

    [SerializeField] private GameObject hud;
    [SerializeField] private Button turnSubmitBtn;

    [HideInInspector] public UnityEvent turnSubmitted = new();

    public bool HasSubmitted { get; private set; }

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
        
        GameManager.Instance.localPlayer = this;
        GameManager.Instance.GameStateChanged += OnGameStateChanged;
        GameManager.Instance.RoundTimerUp += SubmitTurn;
        GameManager.Instance.CmdPlayerSpawned();
        
        DiscordManager.Instance.UpdateActivity(DiscordManager.ActivityState.Game, team, NetworkServer.connections.Count, 1);

        CmdAddToPlayerList(team);
    }
    
    private void OnDestroy()
    {
        // Only execute if leaving during play mode
        if(GameManager.Instance == null)
            return;
        
        GameManager.Instance.RoundTimerUp -= SubmitTurn;
        GameManager.Instance.GameStateChanged -= OnGameStateChanged;
        GameManager.Instance.CmdLeaveLobby();
    }

    public void SubmitTurn()
    {
        if(HasSubmitted)
            return;

        HasSubmitted = true;
        turnSubmitBtn.interactable = false;
        turnSubmitted?.Invoke();
        
        GameManager.Instance.CmdSubmitTurn(team);
    }

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Movement:
            case GameState.Attack:
                turnSubmitBtn.interactable = true;
                HasSubmitted = false;
                break;
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdAddToPlayerList(Team team)
    {
        GameManager.Instance.AddPlayerToTeam(team, this);
    }
}
