using System;
using System.Linq;
using UnityEngine;

public class StageEventManager : MonoBehaviour
{
    public static StageEventManager Instance { get; private set; }
    
    public StageEventInstance[] events;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        GameManager.NewRound.AddListener(OnRoundStart);
        
        foreach (var e in events)
            e.eventType.Setup();
    }

    private void OnDisable()
    {
        GameManager.NewRound.RemoveListener(OnRoundStart);
        StopAllCoroutines();
    }

    private void OnValidate()
    {
        if(events == null) return;
        
        foreach (var e in events)
            e?.SyncParametersWithEventType();
    }

    /// <summary> Retrieves the event with the lowest triggerRound that is bigger than the given currentRound </summary>
    public StageEventInstance GetNextEvent(int currentRound)
    {
        return events.OrderBy(e => e.triggerRound)
                     .FirstOrDefault(eventInstance => eventInstance.triggerRound > currentRound);
    }

    private void OnRoundStart(int currentRound)
    {
        foreach (var e in events)
            if (e.triggerRound == currentRound)
                e.eventType.Execute(e.parameters, this);

    }
}
