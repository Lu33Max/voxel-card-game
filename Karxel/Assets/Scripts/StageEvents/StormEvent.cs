using System;
using UnityEngine;

[CreateAssetMenu(menuName = "StageEvents/StormEvent")]
public class StormEvent : StageEventBase
{
    [Serializable]
    public class StormParameters : StageEventParameters
    {
        public float windSpeed = 5f;
        public float stormDuration = 2f;
    }
    
    public override void Setup()
    {
        
    }
    
    public override void Execute(StageEventParameters parameters, MonoBehaviour runner)
    {
        StormParameters p = (StormParameters)parameters;
        Debug.Log($"Storm triggered! Wind: {p.windSpeed}, Duration: {p.stormDuration}");
        // Spielfeld beeinflussen
    }

    public override StageEventParameters CreateDefaultParameters()
    {
        return new StormParameters();
    }
}
