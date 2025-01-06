using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Unit", menuName = "Units/New Unit", order = 1)]
public class UnitData : ScriptableObject
{
    public string unitName;

    public int health;
    public int moveAmount;
    public int attackDamage;

    public float stepDuration;
}
