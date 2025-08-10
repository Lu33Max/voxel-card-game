using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KingUnit : Unit
{
    [SerializeField] private int shieldGain = 1;
     
    public override List<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition;
        Vector3Int[] directions = { Vector3Int.back, Vector3Int.left, Vector3Int.right, Vector3Int.forward };
        
        foreach (var direction in directions)
        {
            // Key = current Tile, Value = previous Tile
            Dictionary<Vector3Int, Vector3Int> path = new();
            
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(startPosition);

            while (queue.Count > 0)
            {
                var prevPos = queue.Dequeue();
                
                var validNeighbours = GridManager.Instance.GetReachableNeighbours(prevPos, data.maxHeightDiff,
                    true, data.traversableEdgeTypes);

                var targetPosition = prevPos + direction;

                foreach (var neighbour in validNeighbours.Where(n => n.x == targetPosition.x && n.z == targetPosition.z))
                {
                    path.Add(neighbour, prevPos);
                    var pathToTile = ReconstructPath(path, neighbour, startPosition);
                    moves.Add(new MoveCommand { TargetPosition = neighbour, Path = pathToTile });
                    
                    if(pathToTile.Count < movementRange - 1)
                        queue.Enqueue(neighbour);
                }
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
            .Where(t => GridManager.Instance.IsExistingGridPosition(t)).ToList();
    }

    public override Attack GetAttackForHoverPosition(Vector3Int hoveredPos, int attackRange, int damageMultiplier)
    {
        var allTiles = GetValidAttackTiles(attackRange);

        if (!allTiles.Contains(hoveredPos)) return null;

        return new Attack
        {
            Damage = Data.attackDamage * damageMultiplier,
            Tiles = new List<Vector3Int>{ hoveredPos },
            PlayerId = (int)GameManager.Instance.localPlayer.netId,
        };
    }

    protected override void Die()
    {
        base.Die();
        
        if(!isServer)
            return;
        
        GameManager.Instance.KingDefeated(owningTeam);
    }

    protected override void OnGameStateChanged(GameState newState)
    {
        base.OnGameStateChanged(newState);
        
        if(!isServer || newState != GameState.Attack)
            return;
        
        var directions = new List<Vector3Int> { Vector3Int.back, Vector3Int.left, Vector3Int.right, Vector3Int.forward, };
        foreach (var dir in directions)
        {
            if(!GridManager.Instance.IsExistingGridPosition(TilePosition + dir))
                continue;
            
            var unit = GridManager.Instance.GetTileAtGridPosition(TilePosition + dir).Unit;
            
            if(unit != null && unit.owningTeam == owningTeam)
                unit.AddShield(shieldGain);
        }
    }
}
