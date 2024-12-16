using UnityEngine;

[CreateAssetMenu(fileName = "Figure", menuName = "Figures/New Figure", order = 1)]
public class FigureData : ScriptableObject
{
    public string figureName;

    public int health;
    public int moveAmount;
    public int attackDamage;

    public Vector2Int[] movementPatter;
    public Vector2Int[] attackPatter;
}
