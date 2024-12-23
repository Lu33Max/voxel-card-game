using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.PlayerLoop;

public abstract class Unit : NetworkBehaviour
{
    [HideInInspector, SyncVar] public Team owningTeam;
    [HideInInspector, SyncVar] public bool isControlled;
    
    [SerializeField] protected UnitData data;
    
    public Vector2Int TilePosition { get; private set; }
    protected List<MoveCommand> MoveIntent { get; } = new();
    public List<Attack> AttackIntent { get; } = new();

    [SyncVar] private int _currentHealth;

    /// <summary>Get all tiles currently reachable by the unit. Only includes valid moves.</summary>
    /// <param name="movementRange">The movement range given by the played card</param>
    public abstract List<MoveCommand> GetValidMoves(int movementRange);

    /// <summary>Get all tiles that would be effected by an attack. Only includes valid tiles.</summary>
    /// <param name="attackRange">The attack range given by the played card</param>
    /// <param name="damageMultiplier">The damage multiplier given by the played card</param>
    /// <param name="hoveredPosition">Mouse position on the board</param>
    /// <param name="previousPosition">Previous Mouse position on the board</param>
    /// <param name="shouldBreak">Whether the method should return early in case of the same calc results</param>
    /// <param name="hasChanged">Whether the effected tiles have changed because of mouse movements compared to the last calculation</param>
    public abstract Attack GetValidAttackTiles(int attackRange, int damageMultiplier, Vector3 hoveredPosition,
        Vector3 previousPosition, bool shouldBreak, out bool hasChanged);

    private void Start()
    {
        if(!isServer)
            return;
        
        UpdateHealth(data.health);
        GameManager.AttackExecuted.AddListener(OnAttackExecuted);
        GameManager.CheckHealth.AddListener(OnCheckHealth);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        
        if(!isServer)
            return;
        
        GameManager.AttackExecuted.RemoveListener(OnAttackExecuted);
        GameManager.CheckHealth.RemoveListener(OnCheckHealth);
    }

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
        
        GameManager.Instance.CmdUnitMovementDone();
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

    /// <summary>Used to play animations, sfx etc.</summary>
    protected virtual IEnumerator Attack(Attack attack)
    {
        yield return new WaitForSeconds(2);
        GameManager.Instance.CmdUnitAttackDone();
    }

    [Server]
    private void OnAttackExecuted(Attack attack)
    {
        if(!attack.Tiles.Contains(TilePosition))
            return;
        
        UpdateHealth(-attack.Damage);
    }
    
    [Server]
    private void UpdateHealth(int changeAmount)
    {
        _currentHealth = Mathf.Clamp(_currentHealth + changeAmount, 0, data.health);
    }

    [Server]
    private void OnCheckHealth()
    {
        if(_currentHealth <= 0)
            Die();
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
    public void CmdRegisterMoveIntent(MoveCommand moveCommand)
    {
        PathManager.Instance.CreatePath(moveCommand,
            MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition, TilePosition);

        GameManager.Instance.RegisterMoveIntent(TilePosition, moveCommand);
        RPCAddToMoveIntent(moveCommand);
    }

    [Command(requiresAuthority = false)]
    public void CmdRegisterAttackIntent(Attack newAttack)
    {
        GameManager.Instance.RegisterAttackIntent(TilePosition, newAttack, owningTeam);
        RPCAddToAttackIntent(newAttack);
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateControlStatus(bool newState)
    {
        isControlled = newState;
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
    }

    [ClientRpc]
    public void RPCCleanUp()
    {
        MoveIntent.Clear();
        AttackIntent.Clear();
    }

    [ClientRpc]
    private void RPCAddToMoveIntent(MoveCommand moveCommand)
    {
        MoveIntent.Add(moveCommand);
    }

    [ClientRpc]
    private void RPCAddToAttackIntent(Attack newAttack)
    {
        AttackIntent.Add(newAttack);
    }

    [ClientRpc]
    public void RPCExecuteAttack(Attack attack)
    {
        StartCoroutine(Attack(attack));
    }

    [ClientRpc]
    protected virtual void Die()
    {
        GameManager.Instance.UnitDefeated(TilePosition);
        GridManager.Instance.RemoveUnit(TilePosition);
        Destroy(gameObject);
    }

    #endregion
}
