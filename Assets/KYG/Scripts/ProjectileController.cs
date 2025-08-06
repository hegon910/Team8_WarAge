// ProjectileController.cs

using Photon.Pun;
using UnityEngine;

namespace KYG
{
    public class ProjectileController : MonoBehaviourPun
    {
        private Transform target;
        private int damage;
        private float speed;
        private string spawnerTeamTag;

        private PhotonView photonView;
        private Rigidbody2D rb;

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("ProjectileController에서 Rigidbody2D 컴포넌트를 찾지 못했습니다!", this.gameObject);
                return;
            }
            rb.isKinematic = true;
        }

        // [핵심] 오프라인 디버그 모드용 초기화 함수
        public void Init(Transform target, int damage, float speed, string teamTag)
        {
            // 실제 초기화 로직은 공통 Setup 함수에 위임
            Setup(target, damage, speed, teamTag);
        }

        // [핵심] 온라인 네트워크 모드용 초기화 함수 (RPC)
        [PunRPC]
        public void RPC_Initialize(int targetViewID, int damage, float speed, string teamTag)
        {
            PhotonView targetPV = PhotonView.Find(targetViewID);
            if (targetPV != null)
            {
                // 찾은 타겟으로 공통 Setup 함수 호출
                Setup(targetPV.transform, damage, speed, teamTag);
            }
            else if (photonView.IsMine)
            {
                // 타겟을 찾을 수 없으면(이미 파괴됨 등) 발사체를 즉시 파괴
                PhotonNetwork.Destroy(gameObject);
            }
        }

        // [신규] 오프라인/온라인 공통 초기화 로직을 처리하는 내부 함수
        private void Setup(Transform newTarget, int newDamage, float newSpeed, string newTeamTag)
        {
            this.target = newTarget;
            this.damage = newDamage;
            this.speed = newSpeed;
            this.spawnerTeamTag = newTeamTag;

            if (this.target != null)
            {
                // 타겟을 향해 즉시 속도 설정
                Vector2 direction = (this.target.position - transform.position).normalized;
                rb.velocity = direction * speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 충돌 로직은 발사체의 소유자만 처리하도록 하여 중복 데미지 방지
            if (PhotonNetwork.IsConnected && !photonView.IsMine)
            {
                return;
            }

            string enemyUnitTag = (spawnerTeamTag == "BaseP1" || spawnerTeamTag == "P1Turret") ? "P2" : "P1";
            string enemyBaseTag = (spawnerTeamTag == "BaseP1" || spawnerTeamTag == "P1Turret") ? "BaseP2" : "BaseP1";

            if (other.CompareTag(enemyUnitTag) || other.CompareTag(enemyBaseTag))
            {
                if (other.TryGetComponent(out DebugUnitController debugUnit))
                {
                    debugUnit.TakeDamage(damage);
                }
                else if (other.TryGetComponent(out UnitController networkUnit))
                {
                    networkUnit.photonView.RPC("RpcTakeDamage", RpcTarget.All, damage);
                }
                else if (other.TryGetComponent(out BaseController baseCtrl))
                {
                    baseCtrl.photonView.RPC("RpcTakeDamage", RpcTarget.All, damage, spawnerTeamTag);
                }
                DestroyProjectile();
            }
        }

        private void FixedUpdate()
        {
            if (target != null)
            {
                RotateTowardsTarget();
            }
        }

        private void RotateTowardsTarget()
        {
            if (target == null) return;
            Vector2 direction = target.position - transform.position;
            transform.right = direction;
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