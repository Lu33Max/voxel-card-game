using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        Debug.Log("Start Local");
        base.OnStartLocalPlayer();
        GameManager.Instance.localPlayer = gameObject;
    }

    [Command(requiresAuthority = false)]
    public void CMDRequestAuthority(NetworkIdentity obj)
    {
        obj.GetComponent<NetworkIdentity>().AssignClientAuthority(GetComponent<NetworkIdentity>().connectionToClient);
    }

    [Command(requiresAuthority = false)]
    public void CMDRemoveAuthority(NetworkIdentity obj)
    {
        obj.GetComponent<NetworkIdentity>().RemoveClientAuthority();
    }
}
