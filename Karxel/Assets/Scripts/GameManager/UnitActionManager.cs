using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class UnitActionManager : NetworkSingleton<UnitActionManager>
{
    /// <summary>
    ///     Stores all validated and globally registered moves for the current round. <br/> Only exists on the server
    /// </summary>
    private Dictionary<Vector3Int, List<MoveCommand>> _moveIntents = new();
    /// <summary>
    ///     Stores all validated and globally registered attacks for the current round. <br/> Only exists on the server
    /// </summary>
    private Dictionary<Vector3Int, List<Attack>> _attackIntents = new();

    private void Start()
    {
        if (isServer) 
            GameManager.Instance.GameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        if(isServer && NetworkServer.active) 
            GameManager.Instance.GameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if(newState is not GameState.Attack and not GameState.Movement) return;
        
        _moveIntents.Clear();
        _attackIntents.Clear();
    }

    /// <summary>
    ///     Used to validate a given <see cref="MoveCommand"/> before broadcasting it to other clients of the team
    /// </summary>
    /// <param name="sender"> The client netId that tries to register the intent </param>
    /// <param name="unitPosition"> The grid position of that unit this intent comes from </param>
    /// <param name="moveToRegister"> The move that should be registered </param>
    /// <param name="usedCard"> The card that was used on the unit to execute the move </param>
    [Command(requiresAuthority = false)]
    public void CmdTryRegisterMoveIntent(uint sender, Vector3Int unitPosition, MoveCommand moveToRegister, 
        int usedCard)
    {
        var senderConnection = NetworkServer.connections.Select(c => c.Value).First(c => c.identity.netId == sender);
        var senderTeam = senderConnection.identity.GetComponent<Player>().team;
        
        if (!UnitExistsAtTile(unitPosition, out var unit) || unit == null || !IsValidMoveCommand(unit!, moveToRegister, usedCard) || 
            (_moveIntents.ContainsKey(unitPosition) && _moveIntents[unitPosition].Count >= unit!.Data.moveAmount)) 
        {
            Debug.Log("Execute undo");
            Debug.Log(UnitExistsAtTile(unitPosition, out _));
            Debug.Log(unit != null);
            Debug.Log(IsValidMoveCommand(unit!, moveToRegister, usedCard));
            Debug.Log(_moveIntents.ContainsKey(unitPosition) && _moveIntents[unitPosition].Count < unit!.Data.moveAmount);
            
            if (unit == null) return;

            unit.TargetUndoLocallyRegisteredMove(senderConnection);
            unit.isControlled = false;
            return;
        }
        
        if(!_moveIntents.TryAdd(unitPosition, new List<MoveCommand>{ moveToRegister }))
            _moveIntents[unitPosition].Add(moveToRegister);
        
        // Tell the sencer client that its registration was successful
        unit.TargetOnMoveRegisterSuccessful(senderConnection, moveToRegister);
        
        // Tell the other team members about the new intent
        foreach (var connection in NetworkServer.connections.Select(c => c.Value).Where(c => c.identity.netId != sender && c.identity.GetComponent<Player>().team == senderTeam))
        {
            unit.TargetRegisterNewMoveIntent(connection, moveToRegister);
        }
        
        unit.isControlled = false;
    }
    
    /// <summary>
    ///     Used to validate a given <see cref="Attack"/> before broadcasting it to other clients of the team
    /// </summary>
    /// <param name="sender"> The client that tries to register the intent </param>
    /// <param name="unitPosition"> The grid position of that unit this intent comes from </param>
    /// <param name="attackToRegister"> The attack that should be registered </param>
    /// <param name="usedCard"> The card that was used on the unit to execute the attack </param>
    [Command(requiresAuthority = false)]
    public void CmdTryRegisterAttackIntent(NetworkConnectionToClient sender, Vector3Int unitPosition,
        Attack attackToRegister, MoveCardData usedCard)
    {
        
    }

    /// <summary> Checks the <see cref="GridManager"/> whether a unit really does exist on the given tile </summary>
    /// <param name="gridPosition"> The position where the unit should exist </param>
    /// <param name="unit"> The unit found on the given tile. Null only when the return is false </param>
    [Server]
    private static bool UnitExistsAtTile(Vector3Int gridPosition, out Unit? unit)
    {
        GridManager.Instance.IsExistingGridPosition(gridPosition, out var tile);
        unit = tile?.Unit;
        return unit != null;
    }

    /// <summary>
    ///     Checks with unit whether the <see cref="MoveCommand"/> is actually possible with the given <see cref="MoveCardData"/>
    /// </summary>
    /// <param name="unit"> The unit that should execute the move </param>
    /// <param name="command"> The <see cref="MoveCommand"/> to check for its validity </param>
    /// <param name="moveDistance"> The distance multiplier from the used card </param>
    [Server]
    private bool IsValidMoveCommand(Unit unit, MoveCommand command, int moveDistance)
    {
        // Have to use the registered _moveIntents, since _moveIntents on unit don't exist on server version of unit
        return unit.GetValidMoves(moveDistance,
                _moveIntents.TryGetValue(unit.TilePosition, out var intent) ? intent.Last().TargetPosition : null)
            .FirstOrDefault(c => c.TargetPosition == command.TargetPosition) != null;
    }

    /// <summary>
    ///     Checks with unit whether the <see cref="Attack"/> is actually possible with the given <see cref="AttackCardData"/>.
    ///     Validates the targeted tiles and the calculated damage.
    /// </summary>
    /// <param name="unit"> The unit that should execute the attack </param>
    /// <param name="attack"> The <see cref="Attack"/> to check for its validity </param>
    /// <param name="usedCard"> The card that has been played to create the given move </param>
    [Server]
    private static bool IsValidAttack(Unit unit, Attack attack, AttackCardData usedCard)
    {
        var validTiles = unit.GetValidAttackTiles();

        if (attack.Tiles.Any(tile => !validTiles.Contains(tile)))
            return false;

        return attack.Damage == unit.Data.attackDamage * usedCard.damageMultiplier;
    }
}
