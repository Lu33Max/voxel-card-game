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
    [SerializeField, Tooltip("Max number of tiles in x-Direction")] 
    private int gridSizeX = 10;
    [SerializeField, Tooltip("Max number of tiles in z-Direction")] 
    private int gridSizeZ = 10;
    [SerializeField, Tooltip("Edge length of a single tile")] 
    private float gridResolution = 1;
    [SerializeField, Tooltip("Height of a single map layer")]
    private float layerHeight = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    [FormerlySerializedAs("highlightHoverHeight")]
    [Header("Highlighting")]
    [SerializeField] private float markerHeight = 0.001f;
    
    [Header("Unit Setup")]
    [SerializeField] private Transform blueParent;
    [SerializeField] private Transform redParent;

    
    private Dictionary<Vector2Int, TileData> _tiles = new();
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

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeZ; y++)
            {
                Ray ray = new Ray(
                    new Vector3(x * gridResolution + gridResolution / 2, 200, y * gridResolution + gridResolution / 2),
                    Vector3.down);

                // ReSharper disable once Unity.PreferNonAllocApi
                RaycastHit[] results = Physics.RaycastAll(ray, 250, groundLayer);

                foreach (var hit in results)
                {
                    Vector3Int gridPos = new Vector3Int(
                        Mathf.RoundToInt(hit.point.x / gridResolution - gridResolution / 2),
                        Mathf.RoundToInt(hit.point.y / gridResolution - gridResolution / 2),
                        Mathf.RoundToInt(hit.point.z / gridResolution - gridResolution / 2)
                    );

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
    public TileData GetTileAtGridPosition(Vector2Int tilePosition)
    {
        _tiles.TryGetValue(tilePosition, out TileData tile);
        return tile;
    }

    /// <summary>Retrieve TileData at a world position by converting it to grid tiles.</summary>
    public TileData GetTileAtWorldPosition(Vector2 worldPosition)
    {
        Vector2Int gridPosition = WorldToGridPosition(worldPosition);
        return GetTileAtGridPosition(gridPosition);
    }

    /// <summary>Converts a given world coordinate into a grid coordinate.</summary>
    public Vector2Int WorldToGridPosition(Vector2 worldPosition)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPosition.x / gridResolution),
            Mathf.FloorToInt(worldPosition.y / gridResolution));
    }

    /// <summary>Convert a grid position into world coordinates</summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        if (!IsValidGridPosition(gridPosition))
            return Vector3.zero;
        
        return new Vector3(gridPosition.x * gridResolution + gridResolution / 2, _tiles[gridPosition].HeightLayer * layerHeight,
            gridPosition.y * gridResolution + gridResolution / 2);
    }

    /// <summary>Returns a list of all positions currently containing references to units</summary>
    public List<Vector2Int> GetAllUnitTiles()
    {
        return _tiles.Where(t => t.Value.Unit != null)
            .Select(t => t.Key)
            .ToList();
    }

    /// <summary>Checks if grid position is inside the world boundaries.</summary>
    public bool IsValidGridPosition(Vector2Int gridPosition)
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
    public void MoveUnit(Vector2Int startTile, Vector2Int targetTile)
    {
        var unit = _tiles[startTile].Unit;
        
        if(unit == null)
            return;
        
        CmdUpdateTileUnit(startTile, null);
        CmdUpdateTileUnit(targetTile, unit);
    }

    /// <summary>Removes the reference to the unit from the given tile</summary>
    public void RemoveUnit(Vector2Int unitPos)
    {
        CmdUpdateTileUnit(unitPos, null);
    }

    // Calculates all tile positions of the board by doing a raycast at each one of them
    // With this method, even uneven play fields can be correctly mapped
    [Client]
    private void CalculateTilePositions()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeZ; y++)
            {
                Ray ray = new Ray(
                    new Vector3(x * gridResolution + gridResolution / 2, 200, y * gridResolution + gridResolution / 2),
                    Vector3.down);
                
                if (!Physics.Raycast(ray, out RaycastHit hitInfo, 250, groundLayer))
                    continue;
                
                var gridPos = new Vector2Int(x, y);
                var tile = new TileData
                    { Position = gridPos, HeightLayer = Mathf.RoundToInt(hitInfo.point.y / layerHeight) };

                _tiles.Add(gridPos, tile);

                var worldPos = hitInfo.point;
                worldPos.y += markerHeight;
                MarkerManager.Instance.RegisterTile(gridPos, worldPos, gridResolution);
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
            for(int i = 0; i < parent.transform.childCount; i++)
            {
                var unit = parent.transform.GetChild(i);
                var unitScript = unit.GetComponent<Unit>();

                var unitPosition = unit.position;
                Vector2Int gridPos =
                    new Vector2Int(Mathf.RoundToInt((unitPosition.x - (gridResolution / 2)) / gridResolution),
                        Mathf.RoundToInt((unitPosition.z - (gridResolution / 2)) / gridResolution));
            
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
    private void CmdUpdateTileUnit(Vector2Int gridPos, Unit unit)
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
    private void RPCUpdateTile(Vector2Int pos, TileData newTile)
    {
        _tiles[pos] = newTile;
    }
}
