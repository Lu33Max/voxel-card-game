using UnityEngine;
using Mirror;

/// <summary>
/// Own NetworkManager
/// Overrides Mirror Methods with custom functionality
/// Methods for Joining Players as well as leaving ones and Ready states
/// </summary>
public class NetworkRoom : NetworkRoomManager
{
    public override void OnClientConnect()
    {
        base.OnClientConnect();

        if (!NetworkClient.ready)
            NetworkClient.Ready();

        NetworkClient.AddPlayer();
    }

    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer,
        GameObject gamePlayer)
    {
        gamePlayer.GetComponent<Player>().team = roomPlayer.GetComponent<CustomRoomPlayer>().team;
        return true;
    }

    public override void OnRoomServerPlayersReady()
    {
        Debug.Log("All players ready. Host can start the game now.");
    }
}
