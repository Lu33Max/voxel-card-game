using Mirror;
using TMPro;
using UnityEngine;

public class ActionPointManager : MonoBehaviour
{
    public static int ActionPoints { get; private set; }
    public static ActionPointManager Instance { get; private set; }
    
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
        actionText.text = $"Actions: {ActionPoints}/{maxPoints}";
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
        actionText.text = $"Actions: {ActionPoints}/{maxPoints}";

        var handManager = HandManager.Instance;
        
        // If the cost of the currently selected card would be too high to play it after the point reduction deselect it
        if (handManager.SelectedCard != null && ActionPoints - handManager.SelectedCard.CardData.cost < 0)
            handManager.DeselectCurrentCard();
    }

    public void UpdateActionPointsOnPlay(int valueToAdd)
    {
        ActionPoints = Mathf.Clamp(ActionPoints + valueToAdd, 0, maxPoints);
        actionText.text = $"Actions: {ActionPoints}/{maxPoints}";
    }

    private void OnGameStateChanged(GameState newState)
    {
        if(newState != GameState.Movement)
            return;

        UpdateActionPoints(pointsPerRound);
    }
}
