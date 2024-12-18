using System;
using System.Linq;
using UnityEngine;

public class GridMouseInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Hover Highlight")]
    [SerializeField] private GameObject highlightMarker;
    [SerializeField] private float groundDistance = 0.01f;
    
    private Camera _mainCamera;
    private TileData _hoveredTile;
    private Unit _selectedUnit;

    private bool _isHovering;

    private void Start()
    {
        _mainCamera = Camera.main;
        highlightMarker.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
    }

    private void Update()
    {
        CheckForHoveredTile();
        UpdateHighlighter();
    }

    // Gets the currently hovered tile and checks for player interaction in case of a mouse click
    private void CheckForHoveredTile()
    {
        if (Physics.Raycast(_mainCamera.ScreenPointToRay(Input.mousePosition), out var hit, groundLayer))
        {
            Vector2 worldPosition = new Vector2(hit.point.x, hit.point.z);
            Vector2Int hoveredPosition = GridManager.Instance.WorldToGridPosition(worldPosition);

            // Check if the hovered position is one of the tiles
            bool inGrid = GridManager.Instance.IsValidGridPosition(hoveredPosition);
            if (!inGrid)
            {
                _selectedUnit = null;
                _isHovering = false;
                return;
            }

            // Check if a tile exists at the given position
            TileData tile = GridManager.Instance.GetTileAtWorldPosition(new Vector2(hit.point.x, hit.point.z));

            _isHovering = true;
            _hoveredTile = tile;
            
            CheckForMouseInteraction();
        }
        else
        {
            _isHovering = false;
        }
    }

    // Used to handle the unit selection and highlight the reachable tiles
    private void CheckForMouseInteraction()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_selectedUnit == null)
            {
                if(_hoveredTile.Unit == null)
                    return;
                    
                _selectedUnit = _hoveredTile.Unit;
                    
                var moves = _selectedUnit.GetValidMoves(2);
                GridManager.Instance.HighlightMoveTiles(moves, true);

                return;
            }

            // TODO: Replace with cards movement Range
            var moveCommand = _selectedUnit.GetValidMoves(2)
                .Where(move => move.TargetPosition == _hoveredTile.Position).FirstOrDefault();
                
            if (moveCommand != null && GridManager.Instance.IsMoveValid(moveCommand))
            {
                var moves = _selectedUnit.GetValidMoves(2);
                GridManager.Instance.HighlightMoveTiles(moves, false);
                    
                _selectedUnit.StepToTile(moveCommand);
                _selectedUnit = null;
            }
            else
            {
                var moves = _selectedUnit.GetValidMoves(2);
                GridManager.Instance.HighlightMoveTiles(moves, false);
                    
                _selectedUnit = null;
            }
        }
    }

    // Updates the position of the hover highlight
    private void UpdateHighlighter()
    {
        if (_isHovering)
        {
            highlightMarker.SetActive(true);
            highlightMarker.transform.position = _hoveredTile.GetWorldPosition(groundDistance);
        }
        else
        {
            highlightMarker.SetActive(false);
        }
    }
}
