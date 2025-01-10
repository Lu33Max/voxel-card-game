using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [SerializeField] private List<CardData> deck;
    [SerializeField] private int drawCardCost = 1;

    private List<CardData> _usedCards = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void DrawCard()
    {
        // Can only draw cards if the player has action points left this round
        if (ActionPointManager.ActionPoints == 0)
            return;
            
        // Reset all used cards back into the card pile
        if (deck.Count == 0)
        {
            if(_usedCards.Count == 0)
                return;
            
            deck = _usedCards.ToList();
            _usedCards.Clear();
        }

        // Get a random card from the deck and remove it
        CardData newCard = deck[Random.Range(0, deck.Count)];
        deck.Remove(newCard);
        
        HandManager.Instance.AddCardToHand(newCard);
        ActionPointManager.Instance.UpdateActionPoints(-drawCardCost);
    }

    public void AddCardToUsed(CardData card)
    {
        _usedCards.Add(card);
    }
}
