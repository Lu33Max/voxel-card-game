using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HealerUnit : UnitBehaviour
{
    [SerializeField] private int baseRange = 3;
    
    public override IEnumerable<MoveCommand> GetValidMoves(Vector3Int unitPosition, int movementRange)
    {
        var moves = new List<MoveCommand>();

        Vector3Int[] directions = { Vector3Int.back, Vector3Int.left, Vector3Int.right, Vector3Int.forward };
        
        foreach (var direction in directions)
        {
            // Key = current Tile, Value = previous Tile
            Dictionary<Vector3Int, Vector3Int> path = new();
            
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(unitPosition);

            while (queue.Count > 0)
            {
                var prevPos = queue.Dequeue();
                
                var validNeighbours = GridManager.Instance.GetReachableNeighbours(prevPos,
                    true, UnitRef.Data.traversableEdgeTypes, new [] { TileData.TileState.Normal }).ToArray();
                
                var targetPosition = prevPos + direction;

                foreach (var neighbour in validNeighbours.Where(n => n.x == targetPosition.x && n.z == targetPosition.z))
                {
                    path.Add(neighbour, prevPos);
                    var pathToTile = ReconstructPath(path, neighbour, unitPosition);
                    moves.Add(new MoveCommand { TargetPosition = neighbour, Path = pathToTile });
                    
                    if(pathToTile.Count < movementRange * baseRange - 1)
                        queue.Enqueue(neighbour);
                }
            }
        }
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move)).ToList();
    }

    public override List<Vector3Int> GetValidAttackTiles(Vector3Int position)
    {
        return CalculateHealTiles(position)
            .Where(t => GridManager.Instance.IsExistingGridPosition(t, out _)).ToList();
    }

    public override Attack? GetAttackForHoverPosition(Vector3Int hoveredPos, int damageMultiplier)
    {
        var allTiles = CalculateHealTiles(UnitRef.TilePosition);
        if (!allTiles.Contains(hoveredPos)) return null;

        return new Attack
        {
            Damage = UnitRef.Data.attackDamage * damageMultiplier,
            Tiles = allTiles.Where(t => GridManager.Instance.IsExistingGridPosition(t, out _)).ToList(),
            PlayerId = (int)Player.LocalPlayer.netId,
        };
    }

    /// <summary> Calculates a box of tiles around the given center position </summary>
    private static List<Vector3Int> CalculateHealTiles(Vector3Int centerPos)
    {
        List<Vector3Int> positions = new();
        
        for(var x = -1; x <= 1; x++)
            for(var y = -1; y <= 1; y++)
                for(var z = -1; z <= 1; z++)
                    positions.Add(centerPos + new Vector3Int(x, y, z));

        return positions;
    }
}
