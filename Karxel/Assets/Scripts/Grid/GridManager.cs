using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private int gridSizeX;
    [SerializeField] private int gridSizeY;
    [SerializeField] private float gridResolution;
    
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
    /// <param name="tilePosition">Grid position of the tile.</param>
    /// <returns></returns>
    public TileData GetTileAtPosition(Vector2Int tilePosition)
    {
        _tiles.TryGetValue(tilePosition, out TileData tile);
        return tile;
    }

    /// <summary>Retrieve TileData at a world position by converting it to grid tiles.</summary>
    /// <param name="worldPosition">Absolute tile world position</param>
    /// <returns></returns>
    public TileData GetTileAtWorldPosition(Vector2 worldPosition)
    {
        Vector2Int gridPosition = WorldToGridPosition(worldPosition);
        return GetTileAtPosition(gridPosition);
    }

    /// <summary>Converts a given world coordinate into a grid coordinate.</summary>
    /// <param name="worldPosition">Position in world coordinates</param>
    /// <returns></returns>
    public Vector2Int WorldToGridPosition(Vector2 worldPosition)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPosition.x / gridResolution), Mathf.FloorToInt(worldPosition.y / gridResolution));
    }

    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridSizeX && gridPosition.y >= 0 && gridPosition.y < gridSizeY;
    }

    private void CalculateTilePositions()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
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
