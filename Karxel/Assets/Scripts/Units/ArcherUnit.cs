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

    public override Attack GetRotationalAttackTiles(int attackRange, int damageMultiplier, Vector3 hoveredPosition,
        Vector3 previousPosition, bool shouldBreak, out bool hasChanged)
    {
        var worldPos = GridManager.Instance.GridToWorldPosition(TilePosition).GetValueOrDefault();

        var newAngle = Mathf.FloorToInt((int)(Vector2.SignedAngle(new(0, 1),
            new Vector2(hoveredPosition.x, hoveredPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 22.5f);
        var oldAngle = Mathf.FloorToInt((int)(Vector2.SignedAngle(new(0, 1),
            new Vector2(previousPosition.x, previousPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 22.5f);
        
        if (newAngle == oldAngle && shouldBreak)
        {
            hasChanged = false;
            return null;
        }
        
        hasChanged = true;

        Vector3Int tile = newAngle switch
        {
            15 => new(-1, 0, -3),
            14 => new(-2, 0, -3),
            13 => new(-3, 0, -2),
            12 => new(-3, 0, -1),
            11 => new(-3, 0, 1),
            10 => new(-3, 0, 2),
            9 => new(-2, 0, 3),
            8 => new(-1, 0, 3),
            7 => new(1, 0, 3),
            6 => new(2, 0, 3),
            5 => new(3, 0, 2),
            4 => new(3, 0, 1),
            3 => new(3, 0, -1),
            2 => new(3, 0, -2),
            1 => new(2, 0, -3),
            0 => new(1, 0, -3),
            _ => new(1, 0, -3),
        };

        List<Vector3Int> tiles = new();

        for (var i = -3; i < 3; i++)
        {
            var newTile = TilePosition + tile;
            newTile.y += i;
            tiles.Add(newTile);
        }
        
        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = tiles.Where(t => GridManager.Instance.IsExistingGridPosition(t)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
