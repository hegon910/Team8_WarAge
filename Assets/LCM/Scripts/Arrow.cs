// ========== Arrow.cs 전체 코드 ==========
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
    private int attackerActorNumber;

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private float maxRange;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // [HOTFIX] UnitController에서 5개의 파라미터를 보내므로, 이 정의는 올바릅니다. (수정 불필요)
    [PunRPC]
    public void InitializeArrow(string spawnerTag, Vector3 moveDirection, int arrowDamage, float range, int _attackerActorNumber)
    {
        ownerTag = spawnerTag;
        damage = arrowDamage;
        maxRange = range;
        startPosition = transform.position;
        attackerActorNumber = _attackerActorNumber;

        rb.velocity = moveDirection.normalized * speed;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = moveDirection.x < 0;
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            if (Vector3.Distance(startPosition, transform.position) >= maxRange)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 화살의 소유자만 충돌 판정을 처리
        if (!photonView.IsMine) return;

        // 자신의 태그와 다른 오브젝트와 충돌했는지 확인
        if (!other.CompareTag(ownerTag))
        {
            UnitController targetUnit = other.GetComponent<UnitController>();
            BaseController targetBase = other.GetComponent<BaseController>();

            // 상대 유닛과 충돌 시
            if (targetUnit != null)
            {
                string opponentUnitTag = (ownerTag == "P1") ? "P2" : "P1";
                if (other.CompareTag(opponentUnitTag))
                {
                    // [HOTFIX] UnitController의 RpcTakeDamage가 (int, int)를 받으므로 이 코드는 올바릅니다. (수정 불필요)
                    targetUnit.photonView.RPC("RpcTakeDamage", RpcTarget.All, damage, attackerActorNumber);
                }
            }
            // 상대 기지와 충돌 시
            else if (targetBase != null)
            {
                string opponentBaseTag = (ownerTag == "P1") ? "BaseP2" : "BaseP1";
                if (other.CompareTag(opponentBaseTag))
                {
                    targetBase.RpcTakeDamage(damage, ownerTag);
                }
            }

            // 유효한 대상(유닛, 베이스 등)과 충돌했다면 화살을 파괴
            if (targetUnit != null || targetBase != null)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}