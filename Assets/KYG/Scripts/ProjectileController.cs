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
        private string teamTag;  // 소속 팀 정보 추가

        public void Init(Transform target, int damage, float speed, string teamTag) // 발사체 초기화
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;
            this.teamTag = teamTag;
        }

        private void Update()
        {
            if (target == null)
            {
               if(photonView.IsMine) PhotonNetwork.Destroy(gameObject); // 타겟이 없으면 발사체 삭제
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
            if (target == null) return;

            // BaseController에 데미지 전달 시 아군인지 체크
            if (target.TryGetComponent(out BaseController baseCtrl))
            {
                baseCtrl.TakeDamage(damage, teamTag);
            }
            else if (target.TryGetComponent(out UnitController unitCtrl))
            {
                // 유닛은 기존 TakeDamage 사용 (UnitController 내부에서 아군 방어 가능)
                unitCtrl.TakeDamage(damage);
            }
            
            // 소유자만 발사체 제거 권한 있음
            if (photonView.IsMine)
                PhotonNetwork.Destroy(gameObject); // 발사체 제거
        }
    }
}
