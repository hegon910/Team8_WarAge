using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KYG
{
    [CreateAssetMenu(fileName = "TurretData", menuName = "GameData/TurretData")]
    public class TurretData : ScriptableObject // 시대,종류별 데이터를 스크립터블로 따로 관리
    {
        public string turretName; // 터렛 이름
        public GameObject turretPrefab; // 터렛 프리펩
        public int cost; // 구매 가격
        public int sellPrice; // 판매가격
        public float attackRange; // 공격 범위
        public float attackDelay; // 공격 속도
        public int attackDamage; // 공격 데미지
        public Sprite icon; // UI 표시 아이콘

        public GameObject projectilePrefab; // 투사체 프리펩
        public float projectileSpeed; // 투사체 발사 속도
    }
}
