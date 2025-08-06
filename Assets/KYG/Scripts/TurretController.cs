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
            return TeamTag == "BaseP1" ? "P2" : "P1";
        }

        /// <summary>
        /// 발사체 발사
        /// </summary>
        private void FireProjectile()
        {
            if (target == null || data.projectilePrefab == null) return;

            Vector3 spawnPosition = transform.position;
            if (muzzlePoint != null)
            {
                spawnPosition = muzzlePoint.position; // MuzzlePoint가 있으면 그 위치를 사용
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: MuzzlePoint가 설정되지 않아 터렛 위치에서 발사합니다.");
            }

            GameObject projectile;

            if (InGameManager.Instance.isDebugMode && !PhotonNetwork.IsConnected)
            {
                // 오프라인 & 디버그 모드
                projectile = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                // 그 외 모든 경우 (포톤에 연결된 모든 경우)
                projectile = PhotonNetwork.Instantiate(data.projectilePrefab.name, spawnPosition, Quaternion.identity);
            }

            var controller = projectile.GetComponent<ProjectileController>();
            if (controller != null)
            {
                controller.Init(target, data.attackDamage, data.projectileSpeed, TeamTag);
            }
        }

    }
}
