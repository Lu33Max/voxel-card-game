using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KingUnit : Unit
{
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition;
        
        var directions = new List<Vector2Int> { Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up, };

        for (int i = 1; i <= movementRange; i++)
        {
            foreach (var direction in directions)
            {
                var path = new List<Vector2Int>();
                
                for (int j = 1; j < i; j++)
                    path.Add(startPosition + direction * j);
                
                moves.Add(new MoveCommand { TargetPosition = startPosition + i * direction, Path = path });
            }
        }
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move)).ToList();
    }
    
    public override Attack GetValidAttackTiles(int attackRange, int damageMultiplier, Vector3 hoveredPosition,
        Vector3 previousPosition, bool shouldBreak, out bool hasChanged)
    {
        var worldPos = GridManager.Instance.GridToWorldPosition(TilePosition);
        
        var newAngle = Mathf.RoundToInt((Vector2.SignedAngle(Vector2.up, new Vector2(hoveredPosition.x, hoveredPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 90f);
        var oldAngle = Mathf.RoundToInt((Vector2.SignedAngle(Vector2.up, new Vector2(previousPosition.x, previousPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 90f);
        
        if (newAngle == oldAngle && shouldBreak)
        {
            hasChanged = false;
            return null;
        }
        
        hasChanged = true;

        Vector2Int tile = newAngle switch
        {
            0 => Vector2Int.down,
            1 => Vector2Int.right,
            2 => Vector2Int.up,
            3 => Vector2Int.left,
            4 => Vector2Int.down,
            _ => Vector2Int.up
        };

        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = new List<Vector2Int> { TilePosition + tile }.Where(tile => GridManager.Instance.IsValidGridPosition(tile)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }

    protected override void Die()
    {
        base.Die();
        
        if(!isServer)
            return;
        
        GameManager.Instance.KingDefeated(owningTeam);
    }
}
