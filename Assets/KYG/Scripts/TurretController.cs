using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace KYG
{
    
    public class TurretController : MonoBehaviourPun
    {
        public TurretData data; // 터렛 데이터
        private TurretSlot parentSlot; // 설치된 슬롯 참조

        private Transform target; // 현재 공격 중인 타겟
        private float attackTimer = 0f; // 공격 딜레이 타이머
        
        public string TeamTag { get; private set; }  // 팀 정보 추가
        
        /// <summary>
        /// 터렛 초기화
        /// </summary>
        /// <param name="data"></param>
        /// <param name="slot"></param>
        public void Init(TurretData data, TurretSlot slot, string teamTag)
        {
            this.data = data;
            this.parentSlot = slot;
            this.TeamTag = teamTag;
            
            // 태그, 레이어 자동 설정
            if (TeamTag == "BaseP1")
            {
                gameObject.tag = "P1Turret";
                gameObject.layer = LayerMask.NameToLayer("P1Turret");
            }
            else if (TeamTag == "BaseP2")
            {
                gameObject.tag = "P2Turret";
                gameObject.layer = LayerMask.NameToLayer("P2Turret");
            }
            
        }

        private void Update()
        {   
            if (data == null)
            {
                Debug.LogWarning($"{gameObject.name}: TurretData가 초기화되지 않았습니다!");
                return;
            }

            // 타겟이 없거나 사거리 밖이면 다시 찾기
            if (target == null || Vector3.Distance(transform.position, target.position) > data.attackRange)
            {
                target = FindNearestEnemy();
            }

            if (target != null)
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= data.attackDelay)
                {
                    FireProjectile();
                    attackTimer = 0f;
                }
            }
        }
        
        /// <summary>
        /// 사거리 내 가까운 적 탐지
        /// </summary>
        /// <returns></returns>
        private Transform FindNearestEnemy()
        {
            string enemyTag = GetEnemyTag();

            GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
            Transform nearest = null;
            float minDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(transform.position, enemy.transform.position);

                if (dist < minDist && dist <= data.attackRange)
                {
                    minDist = dist;
                    nearest = enemy.transform;
                }
            }

            return nearest;
        }
        
        /// <summary>
        /// 팀 태그에 따라 적 태그 반환
        /// </summary>
        private string GetEnemyTag()
        {
            return TeamTag == "BaseP1" ? "P2Unit" : "P1Unit";
        }
        
        /// <summary>
        /// 발사체 발사
        /// </summary>
        private void FireProjectile()
        {
            if (target == null || data.projectilePrefab == null) return;

            GameObject projectile;

            if (InGameManager.Instance.isDebugMode || !PhotonNetwork.IsConnected)
            {
                projectile = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                projectile = PhotonNetwork.Instantiate(data.projectilePrefab.name, transform.position, Quaternion.identity);
            }

            var controller = projectile.GetComponent<ProjectileController>();
            if (controller != null)
            {
                controller.Init(target, data.attackDamage, data.projectileSpeed, TeamTag);
            }
        }
    }
}
