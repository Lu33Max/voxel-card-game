using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    public UnityEvent CardDeselected = new();
    
    [SerializeField] private GameObject cardPrefab;

    [Header("Hand Positioning")] 
    [SerializeField] private float cardWidth = 0.08f;
    [SerializeField] private float bottomDistance = 80f;
    [SerializeField] private float maxWidth = 0.8f;

    private List<Card> _handCards = new();

    private float _cardWidth;

    public Card SelectedCard { get; private set; }
    
    public void Initialize()
    {
        if (Instance != null && Instance != this)
            return;
        
        Instance = this;
        _cardWidth = cardWidth * Screen.width;
    }

    private void OnEnable()
    {
        GameManager.Instance.gameStateChanged.AddListener(OnGameStateChanged);
    }

    private void OnDisable()
    {
        GameManager.Instance.gameStateChanged.RemoveListener(OnGameStateChanged);
    }

    /// <summary>Adds new card to the deck and does the setup for its values</summary>
    public void AddCardToHand(CardData newCardData)
    {
        GameObject newCardGO = Instantiate(cardPrefab, transform);
        Card newCard = newCardGO.GetComponent<Card>();
        newCard.Initialize(newCardData, new Vector2(0, 105));
        
        _handCards.Add(newCard);
        _handCards.Sort((c1, c2) => c1.CardData.cardType < c2.CardData.cardType ? 1 : c1.CardData.cardType == c2.CardData.cardType ? 0 : -1);

        for (int i = 0; i < _handCards.Count; i++)
        {
            _handCards[i].transform.SetSiblingIndex(i);
        }

        // Logging
        GameManager.Instance.CmdLogAction(GameManager.Instance.localPlayer.netId.ToString(),
            GameManager.Instance.localPlayer.team.ToString(), "drawCard", $"[{newCardData.cardName}]", null, null, null,
            null);
        
        UpdateCardPositions();
        newCard.SetActiveState(newCard.IsCorrectPhase());
    }

    /// <summary>Handle the logic for selecting and deselecting a card depending on whether it was already selected</summary>
    public void CardClicked(Card clickedCard)
    {
        if(SelectedCard != null)
            SelectedCard.DeselectCard();

        // If the already selected card was clicked again, simply reset the card reference to null
        if (SelectedCard == clickedCard)
        {
            CardDeselected?.Invoke();
            SelectedCard = null;
            return;
        }
        
        SelectedCard = clickedCard;
        clickedCard.SelectCard();
    }

    public void PlaySelectedCard()
    {
        ActionPointManager.Instance.UpdateActionPointsOnPlay(-SelectedCard.CardData.cost);
        
        SelectedCard.RemoveCard();
        _handCards.Remove(SelectedCard);
        SelectedCard = null;
        
        UpdateCardPositions();
    }

    public void DeselectCurrentCard()
    {
        SelectedCard.DeselectCard();
        CardDeselected?.Invoke();
        SelectedCard = null;
    }

    private void UpdateCardPositions()
    {
        // If there are so many cards, they need to be placed overlapping
        if (_handCards.Count * _cardWidth > Screen.width * maxWidth)
        {
            var startPos = (1 - maxWidth) * Screen.width / 2 + _cardWidth / 2f;
            var lastPos = Screen.width - (1 - maxWidth) * Screen.width / 2 - _cardWidth / 2f;
            
            _handCards[0].UpdatePosition(new Vector2(startPos, bottomDistance));
            for (int i = 1; i < _handCards.Count; i++)
            {
                _handCards[i].UpdatePosition(new Vector2(Mathf.Lerp(startPos, lastPos, i / (_handCards.Count - 1f)), bottomDistance));
            }   
        }
        else
        {
            var startPos = Screen.width / 2f - _handCards.Count / 2f * _cardWidth + _cardWidth / 2f;
            for (int i = 0; i < _handCards.Count; i++)
            {
                _handCards[i].UpdatePosition(new Vector2(startPos + i * _cardWidth, bottomDistance));
            }   
        }
    }

    // Deselect cards on end of turn
    private void OnGameStateChanged(GameState newState)
    {
        if(SelectedCard != null)
            DeselectCurrentCard();

        foreach (var handCard in _handCards)
        {
            handCard.SetActiveState(handCard.IsCorrectPhase());
        }
    }
}
