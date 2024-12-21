using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PathManager : NetworkBehaviour
{
    public static PathManager Instance { get; private set; }
    
    [SerializeField] private GameObject pathPrefab;
    
    private List<GameObject> _activePaths = new();
    
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
    public void CreatePath(MoveCommand path, Vector2Int start, Vector2Int unitPosition)
    {
        if (pathPrefab == null) 
            return;
        
        RPCSpawnNewPath(path, start, unitPosition);
    }
    
    public void ClearAllPaths()
    {
        foreach (GameObject path in _activePaths)
            Destroy(path);
        
        _activePaths.Clear();
    }
    
    public void RemovePath(GameObject path)
    {
        if (_activePaths.Contains(path))
        {
            _activePaths.Remove(path);
            Destroy(path);
        }
    }
    
    [Server]
    private void OnGameStateChanged(GameState newState)
    {
        if (newState != GameState.Movement)
            RPCRemoveAllPaths();
    }

    [Command(requiresAuthority = false)]
    private void CmdAddMoveIntent(Unit unit, MoveCommand moveCommand, GameObject newPath)
    {
        unit.RPCAddToMoveIntent(moveCommand, newPath);
    }

    [ClientRpc]
    private void RPCSpawnNewPath(MoveCommand moveCommand, Vector2Int start, Vector2Int unitPosition)
    {
        var unit = GridManager.Instance.GetTileAtGridPosition(unitPosition).Unit;
        
        // Do not show the path to players of the opposing team
        if(unit == null || GameManager.Instance.localPlayer.team != unit.owningTeam)
            return;
        
        GameObject newPath = Instantiate(pathPrefab, Vector3.zero, Quaternion.identity, transform);
        _activePaths.Add(newPath);
        
        PathRenderer pathRenderer = newPath.GetComponent<PathRenderer>();
        
        if (pathRenderer != null)
            pathRenderer.DrawPath(moveCommand, start);
        
        CmdAddMoveIntent(unit, moveCommand, newPath);
    }

    [ClientRpc]
    private void RPCRemoveAllPaths()
    {
        ClearAllPaths();
    }
}
