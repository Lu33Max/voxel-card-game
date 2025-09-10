using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class UnitActionManager : NetworkSingleton<UnitActionManager>
{
    public class DamageHealCount
    {
        public int Damage;
        public int Heal;

        public DamageHealCount(int damage, int heal)
        {
            Damage = damage;
            Heal = heal;
        }
    }
    
    /// <summary> Called whenever all units across all clients have finished their moves </summary>
    public event Action? OnAllUnitActionsDone;
    /// <summary> Notifies the units of the accumulated damage and heal to be received on a given tile </summary>
    public event Action<Vector3Int, DamageHealCount>? OnTileDamaged;
    
    /// <summary>
    ///     Stores all validated and globally registered moves for the current round. <br/> Only exists on the server
    /// </summary>
    private Dictionary<Vector3Int, List<MoveCommand>> _moveIntents = new();
    /// <summary>
    ///     Stores all validated and globally registered attacks for the current round. <br/> Only exists on the server
    /// </summary>
    private Dictionary<Vector3Int, List<Attack>> _attackIntents = new(); // TODO: Clear intents of a unit in case it is defeated during one attack round but still has more attacks left 

    private int _totalUnitActions;
    private int _unitActionsExecuted;
    private int _attackRound;
    
    private void Start()
    {
        if (isServer) 
            GameManager.Instance.GameStateChanged += HandleGameStateChanged;
    }

    private void Update()
    {
        // Whenever there are outstanding actions, check every frame whether they are already done in case a client disconnected
        if(_totalUnitActions > 0 && AllUnitActionsDone())
            HandleAllActionsDone();
    }

    private void OnDisable()
    {
        if(isServer && NetworkServer.active) 
            GameManager.Instance.GameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.MovementExecution) ExecuteMoveIntents();
        else if(newState == GameState.AttackExecution) ExecuteAttackIntents();
    }

    /// <summary>
    ///     Used to validate a given <see cref="MoveCommand"/> before broadcasting it to other clients of the team
    /// </summary>
    /// <param name="sender"> The client netId that tries to register the intent </param>
    /// <param name="senderTeam"> Team of the client sending the request </param>
    /// <param name="unitPosition"> The grid position of that unit this intent comes from </param>
    /// <param name="moveToRegister"> The move that should be registered </param>
    /// <param name="cardMoveDistance"> The card that was used on the unit to execute the move </param>
    [Command(requiresAuthority = false)]
    public void CmdTryRegisterMoveIntent(uint sender, Team senderTeam, Vector3Int unitPosition, 
        MoveCommand moveToRegister, int cardMoveDistance)
    {
        var senderConnection = NetworkServer.connections.Select(c => c.Value).First(c => c.identity.netId == sender);
        
        if (!UnitExistsAtTile(unitPosition, out var unit) || unit == null || !IsValidMoveCommand(unit!, moveToRegister, cardMoveDistance) || 
            (_moveIntents.ContainsKey(unitPosition) && _moveIntents[unitPosition].Count >= unit!.Data.moveAmount)) 
        {
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
        GameManager.Instance.CallRpcOnTeam(conn => unit.TargetRegisterNewMoveIntent(conn, moveToRegister), senderTeam, sender);
        
        unit.isControlled = false;
    }

    /// <summary>
    ///     Used to validate a given <see cref="Attack"/> before broadcasting it to other clients of the team
    /// </summary>
    /// <param name="sender"> The client that tries to register the intent </param>
    /// <param name="senderTeam"> Team of the client sending the request </param>
    /// <param name="unitPosition"> The grid position of that unit this intent comes from </param>
    /// <param name="attackToRegister"> The attack that should be registered </param>
    /// <param name="cardDamageMultiplier"> The card that was used on the unit to execute the attack </param>
    [Command(requiresAuthority = false)]
    public void CmdTryRegisterAttackIntent(uint sender, Team senderTeam, Vector3Int unitPosition,
        Attack attackToRegister, int cardDamageMultiplier)
    {
        var senderConnection = NetworkServer.connections.Select(c => c.Value).First(c => c.identity.netId == sender);
        
        if (!UnitExistsAtTile(unitPosition, out var unit) || unit == null || !IsValidAttack(unit!, attackToRegister, cardDamageMultiplier) || 
            (_attackIntents.ContainsKey(unitPosition) && _attackIntents[unitPosition].Count >= unit.Data.attackAmount)) 
        {
            if (unit == null) return;

            unit.TargetUndoLocallyRegisteredAttack(senderConnection);
            unit.isControlled = false;
            return;
        }
        
        if(!_attackIntents.TryAdd(unitPosition, new List<Attack>{ attackToRegister }))
            _attackIntents[unitPosition].Add(attackToRegister);
        
        // Tell the sencer client that its registration was successful
        unit.TargetOnAttackRegisterSuccessful(senderConnection, attackToRegister);
        
        // Tell the other team members about the new intent
        GameManager.Instance.CallRpcOnTeam(conn => unit.TargetRegisterNewAttackIntent(conn, attackToRegister),
            senderTeam, sender);
        
        // Make the targeted tiles show up for each client
        foreach (var tile in attackToRegister.Tiles)
            MarkerManager.Instance.RPCAddMarker(tile, new MarkerData
            {
                Type = MarkerType.Attack,
                Priority = 1,
                Visibility = senderTeam == Team.Blue ? "Blue" : "Red"
            });
        
        unit.isControlled = false;
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
    /// <param name="damageMultiplier"> The damage multiplier from the used card </param>
    [Server]
    private static bool IsValidAttack(Unit unit, Attack attack, int damageMultiplier)
    {
        var validTiles = unit.GetValidAttackTiles();
        
        if (attack.Tiles.Any(tile => !validTiles.Contains(tile)))
            return false;
        
        return attack.Damage == unit.Data.attackDamage * damageMultiplier;
    }
    
    [Server]
    private void ExecuteMoveIntents()
    {
        // Combine all moveCommands for every unit
        var intendedMoves =
            _moveIntents.ToDictionary(intent => intent.Key, intent => new MoveCommand
            {
                TargetPosition = intent.Value.Last().TargetPosition,
                Path = intent.Value.SelectMany((m, index) => m.Path.Concat(index < intent.Value.Count - 1
                        ? new[] { m.TargetPosition }
                        : Enumerable.Empty<Vector3Int>()))
                    .ToList()
            });

        // Static units are all units minus the ones with move intents
        var staticUnits = GridManager.Instance.GetAllUnitTiles().Except(intendedMoves.Keys).ToList();
        Dictionary<Vector3Int, MoveCommand> actualMoves = new();

        var i = 0;
        while (intendedMoves.Count > 0)
        {
            // Remove all moves that have already ended
            foreach (var move in intendedMoves.ToList().Where(move => move.Value.Path.Count < i))
            {
                staticUnits.Add(move.Value.TargetPosition);
                intendedMoves.Remove(move.Key);
            }

            // Needs to be extra loop so that all units that would need to stop because of a collision with other
            // moving units can get to the static units
            foreach (var move in intendedMoves.ToList())
            {
                var currentTarget = move.Value.Path.Count == i ? move.Value.TargetPosition : move.Value.Path[i];

                var unitsWithSameIntent = intendedMoves.Where(m =>
                    m.Key != move.Key && m.Value.Path.Count >= i && (m.Value.Path.Count == i
                        ? m.Value.TargetPosition == currentTarget
                        : m.Value.Path[i] == currentTarget)).ToList();
                
                // If no other unit wants to move to this tile in the turn add the tile to the actual path
                if (unitsWithSameIntent.Count == 0)
                    continue;

                // If x units want to move to the same tile
                intendedMoves.Remove(move.Key);
                staticUnits.Add(i > 0 ? actualMoves[move.Key].TargetPosition : move.Key);

                if (i == 0)
                    actualMoves.Add(move.Key, new MoveCommand
                    {
                        Path = new List<Vector3Int>(), TargetPosition = move.Key,
                        BlockedPosition = move.Value.Path.Count > i ? move.Value.Path[i] : move.Value.TargetPosition
                    });
                else
                    actualMoves[move.Key].BlockedPosition =
                        move.Value.Path.Count > i ? move.Value.Path[i] : move.Value.TargetPosition;

                foreach (var otherMove in unitsWithSameIntent)
                {
                    intendedMoves.Remove(otherMove.Key);
                    staticUnits.Add(i > 0 ? actualMoves[otherMove.Key].TargetPosition : otherMove.Key);
                    
                    if (i == 0)
                        actualMoves.Add(otherMove.Key, new MoveCommand
                        {
                            Path = new List<Vector3Int>(), TargetPosition = otherMove.Key,
                            BlockedPosition = otherMove.Value.Path.Count > i ? otherMove.Value.Path[i] : otherMove.Value.TargetPosition
                        });
                    else
                        actualMoves[otherMove.Key].BlockedPosition =
                            otherMove.Value.Path.Count > i ? otherMove.Value.Path[i] : otherMove.Value.TargetPosition;
                }
                
            }

            var addedStatics = true;

            while (addedStatics)
            {
                addedStatics = false;

                // Needs to be in separate loop in front so that all blocked units are already added to the static units
                foreach (var move in intendedMoves.ToList())
                {
                    var currentTarget = move.Value.Path.Count == i ? move.Value.TargetPosition : move.Value.Path[i];

                    // If a static unit is the next position, stop the movement and add self as static unit
                    if (!staticUnits.Contains(currentTarget))
                        continue;
                    
                    intendedMoves.Remove(move.Key);
                    staticUnits.Add(i > 0 ? actualMoves[move.Key].TargetPosition : move.Key);
                    
                    if (i == 0)
                        actualMoves.Add(move.Key, new MoveCommand
                        {
                            Path = new(), TargetPosition = move.Key,
                            BlockedPosition = move.Value.Path.Count > i ? move.Value.Path[i] : move.Value.TargetPosition
                        });
                    else
                        actualMoves[move.Key].BlockedPosition =
                            move.Value.Path.Count > i ? move.Value.Path[i] : move.Value.TargetPosition;
                    
                    addedStatics = true;
                }
            }

            // All the moves that will neither interfere with other intents nor run into static pieces
            foreach (var move in intendedMoves.ToList())
            {
                var currentTarget = move.Value.Path.Count == i ? move.Value.TargetPosition : move.Value.Path[i];
                
                if (i == 0)
                    actualMoves.Add(move.Key, new MoveCommand { TargetPosition = currentTarget, Path = new() });
                else
                    actualMoves[move.Key] = new MoveCommand
                        { TargetPosition = currentTarget, Path = move.Value.Path.GetRange(0, i) };
            }

            i++;
        }

        // Execute all build moves
        foreach (var intent in actualMoves)
        {
            var unit = GridManager.Instance.GetTileAtGridPosition(intent.Key)?.Unit;

            if (unit == null)
                continue;

            GridManager.Instance.MoveUnit(intent.Key, intent.Value.TargetPosition);
            unit.RPCStep(intent.Value);
        }
        
        _totalUnitActions = actualMoves.Count;
        _moveIntents.Clear();

        if (_totalUnitActions == 0)
            OnAllUnitActionsDone?.Invoke();
    }
    
    /// <summary> Starts executing all attacks with the first round </summary>
    [Server]
    private void ExecuteAttackIntents()
    {
        _attackRound = 0;
        
        // Hide all previous local tiles
        MarkerManager.Instance.RPCClearAllMarkers();
        ExecuteCurrentAttackRound();
    }
    
    /// <summary> Executes attacks round by round, stopping whenever no unit has an intent registered for the round </summary>
    [Server]
    private void ExecuteCurrentAttackRound()
    {
        // Hide all previous' rounds attack tiles
        foreach (var tile in from attacks in _attackIntents.Values from attack in attacks from tile in attack.Tiles select tile)
            MarkerManager.Instance.RPCRemoveMarker(tile, MarkerType.Attack, "All");
        
        var attacksToExecute = _attackIntents.Where(a => a.Value.Count > _attackRound).ToArray();
        
        // Stop execution whenever there are no more attacks left
        if (!attacksToExecute.Any())
        {
            _attackIntents.Clear();
            OnAllUnitActionsDone?.Invoke();
            return;
        }

        // Stores the sum of all damage and healing to be received on the given tile
        Dictionary<Vector3Int, DamageHealCount> accumulatedDamage = new();
        
        // Shorty display every attacked tile, play attack animations and call events for every hit target
        foreach (var attackIntent in attacksToExecute)
        {
            var currentAttack = attackIntent.Value[_attackRound];

            foreach (var tile in currentAttack.Tiles)
            {
                if (!accumulatedDamage.TryAdd(tile, new DamageHealCount(
                        currentAttack.Damage > 0 ? currentAttack.Damage : 0,
                        currentAttack.Damage < 0 ? currentAttack.Damage : 0)))
                {
                    accumulatedDamage[tile].Damage += currentAttack.Damage > 0 ? currentAttack.Damage : 0;
                    accumulatedDamage[tile].Heal += currentAttack.Damage < 0 ? currentAttack.Damage : 0;
                }
                
                MarkerManager.Instance.RPCAddMarker(tile, new MarkerData
                {
                    Type = MarkerType.Attack,
                    Priority = 1,
                    Visibility = "All"
                });   
            }
            
            _totalUnitActions = attacksToExecute.Length;
            
            var unit = GridManager.Instance.GetTileAtGridPosition(attackIntent.Key)?.Unit;
            if (unit != null) unit.RPCExecuteAttack(currentAttack);
        }

        foreach (var damageHealCount in accumulatedDamage)
            OnTileDamaged?.Invoke(damageHealCount.Key, damageHealCount.Value);
    }
    
    /// <summary> Called by clients whenever a unit has executed its action locally </summary>
    [Command(requiresAuthority = false)]
    public void CmdUnitActionDone()
    {
        _unitActionsExecuted++;
        if (_totalUnitActions > 0 && AllUnitActionsDone()) HandleAllActionsDone();
    }

    /// <summary> Called whenever all attacks or moves have been executed on all clients </summary>
    private void HandleAllActionsDone()
    {
        if(GameManager.Instance.gameState == GameState.MovementExecution)
        {
            OnAllUnitActionsDone?.Invoke();
        }
        else if (GameManager.Instance.gameState == GameState.AttackExecution)
        {
            _attackRound++;
            ExecuteCurrentAttackRound();
        }
    }

    /// <summary> Returns whether all units across all clients have executed their actions </summary>
    [Server]
    public bool AllUnitActionsDone()
    {
        if (_unitActionsExecuted < _totalUnitActions * NetworkServer.connections.Count)
            return false;

        _totalUnitActions = 0;
        _unitActionsExecuted = 0;
        return true;
    }
}
