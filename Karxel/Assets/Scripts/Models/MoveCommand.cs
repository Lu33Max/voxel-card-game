using System.Collections.Generic;
using UnityEngine;

public class MoveCommand {
    public Vector3Int TargetPosition;
    public List<Vector3Int> Path = new();
    public Vector3Int? BlockedPosition;
}
