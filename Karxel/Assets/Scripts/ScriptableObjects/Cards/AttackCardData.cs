using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackCard", menuName = "Cards/Attack")]
public class AttackCardData : CardData
{
    public int damageMultiplier;
    
    public override bool IsCorrectPhase() => GameManager.Instance.gameState == GameState.Attack;
    
    public override bool CanBeUsed(TileData? hoveredTile, Unit? selectedUnit)
    {
        var attackCommand = hoveredTile == null || selectedUnit == null
            ? null
            : selectedUnit.GetAttackForHoverPosition(hoveredTile.TilePosition, damageMultiplier);

        return attackCommand != null && GameManager.Instance.gameState == GameState.Attack;
    }

    protected override void UseCard(TileData? hoveredTile, Unit? selectedUnit)
    {
        if (selectedUnit == null || hoveredTile == null)
            throw new NullReferenceException(
                "[AttackCardData] Attack execution called without having a unit or target selected");
        
        var attackCommand = selectedUnit.GetAttackForHoverPosition(hoveredTile.TilePosition, damageMultiplier)!;
        
        HandManager.Instance.PlaySelectedCard();
        selectedUnit.ExecuteAttackLocally(attackCommand, this);
    }
}