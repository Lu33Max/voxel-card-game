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
        var directions = new List<Vector3Int> { new(1, 0, 1), new(-1, 0, 1), new(1, 0, -1), new(-1, 0, -1) };
        
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
        List<Vector3Int> directions = new()
        {
            new(-1, 0, -3), new(-2, 0, -3), new(-3, 0, -2), new(-3, 0, -1), new(-3, 0, 1), new(-3, 0, 2), new(-2, 0, 3), new(-1, 0, 3),
            new(1, 0, 3), new(2, 0, 3), new(3, 0, 2), new(3, 0, 1), new(3, 0, -1), new(3, 0, -2), new(2, 0, -3), new(1, 0, -3)
        };
        var tiles = new List<Vector3Int>();

        foreach (var dir in directions)
            tiles.AddRange(new List<Vector3Int> { TilePosition + dir }
                .Where(t => GridManager.Instance.IsValidGridPosition(t)).ToList());

        return tiles;
    }

    public override Attack GetRotationalAttackTiles(int attackRange, int damageMultiplier, Vector3 hoveredPosition,
        Vector3 previousPosition, bool shouldBreak, out bool hasChanged)
    {
        var worldPos = GridManager.Instance.GridToWorldPosition(TilePosition).GetValueOrDefault();

        var newAngle = Mathf.FloorToInt((int)(Vector2.SignedAngle(new(0, 1),
            new Vector2(hoveredPosition.x, hoveredPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 22.5f);
        var oldAngle = Mathf.FloorToInt((int)(Vector2.SignedAngle(new(0, 1),
            new Vector2(previousPosition.x, previousPosition.z) - new Vector2(worldPos.x, worldPos.z)) + 180) / 22.5f);
        
        if (newAngle == oldAngle && shouldBreak)
        {
            hasChanged = false;
            return null;
        }
        
        hasChanged = true;

        Vector3Int tile = newAngle switch
        {
            15 => new(-1, 0, -3),
            14 => new(-2, 0, -3),
            13 => new(-3, 0, -2),
            12 => new(-3, 0, -1),
            11=> new(-3, 0, 1),
            10 => new(-3, 0, 2),
            9 => new(-2, 0, 3),
            8 => new(-1, 0, 3),
            7 => new(1, 0, 3),
            6 => new(2, 0, 3),
            5 => new(3, 0, 2),
            4 => new(3, 0, 1),
            3 => new(3, 0, -1),
            2 => new(3, 0, -2),
            1 => new(2, 0, -3),
            0 => new(1, 0, -3),
            _ => new(1, 0, -3),
        };
        
        return new Attack
        {
            Damage = data.attackDamage * damageMultiplier,
            Tiles = new List<Vector3Int> { TilePosition + tile }
                .Where(t => GridManager.Instance.IsValidGridPosition(t)).ToList(),
            PlayerId = (int)GameManager.Instance.localPlayer.netId
        };
    }
}
