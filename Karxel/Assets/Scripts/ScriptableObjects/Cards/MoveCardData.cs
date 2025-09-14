using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveCard", menuName = "Cards/Move")]
public class MoveCardData : CardData
{
    public int moveDistance;

    public override bool IsCorrectPhase() => GameManager.Instance!.gameState == GameState.Movement;

    public override bool CanBeUsed(TileData? hoveredTile, Unit? selectedUnit)
    {
        var moveCommand = selectedUnit == null || hoveredTile == null
            ? null
            : selectedUnit.GetValidMoves(moveDistance)
                .FirstOrDefault(move => move.TargetPosition == hoveredTile.TilePosition);

        return moveCommand != null && GridManager.Instance!.IsMoveValid(moveCommand) &&
               GameManager.Instance!.gameState == GameState.Movement;
    }

    protected override void UseCard(TileData? hoveredTile, Unit? selectedUnit)
    {
        if (selectedUnit == null || hoveredTile == null)
            throw new NullReferenceException(
                "[MoveCardData] Move execution called without having a unit or target selected");
        
        var moveCommand = selectedUnit.GetValidMoves(moveDistance)
            .First(move => move.TargetPosition == hoveredTile.TilePosition);
        
        HandManager.Instance.PlaySelectedCard();
        selectedUnit.ExecuteMoveLocally(moveCommand, this);
    }
}