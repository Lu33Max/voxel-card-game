using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShieldCard", menuName = "Cards/Shield")]
public class ShieldCardData : CardData
{
    public override bool IsCorrectPhase() => GameManager.Instance!.gameState is GameState.Attack or GameState.Movement;
    
    public override bool CanBeUsed(TileData? hoveredTile, Unit? selectedUnit)
    {
        return hoveredTile?.Unit != null && hoveredTile.Unit.owningTeam == Player.LocalPlayer.team &&
               !hoveredTile.Unit.HasEffectOfTypeActive(Unit.StatusEffect.Shielded);
    }

    protected override void UseCard(TileData? hoveredTile, Unit? selectedUnit)
    {
        if (hoveredTile == null || hoveredTile.Unit == null)
            throw new NullReferenceException("[StunCardData] Stun execution called without having a valid target tile");
        
        hoveredTile.Unit.CmdAddNewStatusEffect(new Unit.UnitStatus{ Status = Unit.StatusEffect.Shielded, Duration = -1});
        HandManager.Instance.PlaySelectedCard();
    }
}