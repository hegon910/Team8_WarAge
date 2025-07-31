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
    public void InitializeArrow(string spawnerTag, Vector3 moveDirection, int arrowDamage, float maxRange) // [����] attackerActorNumber �Ű����� �߰�
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
                Debug.Log($"{gameObject.name}�� �ִ� ��Ÿ� ({maxRange})�� �����Ͽ� �ı��˴ϴ�.");
                PhotonNetwork.Destroy(gameObject);
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("�浹�߻�");
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
                    Debug.Log($"{gameObject.name} (�߻���: {ownerTag})�� ���� {other.name} (�±�: {other.tag})���� {damage} �������� �־����ϴ�.");
                }
            }
            else if (targetBase != null)
            {
                string opponentBaseTag = (ownerTag == "P1") ? "BaseP2" : "BaseP1";
                if (other.CompareTag(opponentBaseTag))
                {
                    // BaseController�� �ǵ帮�� �ʴ´ٴ� ��ħ�� ���� attackerActorNumber�� �������� �ʽ��ϴ�.
                    targetBase.TakeDamage(damage, ownerTag);
                    Debug.Log($"{gameObject.name} (�߻���: {ownerTag})�� ���̽� {other.name} (�±�: {other.tag})���� {damage} �������� �־����ϴ�.");
                }
            }
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}