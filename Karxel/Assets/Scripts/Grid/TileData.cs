using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData
{
    public Vector2Int Position;
    public float Height;

    /// <summary>Calculates the world position of the current tile</summary>
    /// <param name="yOffset">Optional offset on top of y-coordinate</param>
    /// <returns></returns>
    public Vector3 GetWorldPosition(float yOffset = 0f)
    {
        Vector2 worldPos = GridManager.Instance.GridToWorldPosition(Position);
        return new Vector3(worldPos.x, Height + yOffset, worldPos.y);
    }
}
