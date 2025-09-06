using System.Linq;
using UnityEngine;

public class StageEventManager : Singleton<StageEventManager>
{
    [SerializeField] private StageEventInstance[] events;

    private void Start()
    {
        if (GameManager.Instance != null) HandleGameManagerReady();
        else GameManager.OnReady += HandleGameManagerReady;
        
        foreach (var e in events)
            e.eventType.Setup();
    }

    private void HandleGameManagerReady()
    {
        GameManager.Instance.NewRound += OnRoundStart;
    }

    private void OnDisable()
    {
        GameManager.OnReady -= HandleGameManagerReady;
        GameManager.Instance.NewRound -= OnRoundStart;
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

    /// <summary> Whenever a new round starts, check if there are any events that have to be executed this round </summary>
    /// <param name="currentRound"> The current round counter </param>
    private void OnRoundStart(int currentRound)
    {
        Debug.Log("Round started");
        
        foreach (var e in events)
            if (e.triggerRound == currentRound)
                e.eventType.Execute(e.parameters, this);
    }
}
