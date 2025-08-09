using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HealerUnit : Unit
{
    [SerializeField] private int baseRange = 1;
    
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition;
        
        // Key = current Tile, Value = previous Tile
        Dictionary<Vector3Int, Vector3Int> path = new();
        
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(startPosition);

        while (queue.Count > 0)
        {
            var prevPos = queue.Dequeue();
            
            var validNeighbours = GridManager.Instance.GetReachableNeighbours(prevPos, data.maxHeightDiff,
                false, data.traversableEdgeTypes);
            
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

    public override List<Vector3Int> GetValidAttackTiles(int attackRange)
    {
        Vector3Int[] singleLayer =
        {
            TilePosition + Vector3Int.back, TilePosition + Vector3Int.left, TilePosition + Vector3Int.forward,
            TilePosition + Vector3Int.right, TilePosition + new Vector3Int(1, 0, 1), TilePosition + new Vector3Int(-1, 0, 1),
            TilePosition + new Vector3Int(1, 0, -1), TilePosition + new Vector3Int(-1, 0, -1), TilePosition
        };

        return singleLayer
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 1, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 1, t.z)))
            .Where(t => GridManager.Instance.IsExistingGridPosition(t)).ToList();
    }

    public override Attack GetRotationalAttackTiles(int attackRange, int damageMultiplier, Vector3 hoveredPosition,
        Vector3 previousPosition, bool shouldBreak, out bool hasChanged)
    {
        if (shouldBreak)
        {
            hasChanged = false;
            return null;
        }

        hasChanged = true;

        List<Vector3Int> singleLayer = new()
        {
            TilePosition + Vector3Int.back, TilePosition + Vector3Int.left, TilePosition + Vector3Int.forward,
            TilePosition + Vector3Int.right, TilePosition + new Vector3Int(1, 0, 1), TilePosition + new Vector3Int(-1, 0, 1),
            TilePosition + new Vector3Int(1, 0, -1), TilePosition + new Vector3Int(-1, 0, -1), TilePosition
        };

        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = singleLayer
                .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 1, t.z)))
                .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 1, t.z)))
                .Where(t => GridManager.Instance.IsExistingGridPosition(t)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
