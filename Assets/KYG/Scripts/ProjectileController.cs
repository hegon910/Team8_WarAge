// ProjectileController.cs (로직 재현 방식으로 동기화)

using Photon.Pun;
using UnityEngine;

namespace KYG
{
    // [핵심 수정] IPunObservable 인터페이스와 관련 로직을 모두 제거합니다.
    public class ProjectileController : MonoBehaviourPun
    {
        private Transform target;
        private int damage;
        private float speed;
        private string spawnerTeamTag;

        private PhotonView photonView;
        private Rigidbody2D rb;

        // --- 네트워크 동기화용 변수 모두 삭제 ---

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("ProjectileController에서 Rigidbody2D 컴포넌트를 찾지 못했습니다!", this.gameObject);
            }
        }

        public void Init(Transform target, int damage, float speed, string teamTag)
        {
            Setup(target, damage, speed, teamTag);
        }

        [PunRPC]
        public void RPC_Initialize(int targetViewID, int damage, float speed, string teamTag)
        {
            // 모든 클라이언트가 이 RPC를 받아서 각자 Setup을 실행합니다.
            PhotonView targetPV = PhotonView.Find(targetViewID);
            if (targetPV != null)
            {
                Setup(targetPV.transform, damage, speed, teamTag);
            }
            // 타겟을 못찾은 경우, 소유자만 이 발사체를 파괴합니다.
            else if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        private void Setup(Transform newTarget, int newDamage, float newSpeed, string newTeamTag)
        {
            this.target = newTarget;
            this.damage = newDamage;
            this.speed = newSpeed;
            this.spawnerTeamTag = newTeamTag;

            // [핵심 수정] IsMine 체크를 제거하여, 모든 클라이언트가 발사 로직을 실행합니다.
            if (this.target != null)
            {
                Vector2 direction = (this.target.position - transform.position).normalized;
                rb.velocity = direction * speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // [중요] 데미지 처리는 소유자만 하도록 IsMine 체크를 유지합니다.
            if (!photonView.IsMine) return;

            UnitController enemyUnit = other.GetComponent<UnitController>();
            if (enemyUnit != null && IsEnemy(enemyUnit))
            {
                enemyUnit.photonView.RPC("RpcTakeDamage", RpcTarget.All, damage);
                DestroyProjectile();
            }
            else if (other.TryGetComponent(out BaseController baseCtrl) && IsEnemyBase(baseCtrl))
            {
                baseCtrl.photonView.RPC("RpcTakeDamage", RpcTarget.All, damage, spawnerTeamTag);
                DestroyProjectile();
            }
        }

        private bool IsEnemy(UnitController targetUnit)
        {
            if (string.IsNullOrEmpty(targetUnit.TeamTag)) return false;
            bool isMyTeamP1 = this.spawnerTeamTag.Contains("P1");
            bool isTargetTeamP1 = targetUnit.TeamTag.Contains("P1");
            return isMyTeamP1 != isTargetTeamP1;
        }

        private bool IsEnemyBase(BaseController targetBase)
        {
            if (string.IsNullOrEmpty(targetBase.TeamTag)) return false;
            bool isMyTeamP1 = this.spawnerTeamTag.Contains("P1");
            bool isTargetTeamP1 = targetBase.TeamTag.Contains("P1");
            return isMyTeamP1 != isTargetTeamP1;
        }

        // --- OnPhotonSerializeView 함수 완전 삭제 ---

        // [핵심 수정] Update 로직을 모두에게 동일하게 적용합니다.
        private void Update()
        {
            RotateTowardsTarget();
        }

        private void RotateTowardsTarget()
        {
            if (rb.velocity.sqrMagnitude > 0.1)
            {
                transform.right = rb.velocity;
            }
        }

        private void DestroyProjectile()
        {
            if (PhotonNetwork.IsConnected && photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else if (!PhotonNetwork.IsConnected)
            {
                Destroy(gameObject);
            }
        }
    }
}