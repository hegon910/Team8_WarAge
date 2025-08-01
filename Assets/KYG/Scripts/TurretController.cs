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
            
            // Layer 설정 (유닛과 동일한 팀에 맞춤)
            if (TeamTag == "P1")
                gameObject.layer = LayerMask.NameToLayer("P1Turret");
            else if (TeamTag == "P2")
                gameObject.layer = LayerMask.NameToLayer("P2Turret");
            
        }

        private void Update()
        {   
            // Photon 소유자가 아니면 Update 비활성
            if(!photonView.IsMine) return;
            
            // 타겟이 없거나 사거리 벗어나면 다시 탐색
            if (target == null || Vector3.Distance(transform.position, target.position) > data.attackRange)
            
                target = FindNearestEnemy();
            // 타겟 발견시 공격 타이머 증가
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
            // 팀별로 적 Layer만 탐색하도록 변경
            string enemyLayer = TeamTag == "P1" ? "P2Unit" : "P1Unit";
            Collider[] hits = Physics.OverlapSphere(transform.position, data.attackRange, LayerMask.GetMask(enemyLayer));

            // 추가: 기지도 타겟으로
            string enemyBaseLayer = TeamTag == "P1" ? "P2Base" : "P1Base";
            Collider[] baseHits = Physics.OverlapSphere(transform.position, data.attackRange, LayerMask.GetMask(enemyBaseLayer));

            if (hits.Length > 0) return hits[0].transform;
            if (baseHits.Length > 0) return baseHits[0].transform;

            return null;
        }
        
        /// <summary>
        /// 발사체 생성 타겟 발사
        /// </summary>
        private void FireProjectile()
        {
            // Photon 네트워크로 발사체 생성(모든 크라이언트 초기화)
            if(target == null) return;
            
            // ProjectileController 초기화
            GameObject projObj = PhotonNetwork.Instantiate(data.projectilePrefab.name, transform.position, Quaternion.identity);
            if (projObj.TryGetComponent(out ProjectileController projectile))
            {
                projectile.Init(target, data.attackDamage, data.projectileSpeed, TeamTag); // TeamTag 전달
            }
        }
    }
}
