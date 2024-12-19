using System.Linq;
using Mirror;

public enum Team
{
    Red = 0,
    Blue = 1
}

public class Player : NetworkBehaviour
{
    public Team team;
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        GameManager.Instance.localPlayer = this;

        team = NetworkServer.connections.Keys.ToList()
            .FindIndex(i => i == connectionToClient.connectionId) % 2 == 0
            ? Team.Blue
            : Team.Red;
    }
}
