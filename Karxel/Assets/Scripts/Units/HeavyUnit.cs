using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeavyUnit : Unit
{
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var directions = new List<Vector2Int>
        {
            Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up, 
            new (1, -1), new (1, 1) , new (-1, 1), new (-1, -1)
        };

        foreach (var direction in directions)
        {
            var path = new List<Vector2Int>();
            
            moves.Add(new MoveCommand { TargetPosition = TilePosition + direction, Path = path });
        }
        
        return moves.Where(move => GridManager.Instance.IsMoveValid(move)).ToList();
    }
}
