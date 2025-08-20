using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mirror;
using UnityEditor;
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

    public Vector3 TileSize => tileSize;
    
    private Dictionary<Vector3Int, TileData> _tiles = new();
    private GameObject _map;
    private int _readyPlayers;
    
    #if UNITY_EDITOR
    public Dictionary<Vector3Int, TileData> Tiles => _tiles;
    #endif
    
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
        CmdSetupReady();
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
        if (!IsExistingGridPosition(gridPosition, out _))
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
    public bool IsExistingGridPosition(Vector3Int gridPosition, out TileData tile)
    {
        return _tiles.TryGetValue(gridPosition, out tile);
    }

    /// <summary>Checks if the given move is possible to do on the current board configuration</summary>
    public bool IsMoveValid(MoveCommand move)
    {
        if (!IsExistingGridPosition(move.TargetPosition, out _) || _tiles[move.TargetPosition].Unit != null)
            return false;

        return move.Path.All(tile => IsExistingGridPosition(tile, out _) && _tiles[tile].Unit == null);
    }

    /// <summary>
    ///     Generates an array of neighbour tile positions around the given <paramref name="startPos"/> that comply
    ///     with all given restrictions
    /// </summary>
    /// <param name="startPos"> Position of the tile to retrieve its neighbours from </param>
    /// <param name="onlyMainAxis"> Whether only the four main axis should be checked or all eight </param>
    /// <param name="validEdgeTypes"> All the <see cref="Tile.EdgeType"/>s that can be traversed by the unit </param>
    public IEnumerable<Vector3Int> GetReachableNeighbours(Vector3Int startPos, bool onlyMainAxis, Tile.EdgeType[] validEdgeTypes, TileData.TileState[] validTileStates)
    {
        if (!_tiles.TryGetValue(startPos, out var startTile)) 
            return new Vector3Int[] { };
        ;
        Vector3Int[] mainDirections = { Vector3Int.back, Vector3Int.forward, Vector3Int.left, Vector3Int.right };
        List<Vector3Int> neighbours = new();

        foreach (var dir in mainDirections)
        {
            var targetTiles = startTile.TileNeighbours.Where(t => t.GridPosition.x == startPos.x + dir.x && t.GridPosition.z == startPos.z + dir.z);

            neighbours.AddRange(
                from neighbour in targetTiles
                where validEdgeTypes.Contains(neighbour.EdgeType) 
                where validTileStates.Contains(_tiles[neighbour.GridPosition].State)
                select neighbour.GridPosition);
        }

        if (onlyMainAxis) 
            return neighbours.ToArray();

        Vector3Int[] crossDirections = { new(1, 0, 1), new(1, 0, -1), new(-1, 0, 1), new(-1, 0, -1) };
        
        foreach (var dir in crossDirections)
        {
            var targetTiles = startTile.TileNeighbours.Where(t => t.GridPosition.x == startPos.x + dir.x && t.GridPosition.z == startPos.z + dir.z);

            neighbours.AddRange(
                from neighbour in targetTiles 
                where validEdgeTypes.Contains(neighbour.EdgeType) 
                where validTileStates.Contains(_tiles[neighbour.GridPosition].State)
                select neighbour.GridPosition);
        }

        return neighbours.ToArray();
    }

    /// <summary> Filter all tiles on the board by the given filter function and return them. </summary>
    /// <param name="tileFilter"> Function to filter tiles. True means the tile should be returned </param>
    public IEnumerable<TileData> GetTilesFiltered(Func<TileData, bool> tileFilter)
    {
        return _tiles.Values.Where(tileFilter);
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
    
    #if UNITY_EDITOR
    public void ScanTiles()
    {
        _tiles.Clear();
        CalculateTilePositions();
    }

    public void GenerateNeighbours()
    {
        GenerateTileNeighbours();
    }
    #endif

    // Calculates all tile positions of the board by doing a raycast at each one of them
    // With this method, even uneven play fields can be correctly mapped
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
                    var gridPos = new Vector3Int(
                        Mathf.RoundToInt((hit.point.x - tileSize.x / 2) / tileSize.x),
                        Mathf.RoundToInt((hit.point.y - tileSize.y) / tileSize.y),
                        Mathf.RoundToInt((hit.point.z - tileSize.z / 2) / tileSize.z)
                    );

                    var tileNeighbours = hit.collider.gameObject.GetComponent<Tile>().Neighbours;

                    _tiles.TryAdd(gridPos, new TileData
                    {
                        TilePosition = gridPos,
                        WorldPosition = hit.point,
                        TileNeighbours = tileNeighbours,
                        State = TileData.TileState.Normal
                    });
                    
                    var worldPos = hit.point;
                    worldPos.y += markerHeight;
                    
                    // Exclude Markers from being registered during edit mode
                    if (Application.isPlaying)
                        MarkerManager.Instance.RegisterTile(gridPos, worldPos, tileSize);
                }
            }
        }
    }

    #if UNITY_EDITOR
    private void GenerateTileNeighbours()
    {
        if(_tiles == null) return;
        
        Vector3Int[] directions =
        {
            Vector3Int.back, Vector3Int.forward, Vector3Int.left, Vector3Int.right,
            new (1, 0, 1), new (1, 0, -1), new (-1, 0, 1), new (-1, 0, -1)
        };

        Vector3Int[] heights = { new(0, -1, 0), Vector3Int.zero, new(0, 1, 0) };

        var tileMarkers = FindObjectsOfType<Tile>();
        
        foreach (var tileMarker in tileMarkers)
        {
            tileMarker.ResetNeighbours();
            var tile = GetTileAtWorldPosition(tileMarker.transform.position);

            foreach (var dir in directions)
            {
                foreach (var height in heights)
                {
                    if(!_tiles.TryGetValue(tile.TilePosition + dir + height, out var neighbour)) continue;

                    var newNeighbour = new Tile.TileNeighbour
                        { EdgeType = Tile.EdgeType.None, GridPosition = neighbour.TilePosition };
                    
                    // Already connected to the Tile Component because C# lists are refs
                    tile.TileNeighbours.Add(newNeighbour);
                }
            }
            
            #if UNITY_EDITOR
            EditorUtility.SetDirty(tileMarker);
            #endif
        }
        
        Debug.Log("[GridManager] Done Connection Update");
    }
    #endif

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
                
                var gridPos = WorldToGridPosition(unitPosition);
                gridPos.y -= 1;
                
                if(!IsExistingGridPosition(gridPos, out _))
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
