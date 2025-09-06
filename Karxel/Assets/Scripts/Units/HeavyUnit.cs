using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeavyUnit : UnitBehaviour
{
    public override IEnumerable<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = UnitRef.PositionAfterMove;
        
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
                
                var validNeighbours = GridManager.Instance.GetReachableNeighbours(prevPos,
                    false, UnitRef.Data.traversableEdgeTypes, new [] { TileData.TileState.Normal });

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

    public override List<Vector3Int> GetValidAttackTiles(Vector3Int position)
    {
        Vector3Int[] singleLayer =
        {
            position + Vector3Int.back, position + Vector3Int.left, position + Vector3Int.forward,
            position + Vector3Int.right, position + new Vector3Int(1, 0, 1), position + new Vector3Int(-1, 0, 1),
            position + new Vector3Int(1, 0, -1), position + new Vector3Int(-1, 0, -1)
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

        var tilePosition = UnitRef.TilePosition;
        List<Vector3Int> tilesToAttack = new(){ hoveredPos };

        // Clicked on the main axis
        if (Mathf.Abs(tilePosition.x - hoveredPos.x) != Mathf.Abs(tilePosition.z - hoveredPos.z))
        {
            tilesToAttack.Add(new Vector3Int(hoveredPos.x + (tilePosition.z - hoveredPos.z), tilePosition.y,
                hoveredPos.z + (tilePosition.x - hoveredPos.x)));
            tilesToAttack.Add(new Vector3Int(hoveredPos.x - (tilePosition.z - hoveredPos.z), tilePosition.y,
                hoveredPos.z - (tilePosition.x - hoveredPos.x)));
        }
        // Clicked on a diagonal
        else
        {
            tilesToAttack.Add(new Vector3Int(hoveredPos.x, tilePosition.y, tilePosition.z));
            tilesToAttack.Add(new Vector3Int(tilePosition.x, tilePosition.y, hoveredPos.z));
        }

        return new Attack
        {
            Damage = UnitRef.Data.attackDamage * damageMultiplier,
            Tiles = tilesToAttack
                        .Concat(tilesToAttack.Select(t => new Vector3Int(t.x, t.y + 1, t.z)))
                        .Concat(tilesToAttack.Select(t => new Vector3Int(t.x, t.y - 1, t.z)))
                        .ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId,
        };
    }
}
