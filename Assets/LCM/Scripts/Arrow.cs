using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        if (!photonView.IsMine) return;


        UnitController targetUnit = other.GetComponent<UnitController>();


        if (targetUnit != null)
        {
            if(other.CompareTag(ownerTag) == false)
            {
                targetUnit.TakeDamage(damage);
                Debug.Log($"{gameObject.name} (발사자: {ownerTag})이 {other.name} (태그: {other.tag})에게 {damage} 데미지를 주었습니다.");
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
