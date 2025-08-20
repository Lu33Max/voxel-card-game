using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary> Used to create, manage and destroy all local tile markers of the corresponding unit </summary>
[RequireComponent(typeof(Unit))]
public class UnitMarkerManager : MonoBehaviour
{
    /// <summary> Unit script attached to the same GameObject as this script </summary>
    private Unit _unit;
    
    /// <summary> All tiles currently active on the field from this unit separated by type </summary>
    private Dictionary<MarkerType, List<Vector3Int>> _registeredMarkers = new();
    
    /// <summary> Cache of move tiles on the current TilePosition to use inside update </summary>
    private Vector3Int[] _currentMoveTiles = {};

    private void Start()
    {
        _unit = GetComponent<Unit>();
    }

    /// <summary>
    ///     Display preview tiles when the unit is selected depending on the current gamestate and tile being hovered.
    ///     Gets called once when first selecting the unit.
    /// </summary>
    /// <param name="hoveredTile"> GridPosition of the tile currently hovered </param>
    public void DisplaySelectedPreviews(Vector3Int hoveredTile)
    {
        switch (GameManager.Instance.gameState)
        {
            case GameState.Attack:
                break;
            
            case GameState.Movement:
                ClearHoverPreviews();
                
                var cardData = HandManager.Instance.SelectedCard?.CardData;
                if(cardData == null) return;
                
                _currentMoveTiles = _unit.GetValidMoves(cardData.movementRange).Select(m => m.TargetPosition).ToArray();
                
                CreateNewMarkers(_currentMoveTiles, MarkerType.Move, 2);
                break;
        }
        
        UpdatePreviews(hoveredTile);
    }

    /// <summary> Display preview tiles on hover depending on the current gamestate and tile being hovered </summary>
    /// <param name="hoveredTile"> GridPosition of the tile currently hovered </param>
    public void DisplayHoverPreviews(Vector3Int hoveredTile)
    {
        switch (GameManager.Instance.gameState)
        {
            case GameState.Attack:
                CreateNewMarkers(_unit.GetValidAttackTiles(), MarkerType.AttackPreview, 3);
                break;
            
            case GameState.Movement:
                var cardData = HandManager.Instance.SelectedCard?.CardData;
                
                if(cardData == null) return;

                HashSet<Vector3Int> allReachableAtkTiles = new();
                _currentMoveTiles = _unit.GetValidMoves(cardData.movementRange).Select(m => m.TargetPosition).ToArray();
                foreach (var moveTile in _currentMoveTiles)
                {
                    MarkerManager.Instance.AddMarkerLocal(moveTile, new MarkerData
                    {
                        Priority = 3,
                        Type = MarkerType.MovePreview,
                        Visibility = "local"
                    });
                    AddToMarkers(MarkerType.MovePreview, moveTile);
                    
                    foreach (var end in _unit.GetValidAttackTiles(moveTile))
                        allReachableAtkTiles.Add(end);
                }

                // Add the current position in case the unit does not move / only possible when no intent is active
                if(!_unit.HasMoveIntentsRegistered())
                    foreach (var end in _unit.GetValidAttackTiles())
                        allReachableAtkTiles.Add(end);

                // If the unit is hovered directly, display all possible tiles, else only the ones from the current position
                var attackTiles = hoveredTile == _unit.TilePosition
                    ? allReachableAtkTiles.ToArray()
                    : _currentMoveTiles.Contains(hoveredTile)
                        ? _unit.GetValidAttackTiles(hoveredTile).ToArray()
                        : new Vector3Int[] { };

                CreateNewMarkers(attackTiles, MarkerType.AttackPreview, 4);
                break;
        }
    }

    /// <summary> Updates the displayed attack previews during movement phase or attacks during attack phase </summary>
    /// <param name="hoveredTile"> The newly hovered tile </param>
    public void UpdatePreviews(Vector3Int hoveredTile)
    {
        switch (GameManager.Instance.gameState)
        {
            case GameState.Attack:
                RemoveMarkersOfType(MarkerType.Attack);
                
                var attackPositions = _unit.GetAttackForHoverPosition(hoveredTile, 0)?.Tiles.ToArray();
                if(attackPositions == null) return;

                CreateNewMarkers(attackPositions, MarkerType.Attack, 2);
                return;
            
            case GameState.Movement:
                RemoveMarkersOfType(MarkerType.AttackPreview);
        
                HashSet<Vector3Int> allReachableAttackTiles = new();

                if (!_unit.isControlled && _registeredMarkers.TryGetValue(MarkerType.MovePreview, out var moveTiles))
                {
                    // Add the current position in case the unit does not move / only possible when no intent is active
                    if(!_unit.HasMoveIntentsRegistered())
                        moveTiles.Add(_unit.TilePosition);
                    
                    foreach (var moveTile in moveTiles.SelectMany(moveTile => _unit.GetValidAttackTiles(moveTile)))
                        allReachableAttackTiles.Add(moveTile);   
                }

                var attackTiles = hoveredTile == _unit.TilePosition
                    ? allReachableAttackTiles.ToArray()
                    : _currentMoveTiles.Contains(hoveredTile)
                        ? _unit.GetValidAttackTiles(hoveredTile).ToArray()
                        : new Vector3Int[] {};

                CreateNewMarkers(attackTiles, MarkerType.AttackPreview, 4);
                return;
        }
    }

    /// <summary> Clear all local attack and move preview tiles generated by this unit </summary>
    public void ClearHoverPreviews()
    {
        RemoveMarkersOfType(MarkerType.AttackPreview);
        RemoveMarkersOfType(MarkerType.MovePreview);
    }

    /// <summary> Clear all local attack and move tiles created when playing a card on the selected unit </summary>
    public void ClearSelectPreviews()
    {
        RemoveMarkersOfType(MarkerType.Attack);
        RemoveMarkersOfType(MarkerType.Move);
    }

    /// <summary>
    ///     Adds a reference to a new marker into to the <see cref="_registeredMarkers"/> dictionary, ensuring the given
    ///     type key was already initiated.
    /// </summary>
    /// <param name="type"> Type under which the marker should be stored </param>
    /// <param name="position"> Position of the marker to store </param>
    private void AddToMarkers(MarkerType type, Vector3Int position)
    {
        _registeredMarkers.TryAdd(type, new List<Vector3Int>());
        _registeredMarkers[type].Add(position);
    }

    /// <summary> Takes a number of Tile Positions and generates new markers of the given type at all positions </summary>
    /// <param name="positions"> Tile Positions where the markers should be created </param>
    /// <param name="type"> Type of the newly created marker </param>
    /// <param name="priority"> Display priority of the created marker </param>
    private void CreateNewMarkers(IEnumerable<Vector3Int> positions, MarkerType type, int priority)
    {
        foreach (var position in positions)
        {
            MarkerManager.Instance.AddMarkerLocal(position, new MarkerData
            {
                Priority = priority,
                Type = type,
                Visibility = "local"
            });
            AddToMarkers(type, position);
        }
    }

    /// <summary> Removes all markers of the given type on the field and clears the local reference </summary>
    /// <param name="type"> Type of tiles to remove </param>
    private void RemoveMarkersOfType(MarkerType type)
    {
        if (!_registeredMarkers.TryGetValue(type, out var foundTiles)) 
            return;
        
        foreach (var tile in foundTiles)
            MarkerManager.Instance.RemoveMarkerLocal(tile, type, "local");
        _registeredMarkers[type].Clear();
    }
}
