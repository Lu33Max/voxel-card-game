using UnityEngine;

public abstract class CardData : ScriptableObject
{
    public enum Type
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
    
    public string cardName = null!;
    public int cost;
    
    public Type type;
    public Rarity rarity;
    
    public Sprite cardSprite = null!; 
    
    /// <summary> Returns whether the given card is an item card or an action card </summary>
    public bool IsDisposable() => type is not Type.Attack and not Type.Move;

    /// <summary> Returns whether the card can be used this turn </summary>
    public abstract bool IsCorrectPhase();
    
    /// <summary>
    ///     Try using the card by first checking whether it can be used with the given parameters or else return without
    ///     execution.
    /// </summary>
    /// <param name="hoveredTile"> The tile currently being hovered by the cursor </param>
    /// <param name="selectedUnit"> The currently selected unit </param>
    /// <returns> Boolean indicating whether the usage was successful or aborted </returns>
    public bool TryUseCard(TileData? hoveredTile, Unit? selectedUnit)
    {
        if (!CanBeUsed(hoveredTile, selectedUnit)) return false;
        
        UseCard(hoveredTile, selectedUnit);
        return true;
    }
    
    /// <summary> Checks whether the card can be used on the given tile or unit </summary>
    /// <param name="hoveredTile"> The tile currently being hovered by the cursor </param>
    /// <param name="selectedUnit"> The currently selected unit </param>
    public abstract bool CanBeUsed(TileData? hoveredTile, Unit? selectedUnit);
    
    /// <summary>
    ///     Executes the effect the card on the given unit or tile. Requires <see cref="CanBeUsed"/> to be called first
    ///     for validity checks.
    /// </summary>
    /// <param name="hoveredTile"> The tile currently being hovered by the cursor </param>
    /// <param name="selectedUnit"> The currently selected unit </param>
    protected abstract void UseCard(TileData? hoveredTile, Unit? selectedUnit);
}