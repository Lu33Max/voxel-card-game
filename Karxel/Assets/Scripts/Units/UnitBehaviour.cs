using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary> Interface to define unit specific behaviours such as movement and attack patterns </summary>
[RequireComponent(typeof(Unit))]
public abstract class UnitBehaviour : NetworkBehaviour
{
    /// <summary> Reference to the <see cref="Unit"/> script attached to this gameobject </summary>
    protected Unit UnitRef = null!;

    protected virtual void Awake()
    {
        UnitRef = GetComponent<Unit>();
    }

    /// <summary>Get all tiles currently reachable by the unit. Only includes valid moves.</summary>
    /// <param name="unitPosition"> Grid Position from which the moves should be calculated </param>
    /// <param name="movementRange">The movement range given by the played card</param>
    public abstract IEnumerable<MoveCommand> GetValidMoves(Vector3Int unitPosition, int movementRange);

    /// <summary> Calculates a list of unique tile positions that can be attacked from the current unit position </summary>
    /// <param name="position"> Tile position from which to start the calculation </param>
    public abstract List<Vector3Int> GetValidAttackTiles(Vector3Int position);
    
    /// <summary>
    ///     Calculates which tiles should be attacked based on the currently hovered tile. Returns nul if no valid tile
    ///     is hovered
    /// </summary>
    /// <param name="hoveredPos"> Position of the currently hovered tile </param>
    /// <param name="damageMultiplier"> DamageMultiplier from the currently active card </param>
    public abstract Attack? GetAttackForHoverPosition(Vector3Int hoveredPos, int damageMultiplier);
    
    /// <summary> Used in path calculations to backtrack the pathway from the ending tile </summary>
    /// <param name="path"> Paths taken by flood algorithm. Key = newPosition, Value = prevPosition </param>
    /// <param name="endPosition"> Position from which the backtracking should start </param>
    /// <param name="startPosition"> position on which the backtracking end. This position is NOT included in returned path </param>
    protected static List<Vector3Int> ReconstructPath(IReadOnlyDictionary<Vector3Int, Vector3Int> path, Vector3Int endPosition, Vector3Int startPosition)
    {
        List<Vector3Int> constructedPath = new();
        var currentPosition = endPosition;
        
        while (path.TryGetValue(currentPosition, out var prevPosition) && prevPosition != startPosition)
        {
            constructedPath.Add(prevPosition);
            currentPosition = prevPosition;
        }

        // The reconstruction works from end to beginning, so it has to be reversed
        constructedPath.Reverse();
        return constructedPath;
    }
}
