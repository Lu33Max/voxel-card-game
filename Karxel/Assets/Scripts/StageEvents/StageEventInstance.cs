using System;
using UnityEngine;

[Serializable]
public class StageEventInstance
{
    public int triggerRound;
    public StageEventBase eventType;

    [SerializeReference]
    public StageEventParameters? parameters;

    // Only used internally for Unity to properly reset the available parameter options inside the inspector
    [HideInInspector] public StageEventBase? lastEventType;
    
    public void InitParametersIfNull()
    {
        if (parameters == null && eventType != null)
            parameters = eventType.CreateDefaultParameters();
    }
    
    /// <summary>
    ///     Used internally to reset the displayed parameters in the inspector in case the selected eventType changes
    /// </summary>
    public void SyncParametersWithEventType()
    {
        if (eventType == lastEventType) return;
        
        // If eventType was changed or set to null → replace parameters
        parameters = eventType != null ? eventType.CreateDefaultParameters() : null;
        lastEventType = eventType;
    }
}
