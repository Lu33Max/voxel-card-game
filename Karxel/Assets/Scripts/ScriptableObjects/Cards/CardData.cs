using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Cards/New Card", order = 1)]
public class CardData : ScriptableObject
{
    public string cardName;
    public int cost;
    
    public CardType cardType;
    public Rarity rarity;
    
    public Sprite cardSprite; 
    
    public int movementRange;
    public int attackDamage;
    public int attackRange;
    public int otherValue;
}

public enum CardType
{
    Attack,
    Move,
    Heal,
    Shield,
    Stun
}

public enum Rarity
{
    Common,
    Rare
}
