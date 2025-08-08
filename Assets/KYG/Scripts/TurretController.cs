// TurretController.cs (UnitController 로직 적용 최종본)

using Photon.Pun;
using UnityEngine;

namespace KYG
{
    public class TurretController : MonoBehaviourPun
    {
        public TurretData data;
        public Transform muzzlePoint;
        // public LayerMask enemyLayerMask; // 더 이상 사용하지 않으므로 삭제

        private TurretSlot parentSlot;
        private Transform target;
        private float attackTimer = 0f;

        public string TeamTag { get; private set; }

        public void Init(TurretData data, TurretSlot slot, string teamTag)
        {
            this.data = data;
            this.parentSlot = slot;
            this.TeamTag = teamTag;

            if (TeamTag == "BaseP1")
            {
                gameObject.tag = "P1Turret";
                gameObject.layer = LayerMask.NameToLayer("P1Turret");
            }
            else if (TeamTag == "BaseP2")
            {
                gameObject.tag = "P2Turret";
                gameObject.layer = LayerMask.NameToLayer("P2Turret");

                Vector3 currentScale = transform.localScale;
                transform.localScale = new Vector3(-currentScale.x, currentScale.y, currentScale.z);

                if (muzzlePoint != null)
                {
                    Vector3 muzzleScale = muzzlePoint.localScale;
                    muzzlePoint.localScale = new Vector3(-muzzleScale.x, muzzleScale.y, muzzleScale.z);
                }
            }
        }

        private void Update()
        {
            bool isTargetInvalid = false;
            if (target != null)
            {
                // 타겟이 비활성화(사망 등)되었는지 확인
                if (!target.gameObject.activeInHierarchy)
                {
                    isTargetInvalid = true;
                }
                else
                {
                    Collider2D targetCollider = target.GetComponent<Collider2D>();
                    if (targetCollider == null || !targetCollider.enabled)
                    {
                        isTargetInvalid = true;
                    }
                }
            }

            if (data == null)
            {
                return;
            }

            if (target == null || isTargetInvalid || Vector3.Distance(transform.position, target.position) > data.attackRange)
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

        // [핵심 수정] UnitController의 FindTarget 로직을 그대로 가져와 적용
        private Transform FindNearestEnemy()
        {
            if (data == null || string.IsNullOrEmpty(TeamTag))
            {
                return null;
            }

            // UnitController와 동일하게, 자신의 태그를 기준으로 적의 태그를 결정합니다.
            string opponentUnitTag = this.TeamTag.Contains("P1") ? "P2" : "P1";
            string opponentBaseTag = this.TeamTag.Contains("P1") ? "BaseP2" : "BaseP1";

            Transform nearest = null;
            float minDist = float.MaxValue;

            // UnitController와 동일하게, 태그로 모든 적 유닛을 찾습니다.
            GameObject[] enemyUnits = GameObject.FindGameObjectsWithTag(opponentUnitTag);
            foreach (var enemyUnit in enemyUnits)
            {
                // 죽은 유닛(콜라이더 비활성화)은 건너뛰는 방어 코드
                Collider2D col = enemyUnit.GetComponent<Collider2D>();
                if (col == null || !col.enabled)
                {
                    continue;
                }

                float dist = Vector3.Distance(transform.position, enemyUnit.transform.position);

                // 터렛의 공격 범위(attackRange) 안에 있는 가장 가까운 적을 찾습니다.
                if (dist < minDist && dist <= data.attackRange)
                {
                    minDist = dist;
                    nearest = enemyUnit.transform;
                }
            }

            // 공격 범위 내에 유닛이 없으면, 베이스를 타겟으로 삼습니다.
            if (nearest == null)
            {
                GameObject enemyBaseGO = GameObject.FindGameObjectWithTag(opponentBaseTag);
                if (enemyBaseGO != null && Vector3.Distance(transform.position, enemyBaseGO.transform.position) <= data.attackRange)
                {
                    nearest = enemyBaseGO.transform;
                }
            }

            return nearest;
        }

        // IsEnemy 함수는 더 이상 사용되지 않지만, 만약을 위해 그대로 둡니다.
        private bool IsEnemy(UnitController targetUnit)
        {
            bool isMyTeamP1 = this.TeamTag.Contains("P1");
            bool isTargetTeamP1 = targetUnit.gameObject.CompareTag("P1");
            return isMyTeamP1 != isTargetTeamP1;
        }

        private void FireProjectile()
        {
            if (target == null || data.projectilePrefab == null) return;
            Vector3 spawnPosition = (muzzlePoint != null) ? muzzlePoint.position : transform.position;

            if (InGameManager.Instance.isDebugMode && !PhotonNetwork.IsConnected)
            {
                GameObject projectile = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
                var controller = projectile.GetComponent<ProjectileController>();
                if (controller != null)
                {
                    controller.Init(target, data.attackDamage, data.projectileSpeed, TeamTag);
                }
            }
            else
            {
                PhotonView targetPV = target.GetComponent<PhotonView>();
                if (targetPV == null)
                {
                    Debug.LogError("네트워크 타겟이 PhotonView를 가지고 있지 않아 공격할 수 없습니다.");
                    return;
                }
                GameObject projectile = PhotonNetwork.Instantiate(data.projectilePrefab.name, spawnPosition, Quaternion.identity);
                projectile.GetComponent<PhotonView>().RPC("RPC_Initialize", RpcTarget.All,
                                                           targetPV.ViewID,
                                                           data.attackDamage,
                                                           data.projectileSpeed,
                                                           TeamTag);
            }
        }
    }
}