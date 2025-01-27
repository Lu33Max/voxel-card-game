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
        
        var radius = movementRange * baseRange;
        
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var offset = new Vector2Int(x, y);
                var targetPosition = startPosition + offset;

                if (offset.x * offset.x + offset.y * offset.y > radius * radius) 
                    continue;
                
                var path = GeneratePath(startPosition, targetPosition);
                moves.Add(new MoveCommand { TargetPosition = targetPosition, Path = path });
            }
        }
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move)).ToList();
    }

    public override Attack GetValidAttackTiles(int attackRange, int damageMultiplier, Vector3 hoveredPosition,
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
            Tiles = new List<Vector2Int>
                {
                    TilePosition + Vector2Int.up, TilePosition + Vector2Int.left, TilePosition + Vector2Int.down, 
                    TilePosition + Vector2Int.right, TilePosition + new Vector2Int(1, 1), TilePosition + new Vector2Int(-1, 1), 
                    TilePosition + new Vector2Int(1, -1), TilePosition + new Vector2Int(-1, -1)
                }
                .Where(t => GridManager.Instance.IsValidGridPosition(t)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
