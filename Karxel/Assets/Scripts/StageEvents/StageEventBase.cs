using UnityEngine;

public abstract class StageEventBase : ScriptableObject
{
    public abstract void Execute(StageEventParameters parameters);
    public abstract StageEventParameters CreateDefaultParameters();
}
