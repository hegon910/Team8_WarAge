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
        private int attackerActorNumber; // [추가] 공격자의 ActorNumber를 저장할 변수 (UnitController.TakeDamage 호출용)
        private PhotonView photonView;

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
        }
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

                if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
                {
                    // 디버그 모드에서는 바로 제거
                    Destroy(gameObject);
                }
                else if (photonView != null && photonView.IsMine)
                {
                    // 네트워크 모드에서는 소유자만 제거
                    PhotonNetwork.Destroy(gameObject);
                }
                return;
            }
            // 타겟 방향으로 이동
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.2f) // 타겟에 닿으면 명중
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            if (target == null) return;

            if (target.TryGetComponent(out BaseController baseCtrl))
            {
                // BaseController의 TakeDamage는 RPC가 아니므로 직접 호출하면 안됩니다.
                // BaseController가 스스로 RPC를 호출하도록 RpcTakeDamage를 사용해야 합니다.
                baseCtrl.RpcTakeDamage(damage, teamTag);
            }
            else if (target.TryGetComponent(out UnitController unitCtrl))
            {
                unitCtrl.RpcTakeDamage(damage);
            }

            // 기존의 안전한 파괴 로직은 그대로 둡니다.
            if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
            {
                Destroy(gameObject);
            }
            else if (photonView != null && photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
        }

    }
}
