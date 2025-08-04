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
    private Vector3 startPosition; // <-- ����ü �߻� ���� ��ġ ����
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
        // ����� ����� ���� IsMine üũ ���� �ı� ���� ����
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            if (Vector3.Distance(startPosition, transform.position) >= maxRange)
            {
                Destroy(gameObject);
            }
        }
        // ��Ʈ��ũ ����� ���� IsMine üũ
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
        // �ڱ� �ڽ��̳� �Ʊ��� �浹�ϴ� ���� ����
        if (other.CompareTag(ownerTag))
        {
            return;
        }

        UnitController targetUnit = other.GetComponent<UnitController>();
        BaseController targetBase = other.GetComponent<BaseController>();

        // ��ȿ�� Ÿ��(�� ���� �Ǵ� �� ���̽�)�� �ƴϸ� ����
        string opponentUnitTag = (ownerTag == "P1") ? "P2" : "P1";
        string opponentBaseTag = (ownerTag == "P1") ? "BaseP2" : "BaseP1";
        if (!((targetUnit != null && other.CompareTag(opponentUnitTag)) || (targetBase != null && other.CompareTag(opponentBaseTag))))
        {
            return;
        }

        DebugUnitController debugTargetUnit = other.GetComponent<DebugUnitController>();
        BaseController debugTargetBase = other.GetComponent<BaseController>();
        // --- ����� ��� ó�� ---
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            if (targetUnit != null)
            {
                // RpcTakeDamage�� public �Լ��̹Ƿ� ���� ȣ���մϴ�.
                debugTargetUnit.TakeDamage(damage);
            }
            else if (targetBase != null)
            {
                // RpcTakeDamage�� public �Լ��̹Ƿ� ���� ȣ���մϴ�.
                targetBase.TakeDamage(damage, ownerTag);
            }
            // �Ϲ� Destroy�� ȭ���� �ı��մϴ�.
            Destroy(gameObject);
        }
        // --- ��Ʈ��ũ ��� ó�� ---
        else
        {
            // �� ȭ���� �ƴϸ� �ߺ� ó���� �����ϱ� ���� �ƹ��͵� ���� �ʽ��ϴ�.
            if (!photonView.IsMine) return;

            if (targetUnit != null)
            {
                // RPC�� ���� ��Ʈ��ũ���� ��� Ŭ���̾�Ʈ���� �������� �����մϴ�.
                targetUnit.photonView.RPC("RpcTakeDamage", RpcTarget.All, damage);
            }
            else if (targetBase != null)
            {
                // ������ PhotonView�� ���� RPC�� ȣ���մϴ�.
                PhotonView basePV = targetBase.GetComponent<PhotonView>();
                if (basePV != null)
                {
                    basePV.RPC("RpcTakeDamage", RpcTarget.All, damage, ownerTag);
                }
            }
            // PhotonNetwork.Destroy�� ��Ʈ��ũ���� ȭ���� �ı��մϴ�.
            PhotonNetwork.Destroy(gameObject);
        }
    }
}