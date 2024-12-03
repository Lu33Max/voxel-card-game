using System;
using UnityEngine;

public class GridMouseInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    
    private Camera _mainCamera;
    private TileData _hoveredTile;

    private bool _isHovering;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        CheckForHoveredTile();
    }

    private void CheckForHoveredTile()
    {
        if (Physics.Raycast(_mainCamera.ScreenPointToRay(Input.mousePosition), out var hit, groundLayer))
        {
            Vector2 worldPosition = new Vector2(hit.point.x, hit.point.z);
            Vector2Int hoveredPosition = GridManager.Instance.WorldToGridPosition(worldPosition);

            // Check if the hovered position is inside the grid boundaries
            bool inGrid = GridManager.Instance.IsValidGridPosition(hoveredPosition);
            if (!inGrid)
            {
                _isHovering = false;
                return;
            }

            // Check if a tile exists at the given position
            TileData tile = GridManager.Instance.GetTileAtWorldPosition(new Vector2(hit.point.x, hit.point.z));
            if (tile == null)
            {
                _isHovering = false;
                return;
            }

            _isHovering = true;
            _hoveredTile = tile;

            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"Tile clicked at position: {tile.Position}");
            }
        }
        else
        {
            _isHovering = false;
        }
    }
}
