using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeavyUnit : Unit
{
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition;
        
        Vector3Int[] directions =
        {
            Vector3Int.back, Vector3Int.left, Vector3Int.right, Vector3Int.forward, 
            new (1, 0, -1), new (1, 0, 1) , new (-1, 0, 1), new (-1, 0, -1)
        };

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
                    
                    if(pathToTile.Count < movementRange - 1)
                        queue.Enqueue(neighbour);
                }
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
            TilePosition + new Vector3Int(1, 0, -1), TilePosition + new Vector3Int(-1, 0, -1)
        };
        
        return singleLayer
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 1, t.z)))
            .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 1, t.z)))
            .Where(t => GridManager.Instance.IsExistingGridPosition(t)).ToList();
    }

    public override Attack GetRotationalAttackTiles(int attackRange, int damageMultiplier, Vector3 hoveredPosition,
        Vector3 previousPosition, bool shouldBreak, out bool hasChanged)
    {
        var worldPos = GridManager.Instance.GridToWorldPosition(TilePosition).GetValueOrDefault();
        
        var newAngle = Mathf.RoundToInt((Vector2.SignedAngle(Vector2.up, 
            new Vector2(hoveredPosition.x, hoveredPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 90f);
        var oldAngle = Mathf.RoundToInt((Vector2.SignedAngle(Vector2.up, 
            new Vector2(previousPosition.x, previousPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 90f);
        
        if (newAngle == oldAngle && shouldBreak)
        {
            hasChanged = false;
            return null;
        }
        
        hasChanged = true;

        var relativeLayer = newAngle switch
        {
            1 => new List<Vector3Int> { Vector3Int.right, new (1, 0, -1), new (1, 0, 1) },
            2 => new List<Vector3Int> { Vector3Int.forward, new (-1, 0, 1), new (1, 0, 1) },
            3 => new List<Vector3Int> { Vector3Int.left, new (-1, 0, -1), new (-1, 0, 1) },
            _ => new List<Vector3Int> { Vector3Int.back, new (-1, 0, -1), new (1, 0, -1) },
        };

        var singleLayer = relativeLayer.Select(t => TilePosition + t).ToArray();

        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = singleLayer
                .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y + 1, t.z)))
                .Concat(singleLayer.Select(t => new Vector3Int(t.x, t.y - 1, t.z)))
                .Where(tile => GridManager.Instance.IsExistingGridPosition(tile)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
