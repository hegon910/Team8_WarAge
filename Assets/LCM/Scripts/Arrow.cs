// ========== Arrow.cs ��ü �ڵ� ==========
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

    // [HOTFIX] UnitController���� 5���� �Ķ���͸� �����Ƿ�, �� ���Ǵ� �ùٸ��ϴ�. (���� ���ʿ�)
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
        // ȭ���� �����ڸ� �浹 ������ ó��
        if (!photonView.IsMine) return;

        // �ڽ��� �±׿� �ٸ� ������Ʈ�� �浹�ߴ��� Ȯ��
        if (!other.CompareTag(ownerTag))
        {
            UnitController targetUnit = other.GetComponent<UnitController>();
            BaseController targetBase = other.GetComponent<BaseController>();

            // ��� ���ְ� �浹 ��
            if (targetUnit != null)
            {
                string opponentUnitTag = (ownerTag == "P1") ? "P2" : "P1";
                if (other.CompareTag(opponentUnitTag))
                {
                    // [HOTFIX] UnitController�� RpcTakeDamage�� (int, int)�� �����Ƿ� �� �ڵ�� �ùٸ��ϴ�. (���� ���ʿ�)
                    targetUnit.photonView.RPC("RpcTakeDamage", RpcTarget.All, damage, attackerActorNumber);
                }
            }
            // ��� ������ �浹 ��
            else if (targetBase != null)
            {
                string opponentBaseTag = (ownerTag == "P1") ? "BaseP2" : "BaseP1";
                if (other.CompareTag(opponentBaseTag))
                {
                    targetBase.RpcTakeDamage(damage, ownerTag);
                }
            }

            // ��ȿ�� ���(����, ���̽� ��)�� �浹�ߴٸ� ȭ���� �ı�
            if (targetUnit != null || targetBase != null)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}