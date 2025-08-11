using System;
using UnityEngine;

[Serializable]
public class StageEventInstance
{
    public int triggerRound;
    public StageEventBase eventType;

    [SerializeReference]
    public StageEventParameters parameters;

    // Only used internally for Unity to properly reset the available parameter options inside the inspector
    [HideInInspector] public StageEventBase lastEventType;
    
    public void InitParametersIfNull()
    {
        if (parameters == null && eventType != null)
            parameters = eventType.CreateDefaultParameters();
    }
    
    public void SyncParametersWithEventType()
    {
        // Wenn EventType geändert oder auf null gesetzt wurde → Parameter neu setzen/löschen
        if (eventType != lastEventType)
        {
            parameters = eventType != null ? eventType.CreateDefaultParameters() : null;
            lastEventType = eventType;
        }
    }
}
