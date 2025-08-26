using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    public UnityEvent cardDeselected = new();
    
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private AudioClip drawCardSound;

    [Header("Canvas Ref")]
    [SerializeField] private float canvasWidth = 1920;
    [SerializeField] private float canvasHeight = 1080;

    [Header("Hand Positioning")]
    [SerializeField] private float cardWidth = 100f;
    [SerializeField] private float borderWidth = 150f;
    [SerializeField] private float cardRegularY = 75f;
    [SerializeField] private float cardLoweredY = 20f;
    [SerializeField] private float cardRaisedY = 200f;

    [Header("Mouse Follower")]
    [SerializeField] private MouseFollower mouseFollower;

    [Header("Discarding")] 
    [SerializeField] private int discardForActionCount = 2;
    [SerializeField] private int actionsGained = 1;
    [SerializeField] private Transform discardContainer;
    
    private List<Card> _handCards = new();
    private int _discardCount;
    
    public Card SelectedCard { get; private set; }
    
    public void Initialize()
    {
        if (Instance != null && Instance != this)
            return;
        
        Instance = this;
    }

    private void OnEnable()
    {
        GameManager.Instance.GameStateChanged += OnGameStateChanged;
     
        // TODO: Check if mouse is set in inspector or this is still needed
        mouseFollower = FindObjectOfType<MouseFollower>();
        if (mouseFollower == null)
            Debug.LogWarning("No MouseFollower Object found");
    }

    private void OnDisable()
    {
        GameManager.Instance.GameStateChanged -= OnGameStateChanged;
    }

    /// <summary> Adds new card to the deck and does the setup for its values </summary>
    public void AddCardToHand(CardData newCardData)
    {
        var newCardObject = Instantiate(cardPrefab, transform);
        var newCard = newCardObject.GetComponent<Card>();
        newCard.Initialize(newCardData, new Vector2(0, 105));
        
        _handCards.Add(newCard);
        _handCards = _handCards.OrderBy(c => c.CardData.cardType).ThenBy(c => c.CardData.cardName).ToList();
        
        if(SelectedCard != null) DeselectCurrentCard();
        else AudioManager.Instance.PlaySfx(drawCardSound);
        
        UpdateCardPositions(GameManager.Instance.gameState);
    }

    /// <summary>
    ///     Handle the logic for selecting and deselecting a card depending on whether it was already selected. At this 
    ///     point it is already checked whether the card is actually selectable.
    /// </summary>
    public void CardClicked(Card clickedCard)
    {
        if(SelectedCard != null)
            SelectedCard.UpdateYPosition(cardRegularY);
        
        // If the already selected card was clicked again, simply reset the card reference to null
        if (SelectedCard == clickedCard)
        {
            DeselectCurrentCard();
            return;
        }
        
        AudioManager.Instance.PlaySfx(drawCardSound);
        
        cardDeselected.Invoke();
        
        SelectedCard = clickedCard;
        SelectedCard.UpdateYPosition(cardRaisedY);
        
        mouseFollower.SetUIElement(SelectedCard.gameObject);
    }

    public void PlaySelectedCard()
    {
        ActionPointManager.Instance.UpdateActionPointsOnPlay(-SelectedCard.CardData.cost);
        
        SelectedCard.RemoveCard();
        _handCards.Remove(SelectedCard);
        SelectedCard = null;
        
        mouseFollower.ClearUIElement();
        
        AudioManager.Instance.PlaySfx(drawCardSound);
        UpdateCardPositions(GameManager.Instance.gameState);
    }

    public void DeselectCurrentCard()
    {
        AudioManager.Instance.PlaySfx(drawCardSound);
        
        cardDeselected?.Invoke();
        SelectedCard.UpdateYPosition(cardRegularY);
        SelectedCard = null;
        
        mouseFollower.ClearUIElement();
    }

    public void DiscardCard(Card card)
    {
        if(card == null)
            return;

        _discardCount++;

        if (_discardCount == discardForActionCount)
        {
            _discardCount = 0;
            ActionPointManager.Instance.UpdateActionPoints(actionsGained);
        }
        
        for (var i = 0; i < discardContainer.childCount; i++)
        {
            var cardImage = discardContainer.GetChild(i).GetComponent<Image>();
            cardImage.color = i < _discardCount ? Color.white : Color.gray;
        }
        
        card.RemoveCard();
        _handCards.Remove(card);

        if (card == SelectedCard)
        {
            cardDeselected?.Invoke();
            SelectedCard = null;   
            
            mouseFollower.ClearUIElement();
        }
        
        UpdateCardPositions(GameManager.Instance.gameState);
    }

    private void UpdateCardPositions(GameState state)
    {
        var cardRows = new List<List<Card>> { _handCards.Where(c => c.IsCorrectPhase(state)).ToList() };
        cardRows.Add(_handCards.Except(cardRows[0]).ToList());
        
        var cardCount = 0;
        
        foreach (var cardRow in cardRows)
        {
            var hasOverflow = cardRow.Count * cardWidth > canvasWidth - 2 * borderWidth;
        
            var startPos = hasOverflow 
                ? borderWidth + cardWidth / 2f - canvasWidth / 2 
                : -cardRow.Count / 2f * cardWidth + cardWidth / 2f;
        
            var lastPos = hasOverflow 
                ? -borderWidth - cardWidth / 2f + canvasWidth / 2
                : cardRow.Count / 2f * cardWidth - cardWidth / 2f;

            if (cardRow.Count == 1)
            {
                cardRow[0].UpdatePosition(new Vector2(startPos,
                    cardRow[0].IsCorrectPhase(state) ? cardRegularY : cardLoweredY));
                
                cardRow[0].transform.SetSiblingIndex(cardCount);
                cardCount++;
                
                cardRow[0].UpdateState(state);
                continue;
            }
        
            for (var i = 0; i < cardRow.Count; i++)
            {
                cardRow[i].UpdatePosition(new Vector2(Mathf.Lerp(startPos, lastPos, i / (cardRow.Count - 1f)),
                    cardRow[i].IsCorrectPhase(state) ? cardRegularY : cardLoweredY));
                
                cardRow[i].transform.SetSiblingIndex(cardCount);
                cardCount++;
                
                cardRow[i].UpdateState(state);
            }  
        }
    }

    // Deselect cards on end of turn
    private void OnGameStateChanged(GameState newState)
    {
        if(SelectedCard != null)
            DeselectCurrentCard();
        
        if(newState is GameState.Attack or GameState.Movement)
            UpdateCardPositions(newState);
    }
}
