using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class TileData
{
    public enum TileState
    {
        Normal,
        Flooded,
    }
    
    public Vector3Int TilePosition;
    public Vector3 WorldPosition;
    public List<Tile.TileNeighbour> TileNeighbours;
    public TileState State = TileState.Normal;

    public Unit? Unit;
}
