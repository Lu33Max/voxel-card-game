using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Marker : MonoBehaviour
{
    [SerializeField] private List<MarkerColor> colorMap;
    
    private List<MarkerData> _activeMarkers = new();
    private SpriteRenderer _renderer;
    private Material _materialInstance;
    private static readonly int MainColor = Shader.PropertyToID("_MainColor");
    private static readonly int SecondaryColor = Shader.PropertyToID("_SecondaryColor");

    private void Start()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _materialInstance = _renderer.material;
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
        _activeMarkers.Sort((m, n) => m.Priority - n.Priority);
        
        _materialInstance.SetColor(MainColor,
            _activeMarkers.Count > 0
                ? colorMap.Find(c => c.type == _activeMarkers[0].Type).color
                : new Color(0, 0, 0, 0));
        
        _materialInstance.SetColor(SecondaryColor,
            _activeMarkers.Count > 1
                ? colorMap.Find(c => c.type == _activeMarkers[1].Type).color
                : new Color(0, 0, 0, 0));
    }

    public void ClearAllMarkers()
    {
        _activeMarkers.Clear();
        ClearVisuals();
    }
    
    private void ClearVisuals()
    {
        _materialInstance.SetColor(MainColor, new Color(0, 0, 0, 0));
        _materialInstance.SetColor(SecondaryColor, new Color(0, 0, 0, 0));
    }
}

[Serializable]
public class MarkerColor
{
    public MarkerType type;
    public Color color;
}

public class MarkerData
{
    public MarkerType Type;
    public int Priority;
    public string Visibility;
}

public enum MarkerType
{
    Hover,
    Attack,
    Move,
    MovePreview,
    AttackPreview
}
