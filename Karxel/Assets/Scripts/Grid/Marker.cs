using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Marker : MonoBehaviour
{
    private List<MarkerData> _activeMarkers = new();
    private MarkerData _currentMarker;
    private SpriteRenderer _renderer;

    private void Start()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    public void AddMarker(MarkerData markerData)
    {
        _activeMarkers.Add(markerData);
        UpdateCurrentMarker();
    }
    
    public void RemoveMarker(MarkerType markerType, string visibility = null)
    {
        var markerToRemove = _activeMarkers.FirstOrDefault(m => m.Type == markerType && (visibility == null || m.Visibility == visibility));
        if (markerToRemove == null) 
            return;
        
        _activeMarkers.Remove(markerToRemove);
        UpdateCurrentMarker();
    }
    
    private void UpdateCurrentMarker()
    {
        if (_activeMarkers.Count == 0)
        {
            _currentMarker = null;
            ClearVisuals();
            return;
        }
        
        _currentMarker = _activeMarkers.OrderBy(m => m.Priority).First();
        ApplyVisuals(_currentMarker);
    }

    public void ClearAllMarkers()
    {
        _currentMarker = null;
        _activeMarkers.Clear();
        ClearVisuals();
    }
    
    private void ClearVisuals()
    {
        _renderer.color = new Color(0, 0, 0, 0);
    }
    
    private void ApplyVisuals(MarkerData markerData)
    {
        _renderer.color = markerData.MarkerColor;
        // GetComponentInChildren<SpriteRenderer>().sprite = marker.MarkerIcon;
    }
}

public class MarkerData
{
    public MarkerType Type;
    public int Priority;
    public Color MarkerColor;
    public string Visibility;
}

public enum MarkerType
{
    Hover,
    Attack,
    Move,
    MovePreview
}

public enum Visibility
{
    Player,
    Blue,
    Red,
    All
}
