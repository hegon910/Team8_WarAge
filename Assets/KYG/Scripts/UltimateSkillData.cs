using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KYG
{
    /// <summary>
    /// 시대별 궁극기 정보 저장 ScriptableObject
    /// </summary> 

    [CreateAssetMenu(fileName = "UltimateSkillData", menuName = "Game/Skill/UltimateSkillData", order = 0)]
    public class UltimateSkillData : ScriptableObject
    {
        [Header("공통 정보")]
        public string skillName;                  // 스킬 이름
        public float cooldownTime = 30f;          // 쿨타임 (초)
        public Sprite skillIcon;              // 스킬 아이콘 (UI 표시용)

        [Header("투사체 정보")]
        public GameObject projectilePrefab;       // 투사체 프리팹
        public int projectileCount = 5;           // 투사체 개수
        public float damage = 100f;               // 데미지
        public float areaRadius = 3f;             // 범위 데미지 반경

        [Header("이펙트 & 사운드")]
        public GameObject effectPrefab;           // 화면 연출용 이펙트 (폭발 등)
        public AudioClip skillSound;              // 사운드

        [Header("스킬 타입")]
        public SkillType skillType;

        public enum SkillType
        {
            MeteorRain,        // 고대 - 메테오
            CatapultBombard,   // 중세 - 투석기 폭격
            MissileStrike      // 현대 - 미사일 폭격
        }
    }
}
