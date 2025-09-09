using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HealCard", menuName = "Cards/Heal")]
public class HealCardData : CardData
{
    public int healAmount;
    
    public override bool IsCorrectPhase() => true;
    
    public override bool CanBeUsed(TileData? hoveredTile, Unit? selectedUnit)
    {
        return hoveredTile?.Unit != null && hoveredTile.Unit.owningTeam == Player.LocalPlayer.team &&
               !hoveredTile.Unit.HasMaxHealth;
    }

    protected override void UseCard(TileData? hoveredTile, Unit? selectedUnit)
    {
        if (hoveredTile == null || hoveredTile.Unit == null)
            throw new NullReferenceException("[StunCardData] Stun execution called without having a valid target tile");
        
        hoveredTile.Unit.CmdUpdateHealth(healAmount);
        HandManager.Instance.PlaySelectedCard();
    }
}