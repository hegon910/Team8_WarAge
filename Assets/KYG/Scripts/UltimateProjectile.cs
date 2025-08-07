using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace KYG
{
    
public class UltimateProjectile : MonoBehaviourPun
{
        private float damage;
        private float radius;
        private float speed = 15f;
        private bool hasImpacted = false;
        private string opponentUnitTag;

        [Header("충돌 시 폭발 이펙트")]
        public GameObject impactEffectPrefab;

        private string ownerTag; // P1 or P2

        /// <summary>
        /// 투사체 초기화: 목표 위치, 데미지, 범위, 팀 태그
        /// </summary>
        [PunRPC]
        public void Initialize(float dmg, float area, string tag)
        {
           
            damage = dmg;
            radius = area;
            ownerTag = tag;
            opponentUnitTag = (ownerTag == "P1") ? "P2" : "P1";
            hasImpacted = false;

            // 추가: 5초 후 자동 파괴 코루틴 시작
            StartCoroutine(DestroyAfterTime(5f));
        }

        private void Update()
        {
            // 충돌했다면 더 이상 움직이지 않음
            if (hasImpacted) return;

            // 지속적으로 아래로 이동
            transform.Translate(Vector3.down * speed * Time.deltaTime);

            // 화면 아래로 너무 많이 내려가면 자동으로 파괴 (안전장치)
            if (transform.position.y < -30f)
            {
                if (photonView.IsMine)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
            }
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 이미 충돌했거나, 내 유닛과 부딪혔거나, 상대 유닛이 아니면 무시
            if (hasImpacted || string.IsNullOrEmpty(opponentUnitTag) || !other.CompareTag(opponentUnitTag))
            {
                return;
            }

            // 충돌 판정은 마스터 클라이언트만 수행하여 중복 데미지 방지
            if (PhotonNetwork.IsMasterClient)
            {
                // 즉시 hasImpacted를 true로 설정하여 중복 호출 방지
                hasImpacted = true;

                // 충돌 지점 기준으로 범위 내 모든 유닛 검색
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag(opponentUnitTag))
                    {
                        var unit = hit.GetComponent<UnitController>();
                        if (unit != null)
                        {
                            unit.photonView.RPC("RpcTakeDamage", RpcTarget.All, (int)damage);
                        }
                    }
                }

                // 모든 클라이언트에게 폭발 이펙트 생성 및 투사체 파괴를 명령
                photonView.RPC("RPC_ImpactEffect", RpcTarget.All, transform.position);
            }
        }
        [PunRPC]
        private void RPC_ImpactEffect(Vector3 impactPosition)
        {
            // 이 RPC가 호출되면 hasImpacted를 true로 만들어 모든 동작을 멈춤
            hasImpacted = true;

            // 이펙트 생성
            if (impactEffectPrefab != null)
            {
                // 이펙트는 오브젝트 풀을 사용해도 되고, 일반 Instantiate를 써도 됩니다.
                // 여기서는 가독성을 위해 Instantiate를 사용합니다.
                Instantiate(impactEffectPrefab, impactPosition, Quaternion.identity);
            }

            // 오브젝트의 소유자만 네트워크상에서 이 오브젝트를 파괴할 수 있음
            // 이렇게 해야 오브젝트 풀을 사용하더라도 안전하게 파괴/반환 가능
            if (photonView.IsMine)
            {
                // PhotonNetwork.Instantiate로 생성했으므로 PhotonNetwork.Destroy로 제거하는 것이 가장 안전
                PhotonNetwork.Destroy(gameObject);
            }
        }
    
     //   private void Impact()
     //   {
     //       if (hasImpacted) return;
     //       hasImpacted = true;
     //
     //       if (PhotonNetwork.IsMasterClient)
     //       {
     //           string opponentUnitTag = (ownerTag == "P1") ? "P2" : "P1";
     //
     //           Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, radius);
     //           foreach (var hit in hits)
     //           {
     //               // 유닛 타격
     //               if (hit.CompareTag(opponentUnitTag))
     //               {
     //                   var unit = hit.GetComponent<UnitController>();
     //                   if (unit != null)
     //                   {
     //                       unit.photonView.RPC("RpcTakeDamage", RpcTarget.All, (int)damage);
     //                   }
     //               }
     //
     //               PhotonObjectPool.Instance.Release(gameObject);
     //           }
     //
     //           // 폭발 이펙트
     //           if (impactEffectPrefab != null)
     //           {
     //               GameObject impact = PhotonObjectPool.Instance.Spawn(impactEffectPrefab, targetPosition, Quaternion.identity);
     //               StartCoroutine(PhotonObjectPool.Instance.ReleaseAfterDelay(impact, 2f));
     //           }
     //
     //       }
     //
     //   }
        private IEnumerator DestroyAfterTime(float time)
        {
            yield return new WaitForSeconds(time);

            // 아직 파괴(Impact)되지 않았다면
            if (!hasImpacted && photonView.IsMine)
            {
                // 타임아웃으로 인한 파괴 시에는 폭발 이펙트는 생성하지 않습니다.
               PhotonObjectPool.Instance.Release(gameObject);
            }
        }
    }
}
