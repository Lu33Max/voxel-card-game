using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class HeavyUnit : Unit
{
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition;
        
        var directions = new List<Vector2Int>
        {
            Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up, 
            new (1, -1), new (1, 1) , new (-1, 1), new (-1, -1)
        };

        foreach (var direction in directions)
        {
            var path = new List<Vector2Int>();
            
            moves.Add(new MoveCommand { TargetPosition = startPosition + direction, Path = path });
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

        List<Vector2Int> tiles = newAngle switch
        {
            0 => new() { Vector2Int.down, new Vector2Int(-1, -1), new Vector2Int(1, -1) },
            1 => new() { Vector2Int.right, new Vector2Int(1, -1), new Vector2Int(1, 1) },
            2 => new() { Vector2Int.up, new Vector2Int(-1, 1), new Vector2Int(1, 1) },
            3 => new() { Vector2Int.left, new Vector2Int(-1, -1), new Vector2Int(-1, 1) },
            _ => new() { Vector2Int.down, new Vector2Int(-1, -1), new Vector2Int(1, -1) },
        };

        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = tiles.Select(t => TilePosition + t).Where(tile => GridManager.Instance.IsValidGridPosition(tile)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
