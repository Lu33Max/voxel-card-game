using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Tile))]
public class TileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
    
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawTileGizmos(Tile tile, GizmoType gizmoType)
    {

    }
}
