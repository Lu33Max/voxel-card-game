using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StunCard", menuName = "Cards/Stun")]
public class StunCardData : CardData
{
    public override bool IsCorrectPhase() => true;
    
    public override bool CanBeUsed(TileData? hoveredTile, Unit? selectedUnit)
    {
        return hoveredTile?.Unit != null && hoveredTile.Unit.owningTeam != Player.LocalPlayer.team &&
               !hoveredTile.Unit.HasEffectOfTypeActive(Unit.StatusEffect.Stunned, 2);
    }

    protected override void UseCard(TileData? hoveredTile, Unit? selectedUnit)
    {
        if (hoveredTile == null || hoveredTile.Unit == null)
            throw new NullReferenceException("[StunCardData] Stun execution called without having a valid target tile");
        
        hoveredTile.Unit.CmdAddNewStatusEffect(new Unit.UnitStatus{ Status = Unit.StatusEffect.PreStunned, Duration = 1 });
        HandManager.Instance.PlaySelectedCard();
    }
}