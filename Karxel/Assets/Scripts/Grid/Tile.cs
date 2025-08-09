using System;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum EdgeType
    {
        None,
        HalfBlockade,
        FullBlockade,
        Jump,
        Ladder,
    }
    
    [Serializable]
    public class TileNeighbour
    {
        public Vector3Int GridPosition;
        public EdgeType EdgeType;
    }

    [SerializeField] private List<TileNeighbour> neighbours;

    public List<TileNeighbour> Neighbours => neighbours;

    public void ResetNeighbours()
    {
        neighbours.Clear();
    }
}
