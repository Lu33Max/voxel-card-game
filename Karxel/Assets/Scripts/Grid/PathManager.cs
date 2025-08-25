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

    [Server]
    public void CreatePath(MoveCommand path, Vector3Int start)
    {
        if (pathPrefab == null) 
            return;
        
        RPCSpawnNewPath(path, start, _unit.TilePosition);
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

    [ClientRpc]
    private void RPCSpawnNewPath(MoveCommand moveCommand, Vector3Int start, Vector3Int unitPosition)
    {
        // Do not show the path to players of the opposing team
        if(GameManager.Instance.localPlayer.team != _unit.owningTeam)
            return;

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
        _activePaths.Add(start, pathRenderer);
        
        if (pathRenderer != null)
            pathRenderer.DrawPath(moveCommand, start);
    }

    [ClientRpc]
    private void RPCRemoveAllPaths()
    {
        ClearAllPaths();
    }
}
