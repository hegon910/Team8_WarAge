using Photon.Pun;
using UnityEngine;

namespace KYG
{

    public class TurretController : MonoBehaviourPun
    {
        public TurretData data; // 터렛 데이터
        public Transform muzzlePoint;
        private TurretSlot parentSlot; // 설치된 슬롯 참조

        private Transform target; // 현재 공격 중인 타겟
        private float attackTimer = 0f; // 공격 딜레이 타이머

        public string TeamTag { get; private set; }  // 팀 정보 추가

        /// <summary>
        /// 터렛 초기화
        /// </summary>
        /// <param name="data"></param>
        /// <param name="slot"></param>
        /// 

        public void Init(TurretData data, TurretSlot slot, string teamTag)
        {
            Debug.Log($"TurretController.Init 호출됨. 전달받은 teamTag 값: '{teamTag}'"); // <-- 이 로그를 추가
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
            bool isTargetInvalid = false;
            if (target != null)
            {
                Collider2D targetCollider = target.GetComponent<Collider2D>();
                // 타겟의 콜라이더가 없거나 비활성화 상태이면 무효한 타겟으로 간주
                if (targetCollider == null || !targetCollider.enabled)
                {
                    isTargetInvalid = true;
                }
            }
            if (data == null)
            {
                Debug.LogWarning($"{gameObject.name}: TurretData가 초기화되지 않았습니다!");
                return;
            }

            // 타겟이 없거나 사거리 밖이면 다시 찾기
            if (target == null || isTargetInvalid || Vector3.Distance(transform.position, target.position) > data.attackRange)
            {
                target = FindNearestEnemy();
            }

            // 유효한 타겟이 있을 때만 공격
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

                // [수정] 적의 콜라이더가 활성화 상태인지 확인하는 조건 추가
                Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
                if (enemyCollider == null || !enemyCollider.enabled)
                {
                    continue; // 콜라이더가 없거나 꺼져있으면 이 적은 건너뜀
                }

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
            return TeamTag == "BaseP1" ? "P2" : "P1";
        }

        /// <summary>
        /// 발사체 발사
        /// </summary>
        private void FireProjectile()
        {
            if (target == null || data.projectilePrefab == null) return;

            Vector3 spawnPosition = (muzzlePoint != null) ? muzzlePoint.position : transform.position;

            // [수정] 발사체 생성 및 초기화 로직 분기 처리
            if (InGameManager.Instance.isDebugMode && !PhotonNetwork.IsConnected)
            {
                // --- 오프라인 & 디버그 모드 ---
                GameObject projectile = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
                var controller = projectile.GetComponent<ProjectileController>();
                if (controller != null)
                {
                    // [기존 로직 유지] 로컬 객체이므로 Init()을 직접 호출합니다.
                    controller.Init(target, data.attackDamage, data.projectileSpeed, TeamTag);
                }
            }
            else
            {
                // --- 온라인 네트워크 모드 ---
                PhotonView targetPV = target.GetComponent<PhotonView>();
                if (targetPV == null)
                {
                    Debug.LogError("네트워크 타겟이 PhotonView를 가지고 있지 않아 공격할 수 없습니다.");
                    return;
                }

                GameObject projectile = PhotonNetwork.Instantiate(data.projectilePrefab.name, spawnPosition, Quaternion.identity);

                // [수정] Init() 대신, RPC를 통해 모든 클라이언트에게 초기화 명령을 내립니다.
                projectile.GetComponent<PhotonView>().RPC("RPC_Initialize", RpcTarget.All,
                                                           targetPV.ViewID,
                                                           data.attackDamage,
                                                           data.projectileSpeed,
                                                           TeamTag);
            }
        }

    }
}
