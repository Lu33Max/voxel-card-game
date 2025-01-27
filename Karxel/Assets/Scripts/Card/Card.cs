using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pointText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Image cardImage;

    [SerializeField] private Sprite cardBGRegular;
    [SerializeField] private Sprite cardBGUnselected;
    [SerializeField] private Image backgroundImage;

    public CardData CardData { get; private set; }
    
    private RectTransform _transform;
    
    public void Initialize(CardData data, Vector2 startPos)
    {
        CardData = data;
        
        _transform = GetComponent<RectTransform>();
        _transform.position = new Vector3(0, startPos.y, 0);

        pointText.text = CardData.cost.ToString();
        nameText.text = CardData.cardName;
        cardImage.sprite = CardData.cardSprite;
        
        UpdateState();
    }

    private void OnEnable()
    {
        ActionPointManager.Instance.actionPointsUpdated.AddListener(OnActionPointsUpdated);
    }

    private void OnDisable()
    {
        ActionPointManager.Instance.actionPointsUpdated.RemoveListener(OnActionPointsUpdated);
    }

    public void UpdatePosition(Vector2 newPos)
    {
        _transform.anchoredPosition = new Vector3(newPos.x, newPos.y);
    }
    
    public void UpdateYPosition(float newYPos)
    {
        _transform.anchoredPosition = new Vector3(_transform.anchoredPosition.x, newYPos);
    }

    public void UpdateState()
    {
        backgroundImage.sprite = CanBeSelected() ? cardBGRegular : cardBGUnselected;
    }

    public void CardClickedButton()
    {
        if(!CanBeSelected())
            return;
        
        HandManager.Instance.CardClicked(this);
    }

    public void RemoveCard()
    {
        CardManager.Instance.AddCardToUsed(CardData);
        Destroy(gameObject);
    }

    public bool IsCorrectPhase(GameState state)
    {
        return state == GameState.Movement && CardData.cardType != CardType.Attack ||
               state == GameState.Attack && CardData.cardType == CardType.Attack;
    }
    
    private bool CanBeSelected()
    {
        return ActionPointManager.ActionPoints - CardData.cost >= 0 && IsCorrectPhase(GameManager.Instance.gameState);
    }

    private void OnActionPointsUpdated(int newPoints)
    {
        if (CardData != null && newPoints < CardData.cost)
            backgroundImage.sprite = cardBGUnselected;
    }
}
