using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArcherUnit : UnitBehaviour
{
    [SerializeField] private int baseRange = 2;

    public override IEnumerable<MoveCommand> GetValidMoves(Vector3Int unitPosition, int movementRange)
    {
        var moves = new List<MoveCommand>();
        Vector3Int[] directions = { new(1, 0, 1), new(-1, 0, 1), new(1, 0, -1), new(-1, 0, -1) };
        
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
                    false, UnitRef.Data.traversableEdgeTypes, new [] { TileData.TileState.Normal });

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
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move));
    }

    public override List<Vector3Int> GetValidAttackTiles(Vector3Int position)
    {
        Vector3Int[] directions =
        {
            new(-1, 0, -3), new(-2, 0, -3), new(-3, 0, -2), new(-3, 0, -1), new(-3, 0, 1), new(-3, 0, 2), new(-2, 0, 3), new(-1, 0, 3),
            new(1, 0, 3), new(2, 0, 3), new(3, 0, 2), new(3, 0, 1), new(3, 0, -1), new(3, 0, -2), new(2, 0, -3), new(1, 0, -3)
        };

        var singleLayer = directions.Select(dir => position + dir).ToArray();
        
        return singleLayer
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 1, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 2, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 3, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 1, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 2, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 3, t.z)))
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
