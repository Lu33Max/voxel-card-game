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
        
        var radius = movementRange * baseRange + 1;
        
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var z = -radius; z <= radius; z++)
                {
                    var offset = new Vector3Int(x, y, z);
                    var targetPosition = startPosition + offset;

                    if (offset.x * offset.x + offset.y * offset.y + offset.z * offset.z > radius * radius)
                        continue;

                    var path = GeneratePath(startPosition, targetPosition);
                    moves.Add(new MoveCommand { TargetPosition = targetPosition, Path = path });
                }
            }
        }
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move)).ToList();
    }

    public override List<Vector3Int> GetValidAttackTiles(int attackRange)
    {
        return new List<Vector3Int>
            {
                TilePosition + Vector3Int.back, TilePosition + Vector3Int.left,
                TilePosition + Vector3Int.forward, TilePosition + Vector3Int.right,
                TilePosition + new Vector3Int(1, 0, 1), TilePosition + new Vector3Int(-1, 0, 1),
                TilePosition + new Vector3Int(1, 0, -1), TilePosition + new Vector3Int(-1, 0, -1)
            }
            .Where(t => GridManager.Instance.IsValidGridPosition(t)).ToList();
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

        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = new List<Vector3Int>
                {
                    TilePosition + Vector3Int.back, TilePosition + Vector3Int.left, TilePosition + Vector3Int.forward, 
                    TilePosition + Vector3Int.right, TilePosition + new Vector3Int(1, 0, 1), TilePosition + new Vector3Int(-1, 0, 1), 
                    TilePosition + new Vector3Int(1, 0, -1), TilePosition + new Vector3Int(-1, 0, -1), TilePosition
                }
                .Where(t => GridManager.Instance.IsValidGridPosition(t)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
