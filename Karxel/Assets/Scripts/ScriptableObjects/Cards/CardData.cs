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
    
    /// <summary> Returns whether the given card is an item card or an action card </summary>
    public bool IsDisposable()
    {
        return cardType is not CardType.Attack and not CardType.Move;
    }
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
