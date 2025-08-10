using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArcherUnit : Unit
{
    [SerializeField] private int baseRange = 2;
    
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition;
        Vector3Int[] directions = { new(1, 0, 1), new(-1, 0, 1), new(1, 0, -1), new(-1, 0, -1) };
        
        foreach (var direction in directions)
        {
            // Key = current Tile, Value = previous Tile
            Dictionary<Vector3Int, Vector3Int> path = new();
            
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(startPosition);

            while (queue.Count > 0)
            {
                var prevPos = queue.Dequeue();
                
                var validNeighbours = GridManager.Instance.GetReachableNeighbours(prevPos, data.maxHeightDiff,
                    false, data.traversableEdgeTypes);

                var targetPosition = prevPos + direction;

                foreach (var neighbour in validNeighbours.Where(n => n.x == targetPosition.x && n.z == targetPosition.z))
                {
                    path.Add(neighbour, prevPos);
                    var pathToTile = ReconstructPath(path, neighbour, startPosition);
                    moves.Add(new MoveCommand { TargetPosition = neighbour, Path = pathToTile });
                    
                    if(pathToTile.Count < movementRange * baseRange - 1)
                        queue.Enqueue(neighbour);
                }
            }
        }
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move)).ToList();
    }

    public override List<Vector3Int> GetValidAttackTiles(int attackRange)
    {
        Vector3Int[] directions =
        {
            new(-1, 0, -3), new(-2, 0, -3), new(-3, 0, -2), new(-3, 0, -1), new(-3, 0, 1), new(-3, 0, 2), new(-2, 0, 3), new(-1, 0, 3),
            new(1, 0, 3), new(2, 0, 3), new(3, 0, 2), new(3, 0, 1), new(3, 0, -1), new(3, 0, -2), new(2, 0, -3), new(1, 0, -3)
        };

        var singleLayer = directions.Select(dir => TilePosition + dir).ToArray();
        
        return singleLayer
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 1, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 2, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 3, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 1, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 2, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 3, t.z)))
            .Where(t => GridManager.Instance.IsExistingGridPosition(t)).ToList();
    }

    public override Attack GetAttackForHoverPosition(Vector3Int hoveredPos, int attackRange, int damageMultiplier)
    {
        var allTiles = GetValidAttackTiles(attackRange);

        if (!allTiles.Contains(hoveredPos)) return null;

        return new Attack
        {
            Damage = Data.attackDamage * damageMultiplier,
            Tiles = new List<Vector3Int>{ hoveredPos },
            PlayerId = (int)GameManager.Instance.localPlayer.netId,
        };
    }
}
