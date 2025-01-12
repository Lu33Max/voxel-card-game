using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoomPlayerData : NetworkBehaviour
{
    [SyncVar]
    public int teamID;
}
