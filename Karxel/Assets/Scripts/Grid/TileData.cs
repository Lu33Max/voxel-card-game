using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class TileData
{
    public Vector3Int TilePosition;
    public Vector3 WorldPosition;
    public List<Tile.TileNeighbour> TileNeighbours;

    [CanBeNull] public Unit Unit;
}
