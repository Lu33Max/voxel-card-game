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
        //TilePosition = tilePos;
        Vector3 worldPos = GridManager.Instance.GridToWorldPosition(tilePos);
        //gameObject.transform.position = worldPos;
        CMDChangePosition(gameObject, worldPos, tilePos);
    }

    /// <summary>Step to a target tile while passing over all the given tiles in the path</summary>
    public void StepToTile(MoveCommand moveCommand)
    {
        //Debug.Log($"Target: {moveCommand.TargetPosition} | Calculated Pos: {GridManager.Instance.GridToWorldPosition(moveCommand.TargetPosition)}");
        
        StartCoroutine(MoveToPositions(moveCommand));
        GridManager.Instance.MoveUnit(TilePosition, moveCommand.TargetPosition);
        //TilePosition = moveCommand.TargetPosition;
    }

    [Command(requiresAuthority = false)]
    public void CMDChangePosition(GameObject go, Vector3 position, Vector2Int tilePos)
    {
        RPCChangePosition(go, position, tilePos);
    }

    [ClientRpc]
    public void RPCChangePosition(GameObject go, Vector3 position, Vector2Int tilePos)
    {
        go.transform.position = position;
        TilePosition = tilePos;
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
            //transform.position = Vector3.Lerp(startingPos, targetPos, elapsedTime / stepDuration);
            CMDChangePosition(gameObject, Vector3.Lerp(startingPos, targetPos, elapsedTime / stepDuration), tilePos);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        //transform.position = targetPos;
        CMDChangePosition(gameObject, targetPos, tilePos);
    }
}
