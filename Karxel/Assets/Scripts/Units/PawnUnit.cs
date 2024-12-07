using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PawnUnit : Unit
{
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();

        for (int i = 1; i <= movementRange; i++)
        {
            var directions = new List<Vector2Int> { Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up };

            foreach (var direction in directions)
            {
                var path = new List<Vector2Int>();
                
                for (int j = 1; j < movementRange; j++)
                    path.Add(TilePosition + direction * j);
                
                moves.Add(new MoveCommand { TargetPosition = TilePosition + i * direction, Path = path });
            }
        }
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move)).ToList();
    }
}
