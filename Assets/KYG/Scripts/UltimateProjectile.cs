using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace KYG
{
    
public class UltimateProjectile : MonoBehaviourPun
{
    private Vector3 targetPosition;
        private float damage;
        private float radius;
        private float speed = 15f;

        [Header("충돌 시 폭발 이펙트")]
        public GameObject impactEffectPrefab;

        private string ownerTag; // P1 or P2

        /// <summary>
        /// 투사체 초기화: 목표 위치, 데미지, 범위, 팀 태그
        /// </summary>
        [PunRPC]
        public void Initialize(Vector3 target, float dmg, float area, string tag)
        {
            targetPosition = target;
            damage = dmg;
            radius = area;
            ownerTag = tag;
        }

        private void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                Impact();
            }
        }

        private void Impact()
        {
            string opponentUnitTag = (ownerTag == "P1") ? "P2" : "P1";
            string opponentBaseTag = (ownerTag == "P1") ? "BaseP2" : "BaseP1";

            Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, radius);
            foreach (var hit in hits)
            {
                // 유닛 타격
                if (hit.CompareTag(opponentUnitTag))
                {
                    var unit = hit.GetComponent<UnitController>();
                    if (unit != null)
                    {
                        unit.photonView.RPC("RpcTakeDamage", RpcTarget.All, (int)damage);
                    }
                }

                // 베이스 타격
                else if (hit.CompareTag(opponentBaseTag))
                {
                    var baseController = hit.GetComponent<BaseController>();
                    if (baseController != null)
                    {
                        baseController.RpcTakeDamage((int)damage, ownerTag);
                    }
                }
            }

            // 폭발 이펙트
            if (impactEffectPrefab != null)
            {
                GameObject impact = PhotonObjectPool.Instance.Spawn(impactEffectPrefab, targetPosition, Quaternion.identity);
                StartCoroutine(PhotonObjectPool.Instance.ReleaseAfterDelay(impact, 2f));
            }

            // 자신 반환
            PhotonObjectPool.Instance.Release(gameObject);
        }
    
}
}
