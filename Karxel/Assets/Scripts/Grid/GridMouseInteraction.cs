using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class GridMouseInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;

    public static event Action<Unit>? UnitHovered;
    
    private Camera _mainCamera = null!;
    private TileData? _hoveredTile;
    private Unit? _selectedUnit;
    
    private bool _hasSubmitted;
    private string _playerId = "";

    private bool _isActive;

    private void Start()
    {
        if (Camera.main == null) throw new NullReferenceException("[GridMouseInteraction] Camera.main was not set");
        
        _mainCamera = Camera.main;
        
        InputManager.Instance.OnInteract += OnMouseInteraction;
        HandManager.OnCardDeselected += OnCardDeselected;
        GameManager.Instance!.PlayersReady += OnPlayersReady;
        GameManager.Instance.GameStateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnInteract -= OnMouseInteraction;
        HandManager.OnCardDeselected -= OnCardDeselected;

        if (!GameManager.Instance) return;
        GameManager.Instance.PlayersReady -= OnPlayersReady;
        GameManager.Instance.GameStateChanged -= OnStateChanged;
    }

    private void Update()
    {
        CheckForHoveredTile();
    }

    /// <summary> Calculate whether grid interactions are valid inside the current game phase </summary>
    private static bool ShouldCheckForInteraction()
    {
        return GameManager.Instance != null && GameManager.Instance.gameState is GameState.Attack or GameState.Movement;
    }

    /// <summary> Checks whether the mouse cursor is currently hovering over the stage </summary>
    /// <param name="hit"> RaycastHit with positional data about the hovered point </param>
    private bool IsInteractingWithStage(out RaycastHit hit)
    {
        hit = new RaycastHit();
        
        return !EventSystem.current.IsPointerOverGameObject() &&
               Physics.Raycast(_mainCamera.ScreenPointToRay(Input.mousePosition), out hit, groundLayer);
    }

    /// <summary> Converts the given RaycastHit into a GridPosition. This position is not guaranteed to be valid. </summary>
    /// <param name="hit"> Hit containing data about the hovered point </param>
    private static Vector3Int GetHoveredTilePosition(RaycastHit hit)
    {
        // Move the hit "into" the block for more accurate grid conversion
        var correctedHoverPosition = hit.point - hit.normal * 0.01f;
        return GridManager.Instance.WorldToGridPosition(correctedHoverPosition);
    }

    private void CheckForHoveredTile()
    {
        if(!ShouldCheckForInteraction())
            return;
        
        if (!IsInteractingWithStage(out var hit) ||
            !GridManager.Instance.IsExistingGridPosition(GetHoveredTilePosition(hit), out var newHoveredTile))
        {
            CleanupWithNoInteraction();
            return;
        }
        
        // If the cursor is over the same tile, do nothing
        if (_hoveredTile?.TilePosition == newHoveredTile.TilePosition)
            return;
        
        _selectedUnit?.UnitMarkerManager.UpdatePreviews(newHoveredTile.TilePosition); // Unit can recalculate the displayed preview tiles; attack previews change depending on hovered tile in move phase
        
        if(_hoveredTile?.Unit != _selectedUnit)
            _hoveredTile?.Unit?.UnitMarkerManager.ClearHoverPreviews();
            
        if(newHoveredTile.Unit != _selectedUnit)
            newHoveredTile.Unit?.UnitMarkerManager.DisplayHoverPreviews(newHoveredTile.TilePosition); // Display preview tiles and update stat display
        
        UnitHovered?.Invoke(newHoveredTile.Unit);
        
        UpdateHoverMarker(newHoveredTile.TilePosition);
        _hoveredTile = newHoveredTile;
    }

    private void CleanupWithNoInteraction()
    {
        if(_hoveredTile == null) return;
            
        _hoveredTile?.Unit?.UnitMarkerManager.ClearHoverPreviews();
        UnitHovered?.Invoke(null);

        MarkerManager.Instance.RemoveMarkerLocal(_hoveredTile!.TilePosition, MarkerType.Hover, _playerId);
        _hoveredTile = null;
    }

    private void OnStateChanged(GameState newState)
    {
        _hoveredTile?.Unit?.UnitMarkerManager.ClearHoverPreviews();
        _selectedUnit?.UnitMarkerManager.ClearHoverPreviews();
        _selectedUnit?.UnitMarkerManager.ClearSelectPreviews();

        _selectedUnit = null;
        _hasSubmitted = false;
    }

    private void UpdateHoverMarker(Vector3Int hoveredPosition)
    {
        if(MarkerManager.Instance == null)
            return;
        
        if (_hoveredTile != null)
            MarkerManager.Instance.RemoveMarkerLocal(_hoveredTile.TilePosition, MarkerType.Hover, _playerId);

        MarkerManager.Instance.AddMarkerLocal(hoveredPosition, new MarkerData
        {
            Type = MarkerType.Hover,
            Priority = 0,
            Visibility = _playerId
        });
    }

    private void OnMouseInteraction()
    {
        // Interaction with units can only occur if a card is currently selected
        if (HandManager.Instance.SelectedCard == null || _hasSubmitted || !ShouldCheckForInteraction())
            return;

        if (_hoveredTile == null)
        {
            DeselectUnit();
            return;
        }
        
        var cardValues = HandManager.Instance.SelectedCard.CardData;
        
        // Case 1: Card selected, no unit selected → Play card or select unit
        if (_selectedUnit == null)
        {
            if (cardValues.IsDisposable() && _hoveredTile.Unit != null) 
                cardValues.TryUseCard(_hoveredTile, null);
            else 
                SelectHoveredUnit();
            return;
        }
        
        // Case 2: Card selected, unit selected → Move or attack
        cardValues.TryUseCard(_hoveredTile, _selectedUnit);

        var shouldRecoverPreview = _hoveredTile.TilePosition == _selectedUnit.TilePosition;
        
        DeselectUnit();
        
        // In case the player is hovering over the same unit just deselected, restore its previews
        if(shouldRecoverPreview)
            _hoveredTile.Unit!.UnitMarkerManager.DisplayHoverPreviews(_hoveredTile.TilePosition);
    }

    private void SelectHoveredUnit()
    {
        if (_hoveredTile?.Unit == null || !_hoveredTile.Unit.IsSelectable ||
            _hoveredTile.Unit.owningTeam != Player.LocalPlayer.team) 
            return;

        _selectedUnit = _hoveredTile.Unit;
        _selectedUnit.CmdUpdateControlStatus(true);
        _selectedUnit.UnitMarkerManager.DisplaySelectedPreviews(_hoveredTile.TilePosition);
    }
    
    private void DeselectUnit()
    {
        if(_selectedUnit == null)
            return;
        
        _selectedUnit.UnitMarkerManager.ClearHoverPreviews();
        _selectedUnit.UnitMarkerManager.ClearSelectPreviews();
        _selectedUnit.CmdUpdateControlStatus(false);
        _selectedUnit = null;
    }
    
    private void OnPlayersReady()
    {
        Player.LocalPlayer.turnSubmitted.AddListener(OnTurnSubmitted);
        _playerId = Player.LocalPlayer.netId.ToString();
    }

    private void OnTurnSubmitted()
    {
        _hasSubmitted = true;
        DeselectUnit();
    }
    
    private void OnCardDeselected()
    {
        DeselectUnit();
    }
}
