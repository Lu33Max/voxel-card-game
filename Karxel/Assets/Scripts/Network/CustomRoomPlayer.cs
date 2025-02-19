using Mirror;

/// <summary>
/// Custom Room Player for Lobby
/// Mirror overrides for custom functionality
/// Joining and Leaving Lobby and Ready State
/// </summary>
public class CustomRoomPlayer : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(OnTeamChanged))]
    public Team team;

    [SyncVar(hook = nameof(OnNameChanged))] 
    public string playerName;

    [SyncVar(hook = nameof(OnReadyStatusChanged))]
    public bool isReady;

    private LobbyManager _lobby;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _lobby = FindObjectOfType<LobbyManager>();
    }
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        CmdSetName(NetworkClient.localPlayer.netId.ToString());
    }

    public void SetTeam(Team newTeam)
    {
        // Cannot change team while ready
        if (isReady)
            return;

        CmdSetTeam(newTeam);
    }
    
    public void SetReady(bool readyStatus)
    {
        CmdChangeReadyState(readyStatus);
        CmdSetReady(readyStatus);
    }

    [Command(requiresAuthority = false)]
    private void CmdSetTeam(Team newTeam)
    {
        team = newTeam;
    }

    [Command(requiresAuthority = false)]
    private void CmdSetName(string newName)
    {
        playerName = newName;
    }

    [Command(requiresAuthority = false)]
    private void CmdSetReady(bool newState)
    {
        isReady = newState;
    }

    private void OnTeamChanged(Team oldTeam, Team newTeam)
    {
        if(_lobby)
            _lobby.UpdatePlayerList();
    }

    private void OnReadyStatusChanged(bool oldStatus, bool newStatus)
    {
        if(_lobby)
            _lobby.UpdatePlayerList();
    }

    private void OnNameChanged(string old, string newName)
    {
        if(isLocalPlayer)
            _lobby.SetupOnConnect(this);
    }
}