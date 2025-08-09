using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    private static bool showTileCubes = true;
    private static bool showTileLabels = true;
    private static bool showTileConnections = true;
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var manager = (GridManager)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gizmo Options", EditorStyles.boldLabel);
        showTileCubes = EditorGUILayout.Toggle("Show Tiles", showTileCubes);
        showTileLabels = EditorGUILayout.Toggle("Show Coords", showTileLabels);
        showTileConnections = EditorGUILayout.Toggle("Show Conns", showTileConnections);
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Scan Tiles"))
        {
            manager.ScanTiles();
        }

        if (GUILayout.Button("Generate Connections"))
        {
            Undo.RecordObject(manager, "Generate Tile Connections");
            manager.GenerateNeighbours();
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawTileGizmos(GridManager manager, GizmoType gizmoType)
    {
        if (manager.Tiles == null) return;

        foreach (var tile in manager.Tiles.Values)
        {
            if (showTileCubes)
                RenderTileBlock(tile);
            
            if (showTileLabels)
                RenderTileCoords(tile);
            
            if (showTileConnections)
                RenderTileConnections(tile, manager);
        }
    }

    private static void RenderTileBlock(TileData tile)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(tile.WorldPosition, new Vector3(1, 0.2f, 1) * 0.3f * 0.9f);
    }

    private static void RenderTileCoords(TileData tile)
    {
        GUIStyle style = new()
        {
            normal =
            {
                textColor = Color.white
            },
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };

        Handles.Label(
            tile.WorldPosition + Vector3.up * 0.1f,
            $"{tile.TilePosition.x},{tile.TilePosition.y},{tile.TilePosition.z}",
            style
        );
    }

    private static void RenderTileConnections(TileData tile, GridManager manager)
    {
        Handles.zTest = CompareFunction.Less;
        
        foreach (var neighbour in tile.TileNeighbours)
        {
            var endTile = manager.GetTileAtGridPosition(neighbour.GridPosition);
            
            if(endTile == null) continue;
            
            // Green for an unhindered connection going in both directions
            if (neighbour.EdgeType == Tile.EdgeType.None && endTile.TileNeighbours.Exists(n => n.GridPosition == tile.TilePosition && n.EdgeType == Tile.EdgeType.None))
                Handles.color = Color.blue;
            // Yellow for one-way connections with the other side not connecting to this tile
            else if(!endTile.TileNeighbours.Exists(n => n.GridPosition == tile.TilePosition))
                Handles.color = Color.yellow;
            // Magenta for connections with a half-blockade in their way
            else if (neighbour.EdgeType == Tile.EdgeType.HalfBlockade)
                Handles.color = Color.magenta;
            // Red for completely blocked and unavailable connections
            else if (neighbour.EdgeType == Tile.EdgeType.FullBlockade)
                Handles.color = Color.red;
            else
                return;
            
            Handles.DrawLine(tile.WorldPosition, endTile.WorldPosition);
        }

        Handles.zTest = CompareFunction.Always;
    }
}