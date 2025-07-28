using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace KYG
{
    
    public class ProjectileController : MonoBehaviourPun
    {
        private Transform target; // 발사체 목표
        private int damage; // 데미지
        private float speed; // 이동속도

        public void Init(Transform target, int damage, float speed) // 발사체 초기화
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;
        }

        private void Update()
        {
            if (target == null)
            {
                PhotonNetwork.Destroy(gameObject); // 타겟이 없으면 발사체 삭제
                return;
            }
            
            // 타겟 방향으로 이동
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.2f) // 타겟에 닿으면 명중
            {
                HitTarget();
            }
        }

        private void HitTarget() // 타겟명중 처리
        {
            if (target != null)
            {
                // PhotonView를 찾아 RPC로 데미지 전달
                PhotonView enemyPV = target.GetComponent<PhotonView>();
                if (enemyPV != null)
                {
                    enemyPV.RPC("TakeDamage", RpcTarget.All,damage);
                }
            }
            PhotonNetwork.Destroy(gameObject); // 발사체 제거
        }
    }
}
