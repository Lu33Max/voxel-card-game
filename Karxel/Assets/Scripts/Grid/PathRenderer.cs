using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class PathRenderer : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float groundOffset = 0.01f;
    [SerializeField] private float lineWidth = 0.1f;
    
    public void DrawPath(MoveCommand command, Vector3Int start)
    {
        List<Vector3> positions = new();

        var startPos = GridManager.Instance.GridToWorldPosition(start).GetValueOrDefault();
        startPos.y += groundOffset;
        positions.Add(startPos);
        
        foreach (var tile in command.Path.Append(command.TargetPosition))
        {
            var tilePos = GridManager.Instance.GridToWorldPosition(tile).GetValueOrDefault();
            tilePos.y += groundOffset;

            if (Math.Abs(tilePos.y - positions.Last().y) > 0.1)
            {
                var previous = positions.Last();
                var diffVec = previous - tilePos;

                var sign = diffVec.y >= 0 ? -1 : 1;
                var halfPoint = (previous + tilePos) / 2f + diffVec.normalized * (groundOffset * sign);
                
                positions.Add(new Vector3(halfPoint.x, previous.y, halfPoint.z));
                positions.Add(new Vector3(halfPoint.x, tilePos.y, halfPoint.z));
            }
            
            positions.Add(tilePos);
        }
        
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
        lineRenderer.generateLightingData = false;
        lineRenderer.widthMultiplier = lineWidth;
    }

    public void AppendToPath(MoveCommand command)
    {
        List<Vector3> positions = new() { lineRenderer.GetPosition(lineRenderer.positionCount - 1) };

        foreach (var tile in command.Path.Append(command.TargetPosition))
        {
            var tilePos = GridManager.Instance.GridToWorldPosition(tile).GetValueOrDefault();
            tilePos.y += groundOffset;
            
            if (Math.Abs(tilePos.y - positions.Last().y) > 0.1)
            {
                var previous = positions.Last();
                var diffVec = previous - tilePos;

                var sign = diffVec.y >= 0 ? -1 : 1;
                var halfPoint = (previous + tilePos) / 2f + diffVec.normalized * (groundOffset * sign);
                
                positions.Add(new Vector3(halfPoint.x, previous.y, halfPoint.z));
                positions.Add(new Vector3(halfPoint.x, tilePos.y, halfPoint.z));
            }
            
            positions.Add(tilePos);
        }
        
        var positionCount = lineRenderer.positionCount;
        
        // Exclude the duplicate first position
        lineRenderer.positionCount = positionCount + positions.Count - 1;
        for (var i = 1; i < positions.Count; i++)
            lineRenderer.SetPosition(positionCount + i - 1, positions[i]);   
    }

    public void RegeneratePath(List<MoveCommand> allMoveCommands, Vector3Int startPos, out bool canBeRemoved)
    {
        lineRenderer.SetPositions(Array.Empty<Vector3>());

        if (allMoveCommands.Count == 0)
        {
            canBeRemoved = true;
            return;
        }

        DrawPath(allMoveCommands[0], startPos);          
        
        for(var i = 1; i < allMoveCommands.Count; i++)
            AppendToPath(allMoveCommands[i]);

        canBeRemoved = false;
    }
}
