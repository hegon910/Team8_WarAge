using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Game Data/Unit Data")]
public class Unit : ScriptableObject
{
    public string unitName;
    public int health;
    public int attackDamage;
    public int rangedDamage;
    public float MeleeRange;
    public float rangedrange;
    public float attackSpeed;
    public float SpawnTime;
    public float moveSpeed = 1f;
    public int goldCost;
    public Sprite unitSprite;
    public string description;
    public GameObject ArrowPrefab;
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
