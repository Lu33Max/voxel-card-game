using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PathManager : NetworkBehaviour
{
    public static PathManager Instance { get; private set; }
    
    [SerializeField] private GameObject pathPrefab;
    
    private Dictionary<Vector3Int, PathRenderer> _activePaths = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GameManager.Instance.gameStateChanged.AddListener(OnGameStateChanged);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        GameManager.Instance.gameStateChanged.RemoveListener(OnGameStateChanged);
    }

    [Server]
    public void CreatePath(MoveCommand path, Vector3Int start, Vector3Int unitPosition)
    {
        if (pathPrefab == null) 
            return;
        
        RPCSpawnNewPath(path, start, unitPosition);
    }
    
    public void ClearAllPaths()
    {
        foreach (var path in _activePaths.Values)
            Destroy(path.gameObject);
        
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
        var unit = GridManager.Instance.GetTileAtGridPosition(unitPosition).Unit;
        
        // Do not show the path to players of the opposing team
        if(unit == null || GameManager.Instance.localPlayer.team != unit.owningTeam)
            return;

        if (_activePaths.TryGetValue(unitPosition, out var path))
        {
            path.AppendToPath(moveCommand);
            return;
        }
        
        GameObject newPath = Instantiate(pathPrefab, Vector3.zero, Quaternion.identity, transform);
        PathRenderer pathRenderer = newPath.GetComponent<PathRenderer>();
        
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
