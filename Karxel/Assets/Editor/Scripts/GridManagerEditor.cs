using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var manager = (GridManager)target;
        if (GUILayout.Button("Scan Tiles"))
        {
            manager.ScanTiles();
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawTileGizmos(GridManager manager, GizmoType gizmoType)
    {
        if (manager.tiles == null) return;

        foreach (var tile in manager.tiles.Values)
        {
            Gizmos.color = tile.walkable ? Color.green : Color.red;
            Gizmos.DrawWireCube(tile.worldPos, new Vector3(1, 0.2f, 1) * 0.3f * 0.9f);
        }
    }
}