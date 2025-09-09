using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MarkerManager : NetworkSingleton<MarkerManager>
{
    [SerializeField] private GameObject markerPrefab;
    
    private Dictionary<Vector3Int, Marker> _markers = new();

    private void Start()
    {
        if (GridManager.Instance.IsGridSetup) RegisterTiles();
        else StartCoroutine(AwaitGridManagerSetup());
    }

    private IEnumerator AwaitGridManagerSetup()
    {
        while (!GridManager.Instance.IsGridSetup)
            yield return new WaitForEndOfFrame();
        
        RegisterTiles();
    }

    /// <summary> Registers new tile upon board creation </summary>
    private void RegisterTiles()
    {
        var scale = GridManager.Instance.TileSize;
        
        foreach (var tile in GridManager.Instance.Tiles.Values)
        {
            var marker = Instantiate(markerPrefab, transform);
            marker.transform.position = tile.WorldPosition;
            marker.transform.position += new Vector3(0, 0.01f, 0);
            marker.transform.localScale = scale;
        
            _markers[tile.TilePosition] = marker.GetComponent<Marker>();   
        }
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
        
        if (_markers.TryGetValue(position, out var tile))
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

    private static bool ShouldIgnore(string? visibility)
    {
        if (GameManager.Instance == null || Player.LocalPlayer == null || visibility == null)
            return true;
        
        var player = Player.LocalPlayer;

        return visibility != "All" && ((visibility == "Blue" && player.team != Team.Blue) ||
                                        (visibility == "Red" && player.team != Team.Red) ||
                                        (visibility != "Blue" && visibility != "Red" &&
                                         visibility != player.netId.ToString() && visibility != "local"));
    }
}