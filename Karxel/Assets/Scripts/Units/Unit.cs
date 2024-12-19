using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public abstract class Unit : NetworkBehaviour
{
    [SerializeField] FigureData figureData;

    protected Vector2Int TilePosition { get; private set; }

    [SerializeField] private float stepDuration = 0.3f;
    
    /// <summary>Get all tiles currently reachable by the unit. Only includes valid moves.</summary>
    /// <param name="movementRange">The movement range given by the played card</param>
    public abstract List<MoveCommand> GetValidMoves(int movementRange);

    /// <summary>Instantly move the unit to the given tile</summary>
    public void MoveToTile(Vector2Int tilePos)
    {
        Vector3 worldPos = GridManager.Instance.GridToWorldPosition(tilePos);
        CmdChangePosition(gameObject, worldPos, tilePos);
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
            yield return StartCoroutine(Move(worldPos, moveCommand.TargetPosition));
        }
        
        Vector3 targetPos = GridManager.Instance.GridToWorldPosition(moveCommand.TargetPosition);
        yield return StartCoroutine(Move(targetPos, moveCommand.TargetPosition));
    }

    // MOve the unit to the given world position
    private IEnumerator Move(Vector3 targetPos, Vector2Int tilePos)
    {
        float elapsedTime = 0;
        Vector3 startingPos = transform.position;
        while (elapsedTime < stepDuration)
        {
            transform.position = Vector3.Lerp(startingPos, targetPos, elapsedTime / stepDuration);
            //CmdChangePosition(gameObject, Vector3.Lerp(startingPos, targetPos, elapsedTime / stepDuration), tilePos);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        transform.position = targetPos;
        //CmdChangePosition(gameObject, targetPos, tilePos);
    }
    
    [Command(requiresAuthority = false)]
    private void CmdChangePosition(GameObject go, Vector3 position, Vector2Int tilePos)
    {
        RPCChangePosition(go, position, tilePos);
    }

    [Command(requiresAuthority = false)]
    private void CmdStep(MoveCommand moveCommand)
    {
        RPCStep(moveCommand);
    }
    
    [ClientRpc]
    private void RPCChangePosition(GameObject go, Vector3 position, Vector2Int tilePos)
    {
        go.transform.position = position;
        TilePosition = tilePos;
    }
    
    [ClientRpc]
    private void RPCStep(MoveCommand moveCommand)
    {
        StartCoroutine(MoveToPositions(moveCommand));
        TilePosition = moveCommand.TargetPosition;
    }
}
