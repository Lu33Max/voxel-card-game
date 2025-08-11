using System;
using UnityEngine;

[CreateAssetMenu(menuName = "StageEvents/FloodEvent")]
public class FloodEvent : StageEventBase
{
    [Serializable]
    public class FloodParameters : StageEventParameters
    {
        public int floodRows = 1;
    }
    
    public override void Execute(StageEventParameters parameters)
    {
        FloodParameters p = (FloodParameters)parameters;
        Debug.Log($"Flood triggered! Rows affected: {p.floodRows}");
        // Spielfeld verändern
    }

    public override StageEventParameters CreateDefaultParameters()
    {
        return new FloodParameters();
    }
}
