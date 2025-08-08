using JetBrains.Annotations;
using UnityEngine;

public class TileData
{
    public Vector3Int TilePosition;
    public Vector3 WorldPosition;

    [CanBeNull] public Unit Unit;
}
