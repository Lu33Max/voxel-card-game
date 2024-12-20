using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
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
    private List<MoveCommand> _highlightedTiles;

    private void Start()
    {
        _mainCamera = Camera.main;
        highlightMarker.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        GameManager.PlayersReady.AddListener(OnPlayersReady);
    }

    private void OnDestroy()
    {
        GameManager.PlayersReady.RemoveListener(OnPlayersReady);
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
        if (!Input.GetMouseButtonDown(0)) 
            return;
        
        // If the player has no unit selected, try selecting the hovered one and highlight its movement range
        if (_selectedUnit == null)
        {
            if(_hoveredTile.Unit == null || _hoveredTile.Unit.owningTeam != GameManager.Instance.localPlayer.team || _hoveredTile.Unit.isControlled)
                return;
            
            _selectedUnit = _hoveredTile.Unit;
            _selectedUnit.CmdUpdateControlStatus(true);

            _highlightedTiles = _selectedUnit.GetValidMoves(2);
            GridManager.Instance.HighlightMoveTiles(_highlightedTiles, true);
            return;
        }

        // TODO: Replace with cards movement Range
        var moveCommand = _selectedUnit.GetValidMoves(2)
            .Where(move => move.TargetPosition == _hoveredTile.Position).FirstOrDefault();
                
        if (moveCommand != null && GridManager.Instance.IsMoveValid(moveCommand))
            _selectedUnit.CmdAddToMoveIntent(moveCommand);
        
        DeselectUnit();
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

    private void DeselectUnit()
    {
        if(_selectedUnit == null)
            return;
        
        GridManager.Instance.HighlightMoveTiles(_highlightedTiles, false);
        _selectedUnit.CmdUpdateControlStatus(false);

        _highlightedTiles = null;
        _selectedUnit = null;
    }
    
    private void OnPlayersReady()
    {
        GameManager.Instance.localPlayer.GetComponent<Player>().turnSubmitted.AddListener(OnTurnSubmitted);
    }

    private void OnTurnSubmitted()
    {
        DeselectUnit();
    }
}
