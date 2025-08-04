using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KYG
{
    /// <summary>
    /// 시대별 정보를 담는 ScriptableObject
    /// 시대 종류
    /// 시대별 기지 최대 체력
    /// 기지 이미지
    /// 해당 시기 유닛 리스트
    /// </summary>
    
    [CreateAssetMenu(fileName = "AgeData", menuName = "Game/Age/AgeData", order = 0)]
    public class AgeData : ScriptableObject
    {
        [Header("기본 정보")]
        public AgeType ageType; // 시대 종류

        public int maxHP = 500; // 해당 시대의 기지 최대체력
        public int requiredExp; // 해당 시대의 경험치 필요량
        
        [Header("기지 시각 요소")]
        public Sprite baseSprite; // 기지 이미지

        public GameObject baseModelPrefab;  // 기지 프리팹
        
        [Header("시대별 유닛")]
        public List<GameObject> spawnableUnits = new(); // 해당 시대의 유닛 리스트
        [Header("시대별 터렛")]
        public List<TurretData> availableTurrets; // 해당 시대의 터렛 리스트
        
        [Header("궁극기 데이터")]
        public UltimateSkillData ultimateSkill;

    }
}
