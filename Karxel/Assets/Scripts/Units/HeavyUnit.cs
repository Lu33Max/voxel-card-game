using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeavyUnit : Unit
{
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition;
        
        var directions = new List<Vector3Int>
        {
            Vector3Int.back, Vector3Int.left, Vector3Int.right, Vector3Int.forward, 
            new (1, 0, -1), new (1, 0, 1) , new (-1, 0, 1), new (-1, 0, -1)
        };

        for (var i = 1; i <= movementRange; i++)
        {
            foreach (var direction in directions)
            {
                var path = new List<Vector3Int>();

                for (var j = 1; j < i; j++)
                    path.Add(startPosition + direction * j);
            
                moves.Add(new MoveCommand { TargetPosition = startPosition + direction * i, Path = path });
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
        var worldPos = GridManager.Instance.GridToWorldPosition(TilePosition).GetValueOrDefault();
        
        var newAngle = Mathf.RoundToInt((Vector2.SignedAngle(Vector2.up, new Vector2(hoveredPosition.x, hoveredPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 90f);
        var oldAngle = Mathf.RoundToInt((Vector2.SignedAngle(Vector2.up, new Vector2(previousPosition.x, previousPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 90f);
        
        if (newAngle == oldAngle && shouldBreak)
        {
            hasChanged = false;
            return null;
        }
        
        hasChanged = true;

        List<Vector3Int> tiles = newAngle switch
        {
            1 => new() { Vector3Int.right, new Vector3Int(1, 0, -1), new Vector3Int(1, 0, 1) },
            2 => new() { Vector3Int.forward, new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, 1) },
            3 => new() { Vector3Int.left, new Vector3Int(-1, 0, -1), new Vector3Int(-1, 0, 1) },
            _ => new() { Vector3Int.back, new Vector3Int(-1, 0, -1), new Vector3Int(1, 0, -1) },
        };

        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = tiles.Select(t => TilePosition + t).Where(tile => GridManager.Instance.IsValidGridPosition(tile)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
