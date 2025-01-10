using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    public UnityEvent CardDeselected = new();

    [SerializeField] private GameObject cardPrefab;

    [Header("Hand Positioning")] 
    [SerializeField] private float cardWidth = 100f;
    [SerializeField] private float bottomDistance = 105f;

    private List<Card> _handCards = new();

    public Card SelectedCard { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
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

        UpdateCardPositions();
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
        ActionPointManager.Instance.UpdateActionPoints(-SelectedCard.CardData.cost);
        
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
        var startPos = Screen.width / 2f - _handCards.Count / 2f * cardWidth + cardWidth / 2f;
        for (int i = 0; i < _handCards.Count; i++)
        {
            _handCards[i].UpdatePosition(new Vector2(startPos + i * cardWidth, bottomDistance));
        }
    }

    // Deselect cards on end of turn
    private void OnGameStateChanged(GameState newState)
    {
        if(SelectedCard == null)
            return;
        
        DeselectCurrentCard();
    }
}
