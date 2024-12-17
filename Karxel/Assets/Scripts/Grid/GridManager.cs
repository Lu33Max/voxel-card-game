using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
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

    [Header("Highlighting")]
    [SerializeField] private Transform highlightParent;
    [SerializeField] private GameObject moveTileHighlighter;
    [SerializeField] private float highlightHoverHeight = 0.001f;
    
    [Header("Temporary Unit Setup")]
    [SerializeField] private Transform unitParent;
    [SerializeField] private GameObject pawnUnit;

    private Dictionary<Vector2Int, TileData> _tiles = new();

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
        SetupUnits();
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

        _tiles[startTile].Unit = null;
        _tiles[targetTile].Unit = unit;
    }

    /// <summary>Shows or hides the highlights to display move</summary>
    /// <param name="moves"></param>
    /// <param name="shouldHighlight"></param>
    public void HighlightMoveTiles(List<MoveCommand> moves, bool shouldHighlight)
    {
        foreach (var move in moves)
        {
            var tile = GetTileAtGridPosition(move.TargetPosition);
            tile.Highlight.SetActive(shouldHighlight);
        }
    }

    // Calculates all tile positions of the board by doing a raycast at each one of them
    // With this method, even uneven play fields can be correctly mapped
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

                var highlighter = Instantiate(moveTileHighlighter, highlightParent);
                highlighter.transform.position = tile.GetWorldPosition(highlightHoverHeight);
                highlighter.SetActive(false);

                tile.Highlight = highlighter;
            }
        }
    }

    // TODO: Replace later with correct spawn logic
    private void SetupUnits()
    {
        foreach (var tile in _tiles.Keys.Where(v => v.x == 0 || v.x == gridSizeX - 1))
        {
            var newPiece = Instantiate(pawnUnit, unitParent);
            var unit = newPiece.GetComponent<Unit>();
        
            unit.MoveToTile(tile);
            _tiles[tile].Unit = unit;   
        }
    }
}
