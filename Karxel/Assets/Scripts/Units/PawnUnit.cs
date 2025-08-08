using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PawnUnit : Unit
{
    [SerializeField] private int baseRange = 2;
    
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition;
        var directions = new List<Vector3Int> { Vector3Int.back, Vector3Int.left, Vector3Int.right, Vector3Int.forward };
        
        for (int i = 1; i <= movementRange * baseRange; i++)
        {
            foreach (var direction in directions)
            {
                var path = new List<Vector3Int>();
                
                for (int j = 1; j < i; j++)
                    path.Add(startPosition + direction * j);
                
                moves.Add(new MoveCommand { TargetPosition = startPosition + i * direction, Path = path });
            }
        }
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move)).ToList();
    }
    
    public override List<Vector3Int> GetValidAttackTiles(int attackRange)
    {
        return new List<Vector3Int>
            {
                TilePosition + Vector3Int.forward, TilePosition + Vector3Int.left,
                TilePosition + Vector3Int.back, TilePosition + Vector3Int.right,
            }
            .Where(t => GridManager.Instance.IsValidGridPosition(t)).ToList();
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

        var tile = newAngle switch
        {
            1 => Vector3Int.right,
            2 => Vector3Int.forward,
            3 => Vector3Int.left,
            _ => Vector3Int.back,
        };

        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = new List<Vector3Int> { TilePosition + tile }.Where(tile => GridManager.Instance.IsValidGridPosition(tile)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
