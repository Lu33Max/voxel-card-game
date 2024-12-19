using Mirror;

public class Player : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        GameManager.Instance.localPlayer = gameObject;
    }
}
