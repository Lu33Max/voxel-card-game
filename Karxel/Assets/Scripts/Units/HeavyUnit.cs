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

    public override Attack GetAttackForHoverPosition(Vector3Int hoveredPos, int attackRange, int damageMultiplier)
    {
        var allTiles = GetValidAttackTiles(attackRange);

        if (!allTiles.Contains(hoveredPos)) return null;
        
        List<Vector3Int> tilesToAttack = new(){ hoveredPos };

        // Clicked on the main axis
        if (Mathf.Abs(TilePosition.x - hoveredPos.x) != Mathf.Abs(TilePosition.z - hoveredPos.z))
        {
            tilesToAttack.Add(new Vector3Int(hoveredPos.x + (TilePosition.z - hoveredPos.z), TilePosition.y,
                hoveredPos.z + (TilePosition.x - hoveredPos.x)));
            tilesToAttack.Add(new Vector3Int(hoveredPos.x - (TilePosition.z - hoveredPos.z), TilePosition.y,
                hoveredPos.z - (TilePosition.x - hoveredPos.x)));
        }
        // Clicked on a diagonal
        else
        {
            tilesToAttack.Add(new Vector3Int(hoveredPos.x, TilePosition.y, TilePosition.z));
            tilesToAttack.Add(new Vector3Int(TilePosition.x, TilePosition.y, hoveredPos.z));
        }

        return new Attack
        {
            Damage = Data.attackDamage * damageMultiplier,
            Tiles = tilesToAttack
                        .Concat(tilesToAttack.Select(t => new Vector3Int(t.x, t.y + 1, t.z)))
                        .Concat(tilesToAttack.Select(t => new Vector3Int(t.x, t.y - 1, t.z)))
                        .ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId,
        };
    }
}
