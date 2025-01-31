using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ActionPointManager : MonoBehaviour
{
    public static int ActionPoints { get; private set; }
    public static ActionPointManager Instance { get; private set; }

    public UnityEvent<int> actionPointsUpdated = new();
    
    [SerializeField] private int startPoints;
    [SerializeField] private int maxPoints;
    [SerializeField] private int pointsPerRound;

    [SerializeField] private TextMeshProUGUI actionText;

    public void Initialize()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;
        
        ActionPoints = startPoints;
        actionText.text = $"{ActionPoints}/{maxPoints}";
    }

    private void OnEnable()
    {
        GameManager.Instance.gameStateChanged.AddListener(OnGameStateChanged);
    }

    private void OnDisable()
    {
        GameManager.Instance.gameStateChanged.RemoveListener(OnGameStateChanged);
    }

    public void UpdateActionPoints(int valueToAdd)
    {
        ActionPoints = Mathf.Clamp(ActionPoints + valueToAdd, 0, maxPoints);
        actionText.text = $"{ActionPoints}/{maxPoints}";

        actionPointsUpdated.Invoke(ActionPoints);
        
        var handManager = HandManager.Instance;
        
        // If the cost of the currently selected card would be too high to play it after the point reduction deselect it
        if (handManager.SelectedCard != null && ActionPoints - handManager.SelectedCard.CardData.cost < 0)
            handManager.DeselectCurrentCard();
    }

    public void UpdateActionPointsOnPlay(int valueToAdd)
    {
        ActionPoints = Mathf.Clamp(ActionPoints + valueToAdd, 0, maxPoints);
        actionText.text = $"{ActionPoints}/{maxPoints}";
    }

    private void OnGameStateChanged(GameState newState)
    {
        if(newState != GameState.Movement)
            return;

        UpdateActionPoints(pointsPerRound);
    }
}
