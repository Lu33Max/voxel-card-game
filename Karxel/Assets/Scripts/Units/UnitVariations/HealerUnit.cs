using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HealerUnit : UnitBehaviour
{
    [SerializeField] private int baseRange = 1;
    
    public override IEnumerable<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = UnitRef.PositionAfterMove;
        
        // Key = current Tile, Value = previous Tile
        Dictionary<Vector3Int, Vector3Int> path = new();
        
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(startPosition);

        while (queue.Count > 0)
        {
            var prevPos = queue.Dequeue();

            var validNeighbours = GridManager.Instance.GetReachableNeighbours(prevPos,
                false, UnitRef.Data.traversableEdgeTypes, new[] { TileData.TileState.Normal });
            
            foreach (var neighbour in validNeighbours)
            {
                if(neighbour == startPosition)
                    continue;
                
                var relativePos = startPosition - neighbour;
                
                if(Math.Pow(relativePos.x, 2) + Math.Pow(relativePos.y, 2) + Math.Pow(relativePos.z, 2) > Math.Pow(baseRange * movementRange + 0.5f, 2))
                    continue;
                
                if(!path.TryAdd(neighbour, prevPos))
                    continue;
                
                var pathToTile = ReconstructPath(path, neighbour, startPosition);
                moves.Add(new MoveCommand { TargetPosition = neighbour, Path = pathToTile });
                
                queue.Enqueue(neighbour);
            }
        }
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move)).ToList();
    }

    public override List<Vector3Int> GetValidAttackTiles(Vector3Int position)
    {
        Vector3Int[] singleLayer =
        {
            position + Vector3Int.back, position + Vector3Int.left, position + Vector3Int.forward,
            position + Vector3Int.right, position + new Vector3Int(1, 0, 1), position + new Vector3Int(-1, 0, 1),
            position + new Vector3Int(1, 0, -1), position + new Vector3Int(-1, 0, -1), position
        };

        return singleLayer
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 1, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 1, t.z)))
            .Where(t => GridManager.Instance.IsExistingGridPosition(t, out _)).ToList();
    }

    public override Attack? GetAttackForHoverPosition(Vector3Int hoveredPos, int damageMultiplier)
    {
        var allTiles = GetValidAttackTiles(UnitRef.TilePosition);

        if (!allTiles.Contains(hoveredPos)) return null;

        return new Attack
        {
            Damage = UnitRef.Data.attackDamage * damageMultiplier,
            Tiles = new List<Vector3Int>{ hoveredPos },
            PlayerId = (int)GameManager.Instance.localPlayer.netId,
        };
    }
}
