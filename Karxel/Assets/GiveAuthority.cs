using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class GiveAuthority : NetworkBehaviour
{
    public NetworkIdentity cube;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.V))
        {
            Givee(cube);
        }
    }

    [Command(requiresAuthority = false)]
    public void Givee(NetworkIdentity cube)
    {
        base.OnStartLocalPlayer();

        cube.GetComponent<NetworkIdentity>().AssignClientAuthority(GetComponent<NetworkIdentity>().connectionToClient);
    }
}
