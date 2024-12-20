using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public enum GameState
{
    Movement,
    Attack,
    MovementExecution,
    AttackExecution
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>SERVER ONLY<br/>List of all moves to execute when round finishes</summary>
    public Dictionary<Vector2Int, List<MoveCommand>> MoveIntents = new();

    public UnityEvent<GameState> GameStateChanged;

    [HideInInspector] public Player localPlayer;
    [HideInInspector] public List<Player> redPlayers; // Server only
    [HideInInspector] public List<Player> bluePlayers; // Server only

    private int _redSubmit;
    private int _blueSubmit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [Command(requiresAuthority = false)]
    public void CmdSubmitTurn(Team team)
    {
        if (team == Team.Blue)
            _blueSubmit++;
        else if (team == Team.Red)
            _redSubmit++;

        if (_blueSubmit == bluePlayers.Count && _redSubmit == redPlayers.Count)
        {
            ExecuteMoveIntents2();
            _blueSubmit = 0;
            _redSubmit = 0;
        }
    }

    [Server]
    private void ExecuteMoveIntents()
    {
        foreach (var intent in MoveIntents)
        {
            var unit = GridManager.Instance.GetTileAtGridPosition(intent.Key).Unit;

            if (unit == null || intent.Value.Count == 0)
                continue;

            unit.RPCStep(intent.Value[0]);
        }

        MoveIntents.Clear();
        RPCInvokeStateUpdate(GameState.Movement);
    }

    [Server]
    private void ExecuteMoveIntents2()
    {
        // Combine all moveCommands for every unit
        Dictionary<Vector2Int, MoveCommand> intendedMoves =
            MoveIntents.ToDictionary(intent => intent.Key, intent => new MoveCommand
            {
                TargetPosition = intent.Value.Last().TargetPosition,
                Path = intent.Value.SelectMany((m, index) => m.Path.Concat(index < intent.Value.Count - 1
                        ? new[] { m.TargetPosition }
                        : Enumerable.Empty<Vector2Int>()))
                    .ToList()
            });

        // Static units are all units minus the ones with move intents
        List<Vector2Int> staticUnits = GridManager.Instance.GetAllUnitTiles().Except(intendedMoves.Keys).ToList();

        Dictionary<Vector2Int, MoveCommand> actualMoves = new();

        int i = 0;
        while (intendedMoves.Count > 0)
        {
            // Remove all moves that have already ended
            foreach (var move in intendedMoves.ToList().Where(move => move.Value.Path.Count < i))
            {
                Debug.Log($"I get removed at the path count check with i {i}");
                staticUnits.Add(move.Value.TargetPosition);
                intendedMoves.Remove(move.Key);
            }

            // Needs to be extra loop so that all units that would need to stop because of a collision with other
            // moving units can get to the static units
            foreach (var move in intendedMoves.ToList())
            {
                var currentTarget = move.Value.Path.Count == i ? move.Value.TargetPosition : move.Value.Path[i];

                var unitsWithSameIntent = intendedMoves.Where(m =>
                    (m.Key != move.Key) && (m.Value.Path.Count >= i) && (m.Value.Path.Count == i
                        ? m.Value.TargetPosition == currentTarget
                        : m.Value.Path[i] == currentTarget)).ToList();

                Debug.Log($"Units with same intent: {unitsWithSameIntent.Count}");
                // If no other unit wants to move to this tile in the turn add the tile to the actual path
                if (unitsWithSameIntent.Count == 0)
                    continue;

                // If x units want to move to the same tile
                Debug.Log($"I get removed at the duplicate units with i {i}");
                intendedMoves.Remove(move.Key);
                staticUnits.Add(i > 0 ? actualMoves[move.Key].TargetPosition : move.Key);

                foreach (var otherMove in unitsWithSameIntent)
                {
                    intendedMoves.Remove(otherMove.Key);
                    staticUnits.Add(i > 0 ? actualMoves[otherMove.Key].TargetPosition : otherMove.Key);
                }

                Debug.Log($"Now I'm only ${intendedMoves.Count} members long");
            }

            bool addedStatics = true;

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

                    Debug.Log($"I get removed at the static units with i {i}");
                    intendedMoves.Remove(move.Key);
                    staticUnits.Add(i > 0 ? actualMoves[move.Key].TargetPosition : move.Key);
                    addedStatics = true;
                }
            }

            // All the moves that will neither interfere with other intents nor run into static pieces
            Debug.Log($"After removing the intendedList still has {intendedMoves.Count} members");
            foreach (var move in intendedMoves.ToList())
            {
                var currentTarget = move.Value.Path.Count == i ? move.Value.TargetPosition : move.Value.Path[i];

                Debug.Log("Move is added to actualMoves");
                if (i == 0)
                    actualMoves.Add(move.Key, new MoveCommand { TargetPosition = currentTarget, Path = new() });
                else
                    actualMoves[move.Key] = new MoveCommand
                        { TargetPosition = currentTarget, Path = move.Value.Path.GetRange(0, i) };
            }

            i++;
        }

        Debug.Log(actualMoves.Count);

        foreach (var intent in actualMoves)
        {
            var unit = GridManager.Instance.GetTileAtGridPosition(intent.Key).Unit;

            if (unit == null)
                continue;

            unit.RPCStep(intent.Value);
        }

        foreach (var unit in MoveIntents
                     .Select(origIntent => GridManager.Instance.GetTileAtGridPosition(origIntent.Key).Unit)
                     .Where(unit => unit != null))
        {
            unit.RPCCleanUp();
        }

        MoveIntents.Clear();
        RPCInvokeStateUpdate(GameState.Movement);
    }

    [ClientRpc]
    private void RPCInvokeStateUpdate(GameState newState)
    {
        GameStateChanged?.Invoke(newState);
    }
}