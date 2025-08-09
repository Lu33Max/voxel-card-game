using UnityEngine;

[CreateAssetMenu(fileName = "Unit", menuName = "Units/New Unit", order = 1)]
public class UnitData : ScriptableObject
{
    public string unitName;
    [TextArea] public string unitDescription;

    public int health;
    public int moveAmount;
    public int attackDamage;
    public int maxHeightDiff = 1;
    public Tile.EdgeType[] traversableEdgeTypes = { Tile.EdgeType.None, Tile.EdgeType.Jump, Tile.EdgeType.Ladder };

    public float stepDuration;
}
