using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridMouseInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Hover Highlight")]
    [SerializeField] private GameObject highlightMarker;
    [SerializeField] private float groundDistance = 0.01f;
    
    [Header("Attack Highlight")]
    [SerializeField] private GameObject attackMarker;
    
    private Camera _mainCamera;
    private TileData _hoveredTile;
    private Unit _selectedUnit;

    private bool _isHovering;
    private Vector3 _prevMousePos;
    private List<MoveCommand> _highlightedMoveTiles = new();
    private Attack _currentAttack;
    private Dictionary<Vector2Int, List<GameObject>> _highlightedAttackTiles = new();

    private void Start()
    {
        _mainCamera = Camera.main;
        highlightMarker.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        GameManager.PlayersReady.AddListener(OnPlayersReady);
    }

    private void OnDestroy()
    {
        GameManager.PlayersReady.RemoveListener(OnPlayersReady);
        GameManager.Instance.gameStateChanged.RemoveListener(OnGameStateChanged);
        HandManager.Instance.CardDeselected.RemoveListener(OnCardDeselected);
    }

    private void Update()
    {
        CheckForHoveredTile();
        UpdateHoverHighlighter();
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
                _isHovering = false;
                return;
            }

            // Check if a tile exists at the given position
            TileData tile = GridManager.Instance.GetTileAtWorldPosition(new Vector2(hit.point.x, hit.point.z));

            _isHovering = true;
            _hoveredTile = tile;

            if (GameManager.Instance.gameState == GameState.Attack && _selectedUnit != null)
            {
                var cardValues = HandManager.Instance.SelectedCard.CardData;
                var attack = _selectedUnit.GetValidAttackTiles(cardValues.attackRange, cardValues.attackDamage, hit.point, _prevMousePos, true, out bool hasChanged);

                if (hasChanged)
                {
                    _currentAttack = attack;
                    HighlightAttackTilesLocal(attack.Tiles, _selectedUnit.TilePosition);
                }
            }

            _prevMousePos = hit.point;
            
            CheckForMouseInteraction();
        }
        else
        {
            HideAllLocalAttackTiles();
            _isHovering = false;
        }
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
                
                HandManager.Instance.PlaySelectedCard();
                return;
            }
            
            // If the user tries to select a unit during the attack phase he already assigned an attack to
            if (gameState == GameState.Attack && _hoveredTile.Unit.AttackIntent.Exists(a => a.PlayerId == player.netId))
                return;

            _selectedUnit = _hoveredTile.Unit;
            _selectedUnit.CmdUpdateControlStatus(true);

            switch (gameState)
            {
                // Player can only issue an attack command to a unit once per round
                case GameState.Attack:
                    // If the unit was selected this frame it should always generate the highlight tiles
                    _currentAttack = _selectedUnit.GetValidAttackTiles(cardValues.attackRange, cardValues.attackDamage,
                        _prevMousePos, _prevMousePos, false, out _);
                    HighlightAttackTilesLocal(_currentAttack.Tiles, _selectedUnit.TilePosition);
                    break;
                case GameState.Movement:
                    _highlightedMoveTiles = _selectedUnit.GetValidMoves(cardValues.movementRange);
                    GridManager.Instance.HighlightMoveTiles(_highlightedMoveTiles, true);
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

    // Updates the position of the hover highlight
    private void UpdateHoverHighlighter()
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

        switch (GameManager.Instance.gameState)
        {
            case GameState.Movement:
                GridManager.Instance.HighlightMoveTiles(_highlightedMoveTiles, false);
                break;
            case GameState.Attack:
                HideAllLocalAttackTiles();
                break;
        }
        
        _selectedUnit.CmdUpdateControlStatus(false);
        
        _highlightedMoveTiles = null;
        _selectedUnit = null;
    }
    
    private void HighlightAttackTilesLocal(List<Vector2Int> tiles, Vector2Int unit)
    {
        var exist = _highlightedAttackTiles.TryGetValue(unit, out var previousTiles);
        
        if(exist && previousTiles.Count > 0)
            for (int i = 0; i < previousTiles.Count; i++)
            {
                if (i < tiles.Count)
                    previousTiles[i].transform.position = GridManager.Instance.GetTileAtGridPosition(tiles[i])
                        .GetWorldPosition(groundDistance);
                else
                {
                    _highlightedAttackTiles[unit].Remove(previousTiles[i]);
                    Destroy(previousTiles[i]);
                }
            }
        
        if(tiles.Count - (previousTiles?.Count ?? 0) <= 0)
            return;

        var start = previousTiles?.Count ?? 0;
        
        for (int i = start; i < tiles.Count; i++)
        {
            var newMarker = Instantiate(attackMarker, transform);
            newMarker.transform.position =
                GridManager.Instance.GetTileAtGridPosition(tiles[i]).GetWorldPosition(groundDistance);
            
            var res = GridManager.Instance.GridResolution;
            newMarker.transform.localScale = new Vector3(res, res, res);
            
            if(exist || i > start)
                _highlightedAttackTiles[unit].Add(newMarker);
            else
                _highlightedAttackTiles.Add(unit, new List<GameObject>{newMarker});
        }
    }
    
    private void HideAllLocalAttackTiles()
    {
        foreach(var highlight in _highlightedAttackTiles.Values.SelectMany(h => h))
            Destroy(highlight);
        
        _highlightedAttackTiles.Clear();
    }
    
    private void OnPlayersReady()
    {
        GameManager.Instance.localPlayer.GetComponent<Player>().turnSubmitted.AddListener(OnTurnSubmitted);
        GameManager.Instance.gameStateChanged.AddListener(OnGameStateChanged);
        HandManager.Instance.CardDeselected.AddListener(OnCardDeselected);
    }

    private void OnTurnSubmitted()
    {
        DeselectUnit();
    }

    private void OnGameStateChanged(GameState newState)
    {
        if(newState == GameState.AttackExecution)
            HideAllLocalAttackTiles();
    }
    
    private void OnCardDeselected()
    {
        DeselectUnit();
    }
}
