using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }
    
    [Header("Grid Setup")]
    [SerializeField, Tooltip("The maximum dimensions of the map measured in tiles")]
    private Vector3Int mapSize;
    [SerializeField, Tooltip("The dimensions of a singular tile in meters")]
    private Vector3 tileSize = new(0.3f, 0.3f, 0.3f);
    [SerializeField, Tooltip("Layermask used by all walkable colliders")] 
    private LayerMask groundLayer;

    [FormerlySerializedAs("highlightHoverHeight")]
    [Header("Highlighting")]
    [SerializeField] private float markerHeight = 0.001f;
    
    [Header("Unit Setup")]
    [SerializeField] private Transform blueParent;
    [SerializeField] private Transform redParent;

    
    private Dictionary<Vector3Int, TileData> _tiles = new();
    private GameObject _map;
    private int _readyPlayers;

    public class GridTile
    {
        public Vector3Int gridPos;
        public Vector3 worldPos;
        public bool walkable;
    }
    public Dictionary<Vector3Int, GridTile> tiles = new();
    
    public void ScanTiles()
    {
        tiles.Clear();

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int z = 0; z < mapSize.z; z++)
            {
                Ray ray = new Ray(
                    new Vector3(x * tileSize.x + tileSize.x / 2, 200, z * tileSize.z + tileSize.z / 2),
                    Vector3.down);

                // ReSharper disable once Unity.PreferNonAllocApi
                RaycastHit[] results = Physics.RaycastAll(ray, 250, groundLayer);

                foreach (var hit in results)
                {
                    Vector3Int gridPos = new Vector3Int(
                        Mathf.RoundToInt(hit.point.x / tileSize.x - tileSize.x / 2),
                        Mathf.RoundToInt(hit.point.y / tileSize.y - tileSize.y / 2),
                        Mathf.RoundToInt(hit.point.z / tileSize.z - tileSize.z / 2)
                    );
                    
                    if(gridPos.x < 0 || gridPos.x > mapSize.x || gridPos.y < 0 || gridPos.y > mapSize.y || gridPos.z < 0 || gridPos.z > mapSize.z) return;

                    tiles.TryAdd(gridPos, new GridTile
                    {
                        gridPos = gridPos,
                        worldPos = hit.point,
                        walkable = true
                    });
                }
            }
        }
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        CalculateTilePositions();
    }

    /// <summary>Retrieve the TileData at the given position in grid coordinates.</summary>
    public TileData GetTileAtGridPosition(Vector3Int tilePosition)
    {
        _tiles.TryGetValue(tilePosition, out TileData tile);
        return tile;
    }

    /// <summary>Retrieve TileData at a world position by converting it to grid tiles.</summary>
    public TileData GetTileAtWorldPosition(Vector3 worldPosition)
    {
        Vector3Int gridPosition = WorldToGridPosition(worldPosition);
        return GetTileAtGridPosition(gridPosition);
    }

    /// <summary>Converts a given world coordinate into a grid coordinate.</summary>
    public Vector3Int WorldToGridPosition(Vector3 worldPosition)
    {
        return new Vector3Int(Mathf.FloorToInt(worldPosition.x / tileSize.x),
            Mathf.FloorToInt(worldPosition.y / tileSize.y), Mathf.FloorToInt(worldPosition.z / tileSize.z));
    }

    /// <summary>Convert a grid position into world coordinates</summary>
    public Vector3? GridToWorldPosition(Vector3Int gridPosition)
    {
        if (!IsValidGridPosition(gridPosition))
            return null;
        
        return _tiles[gridPosition].WorldPosition;
    }

    /// <summary>Returns a list of all positions currently containing references to units</summary>
    public IEnumerable<Vector3Int> GetAllUnitTiles()
    {
        return _tiles.Where(t => t.Value.Unit != null)
            .Select(t => t.Key)
            .ToList();
    }

    /// <summary>Checks if grid position is inside the world boundaries.</summary>
    public bool IsValidGridPosition(Vector3Int gridPosition)
    {
        return _tiles.ContainsKey(gridPosition);
    }

    /// <summary>Checks if the given move is possible to do on the current board configuration</summary>
    public bool IsMoveValid(MoveCommand move)
    {
        if (!IsValidGridPosition(move.TargetPosition) || _tiles[move.TargetPosition].Unit != null)
            return false;

        foreach (var tile in move.Path)
        {
            if (!IsValidGridPosition(tile) || _tiles[tile].Unit != null)
                return false;
        }

        return true;
    }

    /// <summary>Transfers a Unit-reference from a start tile to a target tile</summary>
    public void MoveUnit(Vector3Int startTile, Vector3Int targetTile)
    {
        var unit = _tiles[startTile].Unit;
        
        if(unit == null)
            return;
        
        CmdUpdateTileUnit(startTile, null);
        CmdUpdateTileUnit(targetTile, unit);
    }

    /// <summary>Removes the reference to the unit from the given tile</summary>
    public void RemoveUnit(Vector3Int unitPos)
    {
        CmdUpdateTileUnit(unitPos, null);
    }

    // Calculates all tile positions of the board by doing a raycast at each one of them
    // With this method, even uneven play fields can be correctly mapped
    [Client]
    private void CalculateTilePositions()
    {
        for (var x = 0; x < mapSize.x; x++)
        {
            for (var z = 0; z < mapSize.z; z++)
            {
                var ray = new Ray(
                    new Vector3(x * tileSize.x + tileSize.x / 2, 200, z * tileSize.z + tileSize.z / 2),
                    Vector3.down);
                
                // ReSharper disable once Unity.PreferNonAllocApi
                var raycastHits = Physics.RaycastAll(ray, 250, groundLayer);

                foreach (var hit in raycastHits)
                {
                    Vector3Int gridPos = new Vector3Int(
                        Mathf.RoundToInt(hit.point.x / tileSize.x - tileSize.x / 2),
                        Mathf.RoundToInt(hit.point.y / tileSize.y - tileSize.y / 2),
                        Mathf.RoundToInt(hit.point.z / tileSize.z - tileSize.z / 2)
                    );

                    _tiles.TryAdd(gridPos, new TileData
                    {
                        TilePosition = gridPos,
                        WorldPosition = hit.point,
                    });
                    
                    var worldPos = hit.point;
                    worldPos.y += markerHeight;
                    MarkerManager.Instance.RegisterTile(gridPos, worldPos, tileSize);
                }
            }
        }

        CmdSetupReady();
    }

    // Read placed units from the board and write them to the tiles
    [Server]
    private void SetupUnits()
    {
        foreach (var parent in new List<Transform>{blueParent, redParent})
        {
            for(var i = 0; i < parent.transform.childCount; i++)
            {
                var unit = parent.transform.GetChild(i);
                var unitScript = unit.GetComponent<Unit>();

                var unitPosition = unit.position;
                
                Vector3Int gridPos = WorldToGridPosition(unitPosition);
            
                if(!IsValidGridPosition(gridPos))
                    continue;
            
                unit.gameObject.SetActive(true);
                unitScript.MoveToTile(gridPos);

                var newTile = _tiles[gridPos];
                newTile.Unit = unitScript;
                RPCUpdateTile(gridPos, newTile);
            }
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdateTileUnit(Vector3Int gridPos, Unit unit)
    {
        var tile = _tiles[gridPos];
        tile.Unit = unit;
        RPCUpdateTile(gridPos, tile);
    }

    [Command(requiresAuthority = false)]
    private void CmdSetupReady()
    {
        _readyPlayers++;
        
        if(_readyPlayers == NetworkServer.connections.Values.Count)
            SetupUnits();
    }

    [ClientRpc]
    private void RPCUpdateTile(Vector3Int pos, TileData newTile)
    {
        _tiles[pos] = newTile;
    }
}
