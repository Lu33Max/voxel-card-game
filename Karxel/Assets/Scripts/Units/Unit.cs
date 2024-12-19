using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public abstract class Unit : NetworkBehaviour
{
    [SyncVar] public Team owningTeam;
    
    [SerializeField] private UnitData data;
    
    protected Vector2Int TilePosition { get; private set; }
    protected List<MoveCommand> MoveIntent { get; private set; } = new();

    /// <summary>Get all tiles currently reachable by the unit. Only includes valid moves.</summary>
    /// <param name="movementRange">The movement range given by the played card</param>
    public abstract List<MoveCommand> GetValidMoves(int movementRange);
    
    /// <summary>Instantly move the unit to the given tile</summary>
    public void MoveToTile(Vector2Int tilePos)
    {
        Vector3 worldPos = GridManager.Instance.GridToWorldPosition(tilePos);
        CmdChangePosition(worldPos, tilePos);
    }

    /// <summary>Step to a target tile while passing over all the given tiles in the path</summary>
    public void StepToTile(MoveCommand moveCommand)
    {
        //StartCoroutine(MoveToPositions(moveCommand));
        CmdStep(moveCommand);
        GridManager.Instance.MoveUnit(TilePosition, moveCommand.TargetPosition);
    }
    
    // Move the unit along the given path from tile to tile
    private IEnumerator MoveToPositions(MoveCommand moveCommand)
    {
        foreach (var tile in moveCommand.Path)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorldPosition(tile);
            yield return StartCoroutine(Move(worldPos));
        }
        
        Vector3 targetPos = GridManager.Instance.GridToWorldPosition(moveCommand.TargetPosition);
        yield return StartCoroutine(Move(targetPos));
    }

    // MOve the unit to the given world position
    private IEnumerator Move(Vector3 targetPos)
    {
        float elapsedTime = 0;
        Vector3 startingPos = transform.position;
        while (elapsedTime < data.stepDuration)
        {
            transform.position = Vector3.Lerp(startingPos, targetPos, elapsedTime / data.stepDuration);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        transform.position = targetPos;
    }

    #region Networking

    [Command(requiresAuthority = false)]
    private void CmdChangePosition(Vector3 position, Vector2Int tilePos)
    {
        RPCChangePosition(position, tilePos);
    }

    [Command(requiresAuthority = false)]
    private void CmdStep(MoveCommand moveCommand)
    {
        RPCStep(moveCommand);
    }

    [Command(requiresAuthority = false)]
    public void CmdAddToMoveIntent(MoveCommand moveCommand)
    {
        RPCAddToMoveIntent(moveCommand);
        
        if (GameManager.Instance.MoveIntents.TryGetValue(TilePosition, out var _))
            GameManager.Instance.MoveIntents[TilePosition].Add(moveCommand);
        else
            GameManager.Instance.MoveIntents.Add(TilePosition, new List<MoveCommand>{ moveCommand });
    }
    
    [ClientRpc]
    private void RPCChangePosition(Vector3 position, Vector2Int tilePos)
    {
        transform.position = position;
        TilePosition = tilePos;
    }
    
    [ClientRpc]
    public void RPCStep(MoveCommand moveCommand)
    {
        StartCoroutine(MoveToPositions(moveCommand));
        
        GridManager.Instance.MoveUnit(TilePosition, moveCommand.TargetPosition);
        TilePosition = moveCommand.TargetPosition;
        MoveIntent.Clear();
    }

    [ClientRpc]
    private void RPCAddToMoveIntent(MoveCommand moveCommand)
    {
        MoveIntent.Add(moveCommand);
    }

    #endregion
}
