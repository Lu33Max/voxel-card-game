using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class CardComponent
{
    public CardData moveSide;
    public CardData attackSide;
}

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }
    
    [SerializeField] private List<CardComponent> deck;
    [SerializeField] private int drawCardCost = 1;

    private readonly List<CardComponent> _usedCards = new();
    
    public void Initialize()
    {
        if (Instance != null && Instance != this)
            return;

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
            
            // Make copy of list and assign it
            deck = _usedCards.ToList();
            _usedCards.Clear();
        }
        
        // Get a random card from the deck and remove it
        var newCard = deck[Random.Range(0, deck.Count)];
        deck.Remove(newCard);
        
        HandManager.Instance.AddCardToHand(newCard);
        ActionPointManager.Instance.UpdateActionPoints(-drawCardCost);
    }

    public void AddCardToUsed(CardComponent card)
    {
        _usedCards.Add(card);
    }
}
