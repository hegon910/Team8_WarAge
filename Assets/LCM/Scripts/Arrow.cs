using KYG;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Arrow : MonoBehaviourPun
{
    public float speed = 10f;
    public int damage = 10;
    public string ownerTag;

    private Rigidbody2D rb;
    private Vector3 startPosition; // <-- 투사체 발사 시작 위치 저장
    private float maxRange;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    [PunRPC]
    public void InitializeArrow(string spawnerTag, Vector3 moveDirection, int arrowDamage, float maxRange) 
    {
        ownerTag = spawnerTag;
        damage = arrowDamage;
        this.maxRange = maxRange;
        startPosition = transform.position;

        rb.velocity = moveDirection.normalized * speed;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (moveDirection.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                spriteRenderer.flipX = false;
            }
        }
    }

    void Update()
    {
        // 디버그 모드일 때는 IsMine 체크 없이 파괴 로직 실행
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            if (Vector3.Distance(startPosition, transform.position) >= maxRange)
            {
                Destroy(gameObject);
            }
        }
        // 네트워크 모드일 때는 IsMine 체크
        else
        {
            if (photonView.IsMine)
            {
                if (Vector3.Distance(startPosition, transform.position) >= maxRange)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 자기 자신이나 아군과 충돌하는 것을 방지
        if (other.CompareTag(ownerTag))
        {
            return;
        }

        UnitController targetUnit = other.GetComponent<UnitController>();
        BaseController targetBase = other.GetComponent<BaseController>();

        // 유효한 타겟(적 유닛 또는 적 베이스)이 아니면 무시
        string opponentUnitTag = (ownerTag == "P1") ? "P2" : "P1";
        string opponentBaseTag = (ownerTag == "P1") ? "BaseP2" : "BaseP1";
        if (!((targetUnit != null && other.CompareTag(opponentUnitTag)) || (targetBase != null && other.CompareTag(opponentBaseTag))))
        {
            return;
        }

        DebugUnitController debugTargetUnit = other.GetComponent<DebugUnitController>();
        BaseController debugTargetBase = other.GetComponent<BaseController>();
        // --- 디버그 모드 처리 ---
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            if (targetUnit != null)
            {
                // RpcTakeDamage는 public 함수이므로 직접 호출합니다.
                debugTargetUnit.TakeDamage(damage);
            }
            else if (targetBase != null)
            {
                // RpcTakeDamage는 public 함수이므로 직접 호출합니다.
                targetBase.TakeDamage(damage, ownerTag);
            }
            // 일반 Destroy로 화살을 파괴합니다.
            Destroy(gameObject);
        }
        // --- 네트워크 모드 처리 ---
        else
        {
            // 내 화살이 아니면 중복 처리를 방지하기 위해 아무것도 하지 않습니다.
            if (!photonView.IsMine) return;

            if (targetUnit != null)
            {
                // RPC를 통해 네트워크상의 모든 클라이언트에게 데미지를 전달합니다.
                targetUnit.photonView.RPC("RpcTakeDamage", RpcTarget.All, damage);
            }
            else if (targetBase != null)
            {
                // 기지의 PhotonView를 통해 RPC를 호출합니다.
                PhotonView basePV = targetBase.GetComponent<PhotonView>();
                if (basePV != null)
                {
                    basePV.RPC("RpcTakeDamage", RpcTarget.All, damage, ownerTag);
                }
            }
            // PhotonNetwork.Destroy로 네트워크상의 화살을 파괴합니다.
            PhotonNetwork.Destroy(gameObject);
        }
    }
}