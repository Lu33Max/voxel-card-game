using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class PathRenderer : NetworkBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float groundOffset = 0.01f;
    
    public void DrawPath(MoveCommand command, Vector3Int start)
    {
        List<Vector3> positions = new();

        var startPos = GridManager.Instance.GridToWorldPosition(start).GetValueOrDefault();
        startPos.y += groundOffset;
        positions.Add(startPos);
        
        foreach (var tile in command.Path.Append(command.TargetPosition))
        {
            var tilePos = GridManager.Instance.GridToWorldPosition(tile).GetValueOrDefault();

            if (Math.Abs(tilePos.y - positions.Last().y) > 0.1)
            {
                var previous = positions.Last();
                var diffVec = tilePos - previous;

                positions.Add(new Vector3(previous.x + diffVec.x / 2 + Sign(diffVec.x) * groundOffset,
                    previous.y + groundOffset, previous.z + diffVec.z / 2 + Sign(diffVec.z) * groundOffset));
                positions.Add(new Vector3(previous.x + diffVec.x / 2 + Sign(diffVec.x) * groundOffset,
                    tilePos.y + groundOffset, previous.z + diffVec.z / 2 + Sign(diffVec.z) * groundOffset));
            }

            tilePos.y += groundOffset;
            positions.Add(tilePos);
        }
        
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
        lineRenderer.generateLightingData = false;
        lineRenderer.widthMultiplier = 0.05f;
    }

    public void AppendToPath(MoveCommand command)
    {
        List<Vector3> positions = new();
        positions.Add(lineRenderer.GetPosition(lineRenderer.positionCount - 1));
        
        foreach (var tile in command.Path.Append(command.TargetPosition))
        {
            var tilePos = GridManager.Instance.GridToWorldPosition(tile).GetValueOrDefault();

            if (Math.Abs(tilePos.y - positions.Last().y) > 0.01)
            {
                var previous = positions.Last();
                var diffVec = tilePos - previous;

                positions.Add(new Vector3(previous.x + diffVec.x / 2 + Sign(diffVec.x) * groundOffset,
                    previous.y + groundOffset, previous.z + diffVec.z / 2 + Sign(diffVec.z) * groundOffset));
                positions.Add(new Vector3(previous.x + diffVec.x / 2 + Sign(diffVec.x) * groundOffset,
                    tilePos.y + groundOffset, previous.z + diffVec.z / 2 + Sign(diffVec.z) * groundOffset));
            }

            tilePos.y += groundOffset;
            positions.Add(tilePos);

            var positionCount = lineRenderer.positionCount;
            positionCount++;
            lineRenderer.positionCount = positionCount;
            lineRenderer.SetPosition(positionCount - 1, tilePos);
        }
    }

    private static float Sign(float num)
    {
        return num > 0 ? 1f : num < 0 ? -1f : 0f;
    }
}
