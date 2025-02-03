using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridMouseInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    
    private Camera _mainCamera;
    private TileData _hoveredTile;
    private Unit _selectedUnit;
    private UnitStatDisplay _statDisplay;
    
    private Vector3 _prevMousePos;
    private List<MoveCommand> _highlightedMoveTiles = new();
    private List<Vector2Int> _previewTiles = new();
    private Attack _currentAttack;
    
    private bool _hasSubmitted;
    private string _playerId = "";

    private void Start()
    {
        _mainCamera = Camera.main;
        GameManager.PlayersReady.AddListener(OnPlayersReady);
    }

    private void OnDestroy()
    {
        GameManager.PlayersReady.RemoveListener(OnPlayersReady);
        GameManager.Instance.gameStateChanged.RemoveListener(OnGameStateChanged);
        HandManager.Instance.cardDeselected.RemoveListener(OnCardDeselected);
    }

    private void Update()
    {
        CheckForHoveredTile();
    }

    // Gets the currently hovered tile and checks for player interaction in case of a mouse click
    private void CheckForHoveredTile()
    {
        // Checks for correct gamestate, whether the UI is being hovered and whether the board is being hovered
        if (GameManager.Instance.gameState is GameState.Attack or GameState.Movement &&
            !EventSystem.current.IsPointerOverGameObject() &&
            Physics.Raycast(_mainCamera.ScreenPointToRay(Input.mousePosition), out var hit, groundLayer))
        {
            Vector2 worldPosition = new Vector2(hit.point.x, hit.point.z);
            Vector2Int hoveredPosition = GridManager.Instance.WorldToGridPosition(worldPosition);

            // Check if the hovered position is one of the tiles
            bool inGrid = GridManager.Instance.IsValidGridPosition(hoveredPosition);
            if (!inGrid)
            {
                if(_hoveredTile != null)
                    MarkerManager.Instance.RemoveMarkerLocal(_hoveredTile.Position, MarkerType.Hover, _playerId);

                HideUnitDisplay();
                
                if (GameManager.Instance.gameState == GameState.Movement)
                    RemoveAllPreviewTiles();
                
                _hoveredTile = null;
                return;
            }

            // Check if a tile exists at the given position
            TileData newHoveredTile = GridManager.Instance.GetTileAtWorldPosition(new Vector2(hit.point.x, hit.point.z));
            
            // Needs to be checked even if hovered tile has not changed
            if (GameManager.Instance.gameState == GameState.Attack && _selectedUnit != null)
                DisplayAttackTiles(hit.point, _hoveredTile == null);
            
            _prevMousePos = hit.point;

            if (_hoveredTile == null || _hoveredTile.Position != hoveredPosition)
            {
                if (HandManager.Instance != null && HandManager.Instance.SelectedCard != null)
                {
                    if(GameManager.Instance.gameState == GameState.Movement)
                        DisplayMovePreviewTiles(newHoveredTile);
                    else if(GameManager.Instance.gameState == GameState.Attack)
                        DisplayAttackPreviewTiles(newHoveredTile);
                }
                
                UpdateHoverMarker(hoveredPosition);
                _hoveredTile = newHoveredTile;
                
                CheckForUnitHovered();
            }
            
            if(!_hasSubmitted)
                CheckForMouseInteraction();
        }
        // No interaction with the board happening
        else
        {
            if(_currentAttack != null)
                foreach (var tile in _currentAttack.Tiles)
                    MarkerManager.Instance.RemoveMarkerLocal(tile, MarkerType.Attack, _playerId);

            if(!EventSystem.current.IsPointerOverGameObject())
                HideUnitDisplay();
            
            if(_hoveredTile == null)
                return;
            
            if (GameManager.Instance.gameState is GameState.Movement or GameState.Attack)
                RemoveAllPreviewTiles();
            
            MarkerManager.Instance.RemoveMarkerLocal(_hoveredTile.Position, MarkerType.Hover, _playerId);
            _hoveredTile = null;
        }
    }

    private void DisplayMovePreviewTiles(TileData newHoveredTile)
    {
        RemoveAllPreviewTiles();

        if (newHoveredTile == null || newHoveredTile.Unit == null || _selectedUnit != null ||
            newHoveredTile.Unit.owningTeam != GameManager.Instance.localPlayer.team || !newHoveredTile.Unit.CanBeSelected()) 
            return;
        
        var card = HandManager.Instance.SelectedCard;
        
        foreach (var command in newHoveredTile.Unit.GetValidMoves(card.CardData.movementRange))
        {
            MarkerManager.Instance.AddMarkerLocal(command.TargetPosition, new MarkerData
            {
                Type = MarkerType.MovePreview,
                MarkerColor = Color.white,
                Priority = 3,
                Visibility = _playerId
            });
                
            _previewTiles.Add(command.TargetPosition);
        }
    }

    private void DisplayAttackPreviewTiles(TileData newHoveredTile)
    {
        RemoveAllPreviewTiles();
        
        if (newHoveredTile == null || newHoveredTile.Unit == null || _selectedUnit != null ||
            newHoveredTile.Unit.owningTeam != GameManager.Instance.localPlayer.team || !newHoveredTile.Unit.CanBeSelected()) 
            return;
        
        var card = HandManager.Instance.SelectedCard;
        
        foreach (var tile in newHoveredTile.Unit.GetValidAttackTiles(card.CardData.attackRange))
        {
            MarkerManager.Instance.AddMarkerLocal(tile, new MarkerData
            {
                Type = MarkerType.AttackPreview,
                MarkerColor = Color.white,
                Priority = 3,
                Visibility = _playerId
            });
                
            _previewTiles.Add(tile);
        }
    }

    private void RemoveAllPreviewTiles()
    {
        foreach (var tile in _previewTiles)
        {
            MarkerManager.Instance.RemoveMarkerLocal(tile, MarkerType.MovePreview, _playerId);
            MarkerManager.Instance.RemoveMarkerLocal(tile, MarkerType.AttackPreview, _playerId);
        }
    }

    private void UpdateHoverMarker(Vector2Int hoveredPosition)
    {
        if(MarkerManager.Instance == null)
            return;
        
        if (_hoveredTile != null)
            MarkerManager.Instance.RemoveMarkerLocal(_hoveredTile.Position, MarkerType.Hover, _playerId);

        MarkerManager.Instance.AddMarkerLocal(hoveredPosition, new MarkerData
        {
            Type = MarkerType.Hover,
            MarkerColor = Color.cyan,
            Priority = 0,
            Visibility = _playerId
        });
    }

    private void DisplayAttackTiles(Vector3 hit, bool prevNotHovered)
    {
        var cardValues = HandManager.Instance.SelectedCard.CardData;
        var attack = _selectedUnit.GetRotationalAttackTiles(cardValues.attackRange, cardValues.attackDamage, hit,
            _prevMousePos, false, out bool hasChanged);

        if (!hasChanged && !prevNotHovered) return;
        
        foreach (var tile in _currentAttack.Tiles)
            MarkerManager.Instance.RemoveMarkerLocal(tile, MarkerType.Attack, _playerId);

        _currentAttack = attack;

        foreach (var tile in _currentAttack.Tiles)
        {
            MarkerManager.Instance.AddMarkerLocal(tile, new MarkerData
            {
                Type = MarkerType.Attack,
                MarkerColor = Color.red,
                Priority = 1,
                Visibility = _playerId
            });
        }
    }

    private void HideUnitDisplay()
    {
        if(_statDisplay == null)
            return;
        
        _statDisplay.UpdateVisibility(false);
    }

    private void CheckForUnitHovered()
    {
        if(_statDisplay == null)
            return;
        
        if (_hoveredTile.Unit == null)
        {
            HideUnitDisplay();
            return;
        }
        
        _statDisplay.UpdateVisibility(true);
        _statDisplay.UpdateDisplayText(_hoveredTile.Unit.Data);
    }

    // Used to handle the unit selection and highlight the reachable tiles
    private void CheckForMouseInteraction()
    {
        // Interaction with units can only occur if a card is currently selected
        if (!Input.GetMouseButtonDown(0) || HandManager.Instance.SelectedCard == null) 
            return;
        
        var cardValues = HandManager.Instance.SelectedCard.CardData;
        var gameState = GameManager.Instance.gameState;
        var player = GameManager.Instance.localPlayer;
        
        // If the player has no unit selected, try selecting the hovered one and highlight its movement range
        if (_selectedUnit == null)
        {
            if(_hoveredTile.Unit == null || _hoveredTile.Unit.owningTeam != player.team || _hoveredTile.Unit.isControlled)
                return;

            if (gameState == GameState.Movement && cardValues.cardType != CardType.Move)
            {
                if(cardValues.cardType == CardType.Heal)
                    _hoveredTile.Unit.CmdUpdateHealth(cardValues.otherValue);
                
                else if (cardValues.cardType == CardType.Shield)
                    _hoveredTile.Unit.CmdUpdateShield(cardValues.otherValue);
                
                HandManager.Instance.PlaySelectedCard();
                return;
            }
            
            // Check if the unit has no attack registered / is not over the move limit
            if (!_hoveredTile.Unit.CanBeSelected())
                return;

            _selectedUnit = _hoveredTile.Unit;
            _selectedUnit.CmdUpdateControlStatus(true);
            RemoveAllPreviewTiles();

            switch (gameState)
            {
                // Player can only issue an attack command to a unit once per round
                case GameState.Attack:
                    // If the unit was selected this frame it should always generate the highlight tiles
                    _currentAttack = _selectedUnit.GetRotationalAttackTiles(cardValues.attackRange, cardValues.attackDamage,
                        _prevMousePos, _prevMousePos, false, out _);
                    
                    foreach (var tile in _currentAttack.Tiles)
                    {
                        MarkerManager.Instance.AddMarkerLocal(tile, new MarkerData
                        {
                            Type = MarkerType.Attack,
                            MarkerColor = Color.red,
                            Priority = 1,
                            Visibility = _playerId
                        });
                    }
                    break;
                case GameState.Movement:
                    _highlightedMoveTiles = _selectedUnit.GetValidMoves(cardValues.movementRange);
                    
                    // Move to CMD
                    foreach (var command in _highlightedMoveTiles)
                    {
                        MarkerManager.Instance.AddMarkerLocal(command.TargetPosition, new MarkerData
                        {
                            Type = MarkerType.Move,
                            MarkerColor = Color.blue,
                            Priority = 2,
                            Visibility = _playerId
                        });
                    }
                    break;
            }
            return;
        }

        switch (gameState)
        {
            case GameState.Movement:
                var moveCommand = _selectedUnit.GetValidMoves(cardValues.movementRange)
                    .FirstOrDefault(move => move.TargetPosition == _hoveredTile.Position);
                if (moveCommand != null && GridManager.Instance.IsMoveValid(moveCommand))
                {
                    // Logging
                    _selectedUnit.LogMovement(cardValues, moveCommand);
                    
                    _selectedUnit.CmdRegisterMoveIntent(moveCommand);
                    HandManager.Instance.PlaySelectedCard();
                }

                break;
            case GameState.Attack:
                // If the player clicked on a tile within the current attack radius
                if (_currentAttack.Tiles.Contains(_hoveredTile.Position))
                {
                    // Logging
                    _selectedUnit.LogAttack(cardValues, _currentAttack);
                    
                    _selectedUnit.CmdRegisterAttackIntent(_currentAttack);
                    HandManager.Instance.PlaySelectedCard();
                }

                break;
        }
        
        DeselectUnit();
    }

    private void DeselectUnit()
    {
        if(_selectedUnit == null)
            return;
        
        _selectedUnit.CmdUpdateControlStatus(false);
        _selectedUnit = null;

        switch (GameManager.Instance.gameState)
        {
            case GameState.Movement:
                foreach (var command in _highlightedMoveTiles)
                    MarkerManager.Instance.RemoveMarkerLocal(command.TargetPosition, MarkerType.Move, _playerId);
                DisplayMovePreviewTiles(_hoveredTile);
                break;
            case GameState.Attack:
                if(_currentAttack != null)
                    foreach (var tile in _currentAttack.Tiles)
                        MarkerManager.Instance.RemoveMarkerLocal(tile, MarkerType.Attack, _playerId);
                DisplayAttackPreviewTiles(_hoveredTile);
                break;
        }
        
        _highlightedMoveTiles.Clear();
    }
    
    private void OnPlayersReady()
    {
        GameManager.Instance.localPlayer.GetComponent<Player>().turnSubmitted.AddListener(OnTurnSubmitted);
        _playerId = GameManager.Instance.localPlayer.netId.ToString();
        _statDisplay = FindObjectOfType<UnitStatDisplay>();
        
        GameManager.Instance.gameStateChanged.AddListener(OnGameStateChanged);
        HandManager.Instance.cardDeselected.AddListener(OnCardDeselected);
    }

    private void OnTurnSubmitted()
    {
        _hasSubmitted = true;
        DeselectUnit();
    }

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Attack or GameState.Movement:
                _hasSubmitted = false;
                RemoveAllPreviewTiles();
                break;
            case GameState.AttackExecution:
                if(_currentAttack != null)
                    foreach (var tile in _currentAttack.Tiles)
                        MarkerManager.Instance.RemoveMarkerLocal(tile, MarkerType.Attack, _playerId);
                _currentAttack = null;
                break;
            case GameState.MovementExecution:
                RemoveAllPreviewTiles();
                break;
        }
    }
    
    private void OnCardDeselected()
    {
        DeselectUnit();
    }
}
