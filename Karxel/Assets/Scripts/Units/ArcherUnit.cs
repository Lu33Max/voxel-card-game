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
        var directions = new List<Vector2Int> { new(1, 1), new(-1, 1), new(1, -1), new(-1,-1) };
        
        for (int i = 1; i <= movementRange * baseRange; i++)
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

    public override List<Vector2Int> GetValidAttackTiles(int attackRange)
    {
        var directions = new List<Vector2Int> { new(-1, -1), new(1, -1), new(1, 1), new(-1, 1) };
        var tiles = new List<Vector2Int>();

        foreach (var dir in directions)
            tiles.AddRange(new List<Vector2Int> { TilePosition + dir * 3, TilePosition + dir * 2, TilePosition + dir }
                .Where(t => GridManager.Instance.IsValidGridPosition(t)).ToList());

        return tiles;
    }

    public override Attack GetRotationalAttackTiles(int attackRange, int damageMultiplier, Vector3 hoveredPosition,
        Vector3 previousPosition, bool shouldBreak, out bool hasChanged)
    {
        var worldPos = GridManager.Instance.GridToWorldPosition(TilePosition);

        var newAngle = Mathf.RoundToInt((Vector2.SignedAngle(new(1, 1),
            new Vector2(hoveredPosition.x, hoveredPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 90f);
        var oldAngle = Mathf.RoundToInt((Vector2.SignedAngle(new(1, 1),
            new Vector2(previousPosition.x, previousPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 90f);
        
        if (newAngle == oldAngle && shouldBreak)
        {
            hasChanged = false;
            return null;
        }
        
        hasChanged = true;

        Vector2Int tile = newAngle switch
        {
            0 => new(-1, -1),
            1 => new(1, -1),
            2 => new(1, 1),
            3 => new(-1, 1),
            _ => new(-1, -1),
        };

        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = new List<Vector2Int> { TilePosition + tile * 3, TilePosition + tile * 2, TilePosition + tile }
                .Where(t => GridManager.Instance.IsValidGridPosition(t)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
