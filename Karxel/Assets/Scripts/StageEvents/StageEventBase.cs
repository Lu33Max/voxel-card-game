using UnityEngine;

public abstract class StageEventBase : ScriptableObject
{
    public abstract void Setup();
    public abstract void Execute(StageEventParameters parameters, MonoBehaviour runner);
    public abstract StageEventParameters CreateDefaultParameters();
}
