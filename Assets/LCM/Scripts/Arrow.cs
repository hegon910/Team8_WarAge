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
    public void InitializeArrow(string spawnerTag, Vector3 moveDirection, int arrowDamage, float maxRange) // [수정] attackerActorNumber 매개변수 추가
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
        if (photonView.IsMine)
        {
            float distanceTraveled = Vector3.Distance(startPosition, transform.position);

            if (distanceTraveled >= maxRange)
            {
                Debug.Log($"{gameObject.name}이 최대 사거리 ({maxRange})에 도달하여 파괴됩니다.");
                PhotonNetwork.Destroy(gameObject);
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("충돌발생");
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {

        }
        else
        {
            if (!photonView.IsMine) return;
        }


        UnitController targetUnit = other.GetComponent<UnitController>();
        BaseController targetBase = other.GetComponent<BaseController>();
        if (other.CompareTag(ownerTag) == false)
        {
            if (targetUnit != null)
            {
                string opponentUnitTag = (ownerTag == "P1") ? "P2" : "P1";
                if (other.CompareTag(opponentUnitTag))
                {
                    targetUnit.photonView.RPC("RpcTakeDamage", RpcTarget.All, damage);
                    Debug.Log($"{gameObject.name} (발사자: {ownerTag})이 유닛 {other.name} (태그: {other.tag})에게 {damage} 데미지를 주었습니다.");
                }
            }
            else if (targetBase != null)
            {
                string opponentBaseTag = (ownerTag == "P1") ? "BaseP2" : "BaseP1";
                if (other.CompareTag(opponentBaseTag))
                {
                    // BaseController는 건드리지 않는다는 지침에 따라 attackerActorNumber를 전달하지 않습니다.
                    targetBase.TakeDamage(damage, ownerTag);
                    Debug.Log($"{gameObject.name} (발사자: {ownerTag})이 베이스 {other.name} (태그: {other.tag})에게 {damage} 데미지를 주었습니다.");
                }
            }
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}