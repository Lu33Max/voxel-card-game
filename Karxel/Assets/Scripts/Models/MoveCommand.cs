using System.Collections.Generic;
using UnityEngine;

public class MoveCommand {
    public Vector2Int TargetPosition;
    public List<Vector2Int> Path = new();
    public Vector2Int? BlockedPosition;
}
