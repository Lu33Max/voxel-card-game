using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Cards/New Card", order = 1)]
public class CardData : ScriptableObject
{
    public string cardName;

    public CardType cardType;

    public Sprite cardSprite;
}

public enum CardType
{
    Attack,
    Move
}
