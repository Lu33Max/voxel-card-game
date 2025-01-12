using System;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

public class ActionLogger : MonoBehaviour
{
    public static ActionLogger Instance { get; private set; }
    
    private string _currentPhase = "attack";
    private string _logFilePath;

    private void Awake()
    {
        if (Instance != null)
            return;
        
        Instance = this;
        _logFilePath = Path.Combine(Application.persistentDataPath, $"KarxelLog_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.json");
    }

    /// <summary>Add new action to the log</summary>
    public void LogAction(string playerId, string team, string actionType, [CanBeNull] string actionValues,
        [CanBeNull] string target, [CanBeNull] string unitId, [CanBeNull] string unitName, [CanBeNull] string startPos)
    {
        var action = new PlayerAction
        {
            playerId = playerId,
            team = team,
            actionType = actionType,
            actionValues = actionValues,
            unitId = unitId,
            unitName = unitName,
            startPos = startPos,
            targetPos = target,
            timestamp = DateTime.Now.ToString("o"),
        };
        
        File.AppendAllText(_logFilePath, JsonUtility.ToJson(action) + "\n");
        
        if (action.actionType == "phaseSwitch")
        {
            _currentPhase = _currentPhase == "move" ? "attack" : "move";
            File.AppendAllText(_logFilePath, "#" + _currentPhase.ToUpperInvariant() + "\n");
        }
    }
}

[Serializable]
public class PlayerAction
{
    public string playerId;
    public string team;
    public string actionType;
    [CanBeNull] public string actionValues;
    [CanBeNull] public string unitId;
    [CanBeNull] public string unitName;
    [CanBeNull] public string startPos;
    [CanBeNull] public string targetPos;
    public string timestamp;
}
