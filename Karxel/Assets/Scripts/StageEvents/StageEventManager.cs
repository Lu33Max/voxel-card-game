using UnityEngine;

public class StageEventManager : MonoBehaviour
{
    public StageEventInstance[] events;

    private void Start()
    {
        GameManager.NewRound.AddListener(OnRoundStart);
    }

    private void OnDisable()
    {
        GameManager.NewRound.RemoveListener(OnRoundStart);
    }

    private void OnValidate()
    {
        if(events == null) return;
        
        foreach (var e in events)
        {
            e?.SyncParametersWithEventType();
        }
    }

    private void OnRoundStart(int currentRound)
    {
        foreach (var e in events)
        {
            if (e.triggerRound == currentRound)
            {
                e.eventType.Execute(e.parameters);
            }
        }
    }
}
