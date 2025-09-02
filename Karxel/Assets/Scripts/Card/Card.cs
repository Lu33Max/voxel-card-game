using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Sprite cardBGRare;
    [SerializeField] private Image backgroundImage;

    public CardData CardData => GameManager.Instance != null &&
                                GameManager.Instance.gameState is GameState.Movement or GameState.MovementExecution
        ? _cardData.moveSide
        : _cardData.attackSide;

    private CardComponent _cardData;
    private RectTransform _transform;
    
    public void Initialize(CardComponent data, Vector2 startPos)
    {
        _cardData = data;
        
        _transform = GetComponent<RectTransform>();
        _transform.position = new Vector3(0, startPos.y, 0);

        SetCardDesign();
    }

    public void SwapActiveFace()
    {
        StartCoroutine(RotateCard());
    }

    private void SetCardDesign()
    {
        pointText.text = CardData.cost.ToString();
        nameText.text = CardData.cardName;
        cardImage.sprite = CardData.cardSprite;
        backgroundImage.sprite = CardData.rarity == Rarity.Common ? cardBGRegular : cardBGRare;
        
        UpdateState(GameManager.Instance.gameState);
    }

    private void OnEnable()
    {
        ActionPointManager.Instance.actionPointsUpdated.AddListener(OnActionPointsUpdated);
    }

    private void OnDisable()
    {
        ActionPointManager.Instance.actionPointsUpdated.RemoveListener(OnActionPointsUpdated);
        StopAllCoroutines();
    }

    public void UpdatePosition(Vector2 newPos)
    {
        _transform.anchoredPosition = new Vector3(newPos.x, newPos.y);
    }
    
    public void UpdateYPosition(float newYPos)
    {
        _transform.anchoredPosition = new Vector3(_transform.anchoredPosition.x, newYPos);
    }

    public void UpdateState(GameState state)
    {
        backgroundImage.sprite = !CanBeSelected(state) ? cardBGUnselected : CardData.rarity == Rarity.Common ? cardBGRegular : cardBGRare;
    }

    public void CardClickedButton()
    {
        if(!CanBeSelected(GameManager.Instance.gameState))
            return;
        
        HandManager.Instance.CardClicked(this);
    }

    public void RemoveCard()
    {
        CardManager.Instance.AddCardToUsed(_cardData);
        Destroy(gameObject);
    }

    private IEnumerator RotateCard()
    {
        while (transform.rotation.eulerAngles.y < 90)
        {
            transform.Rotate(new Vector3(0, 5, 0));
            yield return new WaitForEndOfFrame();
        }
        
        SetCardDesign();
        transform.rotation = Quaternion.Euler(0, 270, 0);
        
        while (transform.rotation.eulerAngles.y > 5)
        {
            transform.Rotate(new Vector3(0, 5, 0));
            yield return new WaitForEndOfFrame();
        }
        
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    private bool IsCorrectPhase(GameState state)
    {
        return state == GameState.Movement && CardData.cardType != CardType.Attack ||
               state == GameState.Attack && CardData.cardType != CardType.Move;
    }
    
    private bool CanBeSelected(GameState state)
    {
        return ActionPointManager.ActionPoints - CardData.cost >= 0 && IsCorrectPhase(state);
    }

    private void OnActionPointsUpdated(int newPoints)
    {
        if (CardData != null && newPoints < CardData.cost)
            backgroundImage.sprite = cardBGUnselected;
    }
}
