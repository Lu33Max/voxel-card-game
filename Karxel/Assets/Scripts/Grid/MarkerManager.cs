using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MarkerManager : NetworkBehaviour
{
    [SerializeField] private GameObject markerPrefab;
    
    public static MarkerManager Instance { get; private set; }
    
    private Dictionary<Vector2Int, Marker> _markers = new();
    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    /// <summary>Registers new tile upon board creation</summary>
    [Client]
    public void RegisterTile(Vector2Int tilePos, Vector3 worldPos, float scale)
    {
        var marker = Instantiate(markerPrefab, transform);
        marker.transform.position = worldPos;
        marker.transform.localScale = new Vector3(scale, scale, scale);
        
        _markers[tilePos] = marker.GetComponent<Marker>();
    }

    /// <summary>Adds a new Marker to the given position</summary>
    [ClientRpc]
    public void RPCAddMarker(Vector2Int position, MarkerData markerData)
    {
        if (ShouldIgnore(markerData.Visibility))
            return;
        
        if (_markers.TryGetValue(position, out Marker tile))
            tile.AddMarker(markerData);
    }
    
    [Client]
    public void AddMarkerLocal(Vector2Int position, MarkerData markerData)
    {
        if (ShouldIgnore(markerData.Visibility))
            return;
        
        if (_markers.TryGetValue(position, out Marker tile))
            tile.AddMarker(markerData);
    }

    // Entfernt eine Markierung von einem spezifischen Tile
    [ClientRpc]
    public void RPCRemoveMarker(Vector2Int position, MarkerType markerType, string visibility)
    {
        if (_markers.TryGetValue(position, out Marker tile))
            tile.RemoveMarker(markerType, visibility);
    }
    
    [Client]
    public void RemoveMarkerLocal(Vector2Int position, MarkerType markerType, string visibility)
    {
        if (_markers.TryGetValue(position, out Marker tile))
            tile.RemoveMarker(markerType, visibility);
    }

    // Entfernt alle Markierungen von einem Tile
    [ClientRpc]
    public void RPCClearMarkers(Vector2Int position)
    {
        if (_markers.TryGetValue(position, out Marker tile))
            tile.ClearAllMarkers();
    }

    [ClientRpc]
    public void RPCClearAllMarkers()
    {
        foreach (var tile in _markers.Values)
            tile.ClearAllMarkers();
    }

    private bool ShouldIgnore(string visibility)
    {
        var player = GameManager.Instance.localPlayer;

        return visibility != "All" && ((visibility == "Blue" && player.team != Team.Blue) ||
                                        (visibility == "Red" && player.team != Team.Red) ||
                                        (visibility != "Blue" && visibility != "Red" &&
                                         visibility != player.netId.ToString()));
    }
}