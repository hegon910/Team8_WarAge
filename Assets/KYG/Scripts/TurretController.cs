using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace KYG
{
    
    public class TurretController : MonoBehaviourPun
    {
        private TurretData data; // 터렛 데이터
        private TurretSlot parentSlot; // 설치된 슬롯 참조

        private Transform target; // 현재 공격 중인 타겟
        private float attackTimer = 0f; // 공격 딜레이 타이머
        
        /// <summary>
        /// 터렛 초기화
        /// </summary>
        /// <param name="data"></param>
        /// <param name="slot"></param>
        public void Init(TurretData data, TurretSlot slot)
        {
            this.data = data;
            this.parentSlot = slot;
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
            Collider[] hits = Physics.OverlapSphere(transform.position, data.attackRange, LayerMask.GetMask("Enemy"));
            return hits.Length > 0 ? hits[0].transform : null;
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
                projectile.Init(target, data.attackDamage, data.projectileSpeed);
        }
    }
}
