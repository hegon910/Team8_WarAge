using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Game Data/Unit Data")]
public class Unit : ScriptableObject
{
    public string unitName;
    public int health;
    public int attackDamage;
    public float attackRange;
    public float attackSpeed;
    public float moveSpeed;
    public int goldCost;
    public Sprite unitSprite;
    public string description;
    public UnitType unitType;
    public EraType eraType;
}

public enum UnitType
{
    Melee,
    Ranged
}

public enum EraType
{
    StoneAge,
    MedievalAge,
    IndustrialAge,
    ModernAge,
    FutureAge
}
