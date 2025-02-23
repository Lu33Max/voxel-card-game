using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class ActionPointManager : MonoBehaviour
{
    public static int ActionPoints { get; private set; }
    public static ActionPointManager Instance { get; private set; }

    public UnityEvent<int> actionPointsUpdated = new();
    
    [SerializeField] private int startPoints;
    [FormerlySerializedAs("maxPoints")] [SerializeField] private int baseMaxPoints;
    [FormerlySerializedAs("pointsPerRound")] [SerializeField] private int basePointsPerRound;

    [SerializeField] private TextMeshProUGUI actionText;

    private int _maxPoints;
    private int _pointsPerRound;

    public void Initialize()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;

        _maxPoints = baseMaxPoints;
        _pointsPerRound = basePointsPerRound;
        
        ActionPoints = startPoints;
        actionText.text = $"{ActionPoints}/{_maxPoints}";
    }

    private void OnEnable()
    {
        GameManager.Instance.updateActionPoints.AddListener(UpdatePointsOnNewRound);
    }

    private void OnDisable()
    {
        GameManager.Instance.updateActionPoints.RemoveListener(UpdatePointsOnNewRound);
    }

    public void UpdateActionPoints(int valueToAdd)
    {
        ActionPoints = Mathf.Clamp(ActionPoints + valueToAdd, 0, _maxPoints);
        actionText.text = $"{ActionPoints}/{_maxPoints}";

        actionPointsUpdated.Invoke(ActionPoints);
        
        var handManager = HandManager.Instance;
        
        // If the cost of the currently selected card would be too high to play it after the point reduction deselect it
        if (handManager.SelectedCard != null && ActionPoints - handManager.SelectedCard.CardData.cost < 0)
            handManager.DeselectCurrentCard();
    }

    public void UpdateActionPointsOnPlay(int valueToAdd)
    {
        ActionPoints = Mathf.Clamp(ActionPoints + valueToAdd, 0, _maxPoints);
        actionText.text = $"{ActionPoints}/{_maxPoints}";
    }

    private void UpdatePointsOnNewRound(int blueCount, int redCount)
    {
        var pointDiff = Mathf.Abs(blueCount - redCount) * baseMaxPoints;
        var pointGainDiff = Mathf.Abs(blueCount - redCount) * basePointsPerRound;

        // If the player is part of the disadvantaged team
        if ((GameManager.Instance.localPlayer.team == Team.Red && redCount < blueCount) ||
            (GameManager.Instance.localPlayer.team == Team.Blue && blueCount < redCount))
        {
            _maxPoints = baseMaxPoints + (pointDiff / redCount);
            _pointsPerRound = basePointsPerRound + (pointGainDiff / redCount);
        }
        else
        {
            _maxPoints = baseMaxPoints;
            _pointsPerRound = basePointsPerRound;
        }

        UpdateActionPoints(_pointsPerRound);
    }
}
