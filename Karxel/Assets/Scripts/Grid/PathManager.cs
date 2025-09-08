using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary> Manages the lifecycle of unit-specific path indicators for movement or attacks </summary>
[DisallowMultipleComponent]
public class PathManager : NetworkBehaviour
{
    [SerializeField] private GameObject pathPrefab;

    private static readonly Queue<GameObject> PathPool = new();
    private static Transform _parent;
    
    private readonly Dictionary<Vector3Int, PathRenderer> _activePaths = new();
    private Unit _unit;
    
    public void Setup(Unit parentUnit)
    {
        _unit = parentUnit;

        if (_parent == null)
            _parent = GameObject.Find("Paths").transform;
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        GameManager.Instance.GameStateChanged += OnGameStateChanged;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        GameManager.Instance.GameStateChanged -= OnGameStateChanged;
    }

    [Client]
    public void CreatePathLocally(MoveCommand path, Vector3Int start)
    {
        if (pathPrefab == null) 
            return;
        
        SpawnNewPath(path, start, _unit.TilePosition);
    }

    [Client]
    public void RegeneratePathLocally(List<MoveCommand> path)
    {
        if(!_activePaths.TryGetValue(_unit.TilePosition, out var pathRenderer)) return;

        pathRenderer.RegeneratePath(path, _unit.TilePosition, out var canBeRemoved);

        if (!canBeRemoved) return;
        
        _activePaths[_unit.TilePosition].gameObject.SetActive(false);
        PathPool.Enqueue(_activePaths[_unit.TilePosition].gameObject);
        _activePaths.Remove(_unit.TilePosition);
    }

    private void ClearAllPaths()
    {
        foreach (var go in _activePaths.Values.Select(path => path.gameObject))
        {
            go.SetActive(false);
            PathPool.Enqueue(go);
        }

        _activePaths.Clear();
    }
    
    [Server]
    private void OnGameStateChanged(GameState newState)
    {
        if (newState != GameState.Movement)
            RPCRemoveAllPaths();
    }

    [Client]
    private void SpawnNewPath(MoveCommand moveCommand, Vector3Int start, Vector3Int unitPosition)
    {
        if (_activePaths.TryGetValue(unitPosition, out var path))
        {
            path.AppendToPath(moveCommand);
            return;
        }

        var newPath = PathPool.Count > 0
            ? PathPool.Dequeue()
            : Instantiate(pathPrefab, Vector3.zero, Quaternion.identity, _parent);
        newPath.SetActive(true);
        
        var pathRenderer = newPath.GetComponent<PathRenderer>();
        _activePaths.Add(unitPosition, pathRenderer);
        
        if (pathRenderer != null)
            pathRenderer.DrawPath(moveCommand, start);
    }

    [ClientRpc]
    private void RPCRemoveAllPaths()
    {
        ClearAllPaths();
    }
}
