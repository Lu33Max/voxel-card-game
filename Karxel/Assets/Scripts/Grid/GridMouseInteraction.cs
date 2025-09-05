using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class GridMouseInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;

    public static UnityEvent<Unit> UnitHovered;
    
    private Camera _mainCamera;
    private TileData _hoveredTile;
    private Unit _selectedUnit;
    
    private bool _hasSubmitted;
    private string _playerId = "";

    private bool _isActive;

    private void Start()
    {
        _mainCamera = Camera.main;

        // Reassign in case any subscribers did not unsubscribe
        UnitHovered = new UnityEvent<Unit>();
    }

    private void OnEnable()
    {
        HandManager.OnCardDeselected += OnCardDeselected;
        GameManager.OnReady += HandleGameManagerReady;
        InputManager.Instance.OnInteract += OnMouseInteraction;
    }

    private void HandleGameManagerReady()
    {
        GameManager.Instance.PlayersReady += OnPlayersReady;
        GameManager.Instance.GameStateChanged += OnStateChanged;
        _isActive = true;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnInteract -= OnMouseInteraction;
        GameManager.OnReady -= HandleGameManagerReady;
        GameManager.Instance.PlayersReady -= OnPlayersReady;
        GameManager.Instance.GameStateChanged -= OnStateChanged;
        HandManager.OnCardDeselected -= OnCardDeselected;
    }

    private void Update()
    {
        if(!_isActive) return;
        
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
        
        _selectedUnit?.MarkerManager.UpdatePreviews(newHoveredTile.TilePosition); // Unit can recalculate the displayed preview tiles; attack previews change depending on hovered tile in move phase
        
        if(_hoveredTile?.Unit != _selectedUnit)
            _hoveredTile?.Unit?.MarkerManager.ClearHoverPreviews();
            
        if(newHoveredTile.Unit != _selectedUnit)
            newHoveredTile.Unit?.MarkerManager.DisplayHoverPreviews(newHoveredTile.TilePosition); // Display preview tiles and update stat display
        
        UnitHovered?.Invoke(newHoveredTile.Unit);
        
        UpdateHoverMarker(newHoveredTile.TilePosition);
        _hoveredTile = newHoveredTile;
    }

    private void CleanupWithNoInteraction()
    {
        if(_hoveredTile == null) return;
            
        _hoveredTile?.Unit?.MarkerManager.ClearHoverPreviews();
        UnitHovered?.Invoke(null);

        MarkerManager.Instance.RemoveMarkerLocal(_hoveredTile!.TilePosition, MarkerType.Hover, _playerId);
        _hoveredTile = null;
    }

    private void OnStateChanged(GameState newState)
    {
        _hoveredTile?.Unit?.MarkerManager.ClearHoverPreviews();
        _selectedUnit?.MarkerManager.ClearHoverPreviews();
        _selectedUnit?.MarkerManager.ClearSelectPreviews();

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
        var gameState = GameManager.Instance.gameState;
        
        // Case 1: Card selected, no unit selected → Play card or select unit
        if (_selectedUnit == null)
        {
            if (cardValues.IsDisposable()) PlaySelectedItemCard(cardValues);
            else SelectHoveredUnit();
            return;
        }
        
        // Case 2: Card selected, unit selected → Move or attack
        switch (gameState)
        {
            case GameState.Movement:
                var moveCommand = _selectedUnit.GetValidMoves(cardValues.movementRange)
                    .FirstOrDefault(move => move.TargetPosition == _hoveredTile.TilePosition);
                
                if (moveCommand == null || !GridManager.Instance.IsMoveValid(moveCommand)) 
                    break;
                
                _selectedUnit.CmdRegisterMoveIntent(moveCommand);
                HandManager.Instance.PlaySelectedCard();
                // ^^ Replace with UnitActionManager.Instance.RegisterMove(_selectedUnit, moveCommand);
                break;
            
            case GameState.Attack:
                var attackCommand =
                    _selectedUnit.GetAttackForHoverPosition(_hoveredTile.TilePosition, cardValues.attackDamage);
                
                if (attackCommand == null) break;
                
                _selectedUnit.CmdRegisterAttackIntent(attackCommand);
                HandManager.Instance.PlaySelectedCard();
                break;
        }

        var shouldRecoverPreview = _hoveredTile.TilePosition == _selectedUnit.TilePosition;
        
        DeselectUnit();
        
        // In case the player is hovering over the same unit just deselected, restore its previews
        if(shouldRecoverPreview)
            _hoveredTile.Unit!.MarkerManager.DisplayHoverPreviews(_hoveredTile.TilePosition);
    }

    /// <summary> Plays the currently selected card and executes its effects depending on the cardType </summary>
    /// <param name="cardValues"> Values of the <see cref="HandManager.SelectedCard"/> in the HandManager </param>
    /// <exception cref="ArgumentException"> The given cardData was no item type </exception>
    private void PlaySelectedItemCard(CardData cardValues)
    {
        if(_hoveredTile?.Unit == null) return;
        
        var player = GameManager.Instance.localPlayer;

        switch (cardValues.cardType)
        {
            case CardType.Stun:
                if (_hoveredTile.Unit.owningTeam == player.team ||
                    _hoveredTile.Unit.HasEffectOfTypeActive(Unit.StatusEffect.Stunned, 2))
                    return;
                _hoveredTile.Unit.CmdAddNewStatusEffect(new Unit.UnitStatus{ Status = Unit.StatusEffect.Stunned, Duration = 2 });
                break;
            
            case CardType.Heal:
                if(_hoveredTile.Unit.owningTeam != player.team)
                    return;
                _hoveredTile.Unit.CmdUpdateHealth(cardValues.otherValue);
                break;
            
            case CardType.Shield:
                if(_hoveredTile.Unit.owningTeam != player.team || 
                   _hoveredTile.Unit.HasEffectOfTypeActive(Unit.StatusEffect.Shielded)) 
                    return;
                _hoveredTile.Unit.CmdAddNewStatusEffect(new Unit.UnitStatus{ Status = Unit.StatusEffect.Shielded, Duration = -1});
                break;
            
            default:
                throw new ArgumentException($"The provided car type {cardValues.cardType} is not a disposable type.");
        }
        
        // Only here if the card action was actually executed
        HandManager.Instance.PlaySelectedCard();
    }

    private void SelectHoveredUnit()
    {
        if (_hoveredTile?.Unit == null || !_hoveredTile.Unit.IsSelectable ||
            _hoveredTile.Unit.owningTeam != GameManager.Instance.localPlayer.team) 
            return;

        _selectedUnit = _hoveredTile.Unit;
        _selectedUnit.CmdUpdateControlStatus(true);
        _selectedUnit.MarkerManager.DisplaySelectedPreviews(_hoveredTile.TilePosition);
    }
    
    private void DeselectUnit()
    {
        if(_selectedUnit == null)
            return;
        
        _selectedUnit.MarkerManager.ClearHoverPreviews();
        _selectedUnit.MarkerManager.ClearSelectPreviews();
        _selectedUnit.CmdUpdateControlStatus(false);
        _selectedUnit = null;
    }
    
    private void OnPlayersReady()
    {
        GameManager.Instance.localPlayer.GetComponent<Player>().turnSubmitted.AddListener(OnTurnSubmitted);
        _playerId = GameManager.Instance.localPlayer.netId.ToString();
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
