using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KingUnit : UnitBehaviour
{
    private void Start()
    {
        GameManager.Instance.GameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.Instance.GameStateChanged -= HandleGameStateChanged;
    }

    public override IEnumerable<MoveCommand> GetValidMoves(int movementRange)
    {
        var moves = new List<MoveCommand>();
        var startPosition = UnitRef.PositionAfterMove;
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
                
                var validNeighbours = GridManager.Instance.GetReachableNeighbours(prevPos,
                    true, UnitRef.Data.traversableEdgeTypes, new [] { TileData.TileState.Normal });

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

    public override List<Vector3Int> GetValidAttackTiles(Vector3Int position)
    {
        return new List<Vector3Int>
            {
                position + Vector3Int.forward, position + Vector3Int.left,
                position + Vector3Int.back, position + Vector3Int.right,
            }
            .Where(t => GridManager.Instance.IsExistingGridPosition(t, out _)).ToList();
    }

    public override Attack? GetAttackForHoverPosition(Vector3Int hoveredPos, int damageMultiplier)
    {
        var allTiles = GetValidAttackTiles(UnitRef.TilePosition);

        if (!allTiles.Contains(hoveredPos)) return null;

        return new Attack
        {
            Damage = UnitRef.Data.attackDamage * damageMultiplier,
            Tiles = new List<Vector3Int>{ hoveredPos },
            PlayerId = (int)GameManager.Instance.localPlayer.netId,
        };
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if(!isServer || newState != GameState.Attack)
            return;
        
        var directions = new List<Vector3Int>
            { Vector3Int.back, Vector3Int.left, Vector3Int.right, Vector3Int.forward, };
        
        foreach (var unit in from dir in directions
                 where GridManager.Instance.IsExistingGridPosition(UnitRef.TilePosition + dir, out _)
                 select GridManager.Instance.GetTileAtGridPosition(UnitRef.TilePosition + dir).Unit
                 into unit
                 where unit != null && unit.owningTeam == UnitRef.owningTeam && !unit.HasEffectOfTypeActive(Unit.StatusEffect.Shielded)
                 select unit)
        {
            unit.ServerAddNewStatusEffect(new Unit.UnitStatus{ Status = Unit.StatusEffect.Shielded, Duration = -1});
        }
    }
}
