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

    [Header("Hand Position")]
    [SerializeField] private float selectedRaise;
    [SerializeField] private float inactiveLower = 50f;

    public CardData CardData { get; private set; }
    
    private Vector3 _defaultPos;
    private bool _isSelected;
    
    private RectTransform _transform;
    
    public void Initialize(CardData data, Vector2 startPos)
    {
        CardData = data;
        
        _transform = GetComponent<RectTransform>();
        _transform.position = new Vector3(0, startPos.y, 0);

        pointText.text = CardData.cost.ToString();
        nameText.text = CardData.cardName;
        cardImage.sprite = CardData.cardSprite;
    }

    public void UpdatePosition(Vector2 newPos)
    {
        _defaultPos = new Vector3(newPos.x, newPos.y, 0);
        _transform.position = new Vector3(newPos.x, _transform.position.y, transform.position.z);
    }

    public void CardClickedButton()
    {
        if(!CanBeSelected())
            return;
        
        HandManager.Instance.CardClicked(this);
    }

    public void SelectCard()
    {
        _isSelected = true;
        _transform.position = new Vector3(_defaultPos.x, _defaultPos.y + selectedRaise, _defaultPos.z);
    }

    public void DeselectCard()
    {
        _isSelected = false;
        _transform.position = _defaultPos;
    }

    public void SetActiveState(bool isActive)
    {
        _transform.position = new Vector3(_defaultPos.x, _defaultPos.y - (isActive ? 0 : inactiveLower), _defaultPos.z);
    }

    public void RemoveCard()
    {
        CardManager.Instance.AddCardToUsed(CardData);
        Destroy(gameObject);
    }

    public bool CanBeSelected()
    {
        if (ActionPointManager.ActionPoints - CardData.cost < 0)
            return false;
        
        var gameState = GameManager.Instance.gameState;

        return gameState == GameState.Movement && CardData.cardType != CardType.Attack ||
               gameState == GameState.Attack && CardData.cardType == CardType.Attack;
    }
}
