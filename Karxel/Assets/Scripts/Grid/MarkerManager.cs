using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MarkerManager : NetworkBehaviour
{
    [SerializeField] private GameObject markerPrefab;
    
    public static MarkerManager Instance { get; private set; }
    
    private Dictionary<Vector3Int, Marker> _markers = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    /// <summary> Registers new tile upon board creation </summary>
    [Client]
    public void RegisterTile(Vector3Int tilePos, Vector3 worldPos, Vector3 scale)
    {
        var marker = Instantiate(markerPrefab, transform);
        marker.transform.position = worldPos;
        marker.transform.localScale = scale;
        
        _markers[tilePos] = marker.GetComponent<Marker>();
    }

    /// <summary> Adds a new Marker to the given position </summary>
    [ClientRpc]
    public void RPCAddMarker(Vector3Int position, MarkerData markerData)
    {
        if (ShouldIgnore(markerData.Visibility))
            return;
        
        if (_markers.TryGetValue(position, out Marker tile))
            tile.AddMarker(markerData);
    }
    
    [Client]
    public void AddMarkerLocal(Vector3Int position, MarkerData markerData)
    {
        if (ShouldIgnore(markerData.Visibility))
            return;
        
        if (_markers.TryGetValue(position, out Marker tile))
            tile.AddMarker(markerData);
    }
    
    [ClientRpc]
    public void RPCRemoveMarker(Vector3Int position, MarkerType markerType, string visibility)
    {
        if (_markers.TryGetValue(position, out Marker tile))
            tile.RemoveMarker(markerType, visibility);
    }
    
    [Client]
    public void RemoveMarkerLocal(Vector3Int position, MarkerType markerType, string visibility)
    {
        if (_markers.TryGetValue(position, out var tile))
            tile.RemoveMarker(markerType, visibility);
    }

    [ClientRpc]
    public void RPCClearAllMarkers()
    {
        foreach (var tile in _markers.Values)
            tile.ClearAllMarkers();
    }

    private static bool ShouldIgnore(string visibility)
    {
        if (GameManager.Instance == null || GameManager.Instance.localPlayer == null || visibility == null)
            return true;
        
        var player = GameManager.Instance.localPlayer;

        return visibility != "All" && ((visibility == "Blue" && player.team != Team.Blue) ||
                                        (visibility == "Red" && player.team != Team.Red) ||
                                        (visibility != "Blue" && visibility != "Red" &&
                                         visibility != player.netId.ToString() && visibility != "local"));
    }
}