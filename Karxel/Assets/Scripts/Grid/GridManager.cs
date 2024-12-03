using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField, Tooltip("Max number of tiles in x-Direction")] 
    private int gridSizeX;
    [SerializeField, Tooltip("Max number of tiles in z-Direction")] 
    private int gridSizeZ;
    [SerializeField, Tooltip("Edge length of a single tile")] 
    private float gridResolution;
    
    [SerializeField] private LayerMask groundLayer;

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
    public Vector2 GridToWorldPosition(Vector2Int gridPosition)
    {
        return new Vector2(gridPosition.x * gridResolution + gridResolution / 2,
            gridPosition.y * gridResolution + gridResolution / 2);
    }

    /// <summary>Checks if grid position is inside the world boundaries.</summary>
    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridSizeX && gridPosition.y >= 0 && gridPosition.y < gridSizeZ;
    }

    // Calculates all tile positions of the board by doing a raycast at each one of them
    // With this method, even uneven play fields can be correctly mapped
    private void CalculateTilePositions()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeZ; y++)
            {
                Ray ray = new Ray(new Vector3(x * gridResolution, 200, y * gridResolution), Vector3.down);
                
                if (!Physics.Raycast(ray, out RaycastHit hitInfo, 250, groundLayer))
                    continue;

                var gridPos = new Vector2Int(x, y);
                
                _tiles.Add(gridPos, new TileData{ Position = gridPos, Height = hitInfo.point.y });
            }
        }
    }
}
