using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] public Unit unitdata;
    [SerializeField] private Rigidbody2D rb;

    private int currentHealth;
    private Transform currentTarget;
    private float attackCooldownTimer;

    public float meleeSwitchRange = 1.5f;
    public LayerMask unitLayer;
    public float stopDistance = 1f;

    public Vector3 moveDirection = Vector3.right;

    public bool IsMine => photonView.IsMine;

    private void Awake()
    {
        currentHealth = unitdata.health;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        if(attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (currentTarget == null || !IsTargetInRange()) 
        {
            FindTarget();
            if (currentTarget == null || !IsTargetInRange())
            {
                Move();
            }
            else 
            {
                Attack(currentTarget);
            }
        }
        else 
        {
            Attack(currentTarget);
        }
    }

    // ---------- ��Ʈ��ũ ���� -------------
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentHealth);
            if (currentTarget != null)
            {
                PhotonView targetPV = currentTarget.GetComponent<PhotonView>();
                stream.SendNext(targetPV != null ? targetPV.ViewID : -1);
            }
            else
            {
                stream.SendNext(-1); 
            }
        }
        else
        {
            currentHealth = (int)stream.ReceiveNext();
            int targetViewID = (int)stream.ReceiveNext();

            if (targetViewID != -1)
            {
                PhotonView targetPV = PhotonNetwork.GetPhotonView(targetViewID);
                if (targetPV != null)
                {
                    currentTarget = targetPV.transform;
                }
                else
                {
                    currentTarget = null; 
                }
            }
            else
            {
                currentTarget = null;
            }
        }
    }

    [PunRPC]
    public void RpcSetPlayerProps(string playerTag, Vector3 initialMoveDirection)
    {
        gameObject.tag = playerTag; 
        moveDirection = initialMoveDirection; 

        Debug.Log($"{gameObject.name}�� �±װ� {playerTag}�� �����ǰ�, ������ {moveDirection}�Դϴ�.");
    }

    // --------------------------------------------------------


    //---------- �̵� -----------------
    private void Move()
    {
        if (!CanMove()) return;

        Vector2 checkDirection = moveDirection;

        Collider2D myCollider = GetComponent<Collider2D>();
        Vector2 raycastOrigin = (Vector2)transform.position;
        if (myCollider != null)
        {
            raycastOrigin += checkDirection * (myCollider.bounds.extents.x + 0.05f);
        }

        //���� ������ ������ ���ߴ� �Ÿ� ǥ��
        //Debug.DrawRay(raycastOrigin, checkDirection * stopDistance, Color.red, 0.1f);

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, checkDirection, stopDistance, unitLayer);

        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        rb.MovePosition(transform.position + moveDirection * unitdata.moveSpeed * Time.deltaTime);
    }

    private bool CanMove()
    {
        return currentHealth > 0;
    }

    //-----------------------------------


    //----------- ���� ------------------

    private void FindTarget()
    {
        string targetTag = CompareTag("P1") ? "P2" : "P1";

        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(targetTag);

        Transform closestTarget = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject go in potentialTargets)
        {
            float distance = Vector3.Distance(transform.position, go.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestTarget = go.transform;
            }
        }
        SetTarget(closestTarget);
    }
    private bool IsTargetInRange()
    {
        if (currentTarget == null) return false;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (unitdata.unitType == UnitType.Melee)
        {
            return distanceToTarget <= unitdata.MeleeRange;
        }
        else if (unitdata.unitType == UnitType.Ranged)
        {
            return distanceToTarget <= meleeSwitchRange || distanceToTarget <= unitdata.rangedrange;
        }
        return false;
    }
    private void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    private void Attack(Transform target)
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if(unitdata.unitType == UnitType.Melee)
        {
            if (distanceToTarget <= unitdata.MeleeRange)
            {
                rb.velocity = Vector2.zero;
                // ���� ��ٿ��� ������ ���� ����
                if (attackCooldownTimer <= 0)
                {
                    UnitController targetUnit = target.GetComponent<UnitController>();
                    if (targetUnit != null)
                    {
                        targetUnit.TakeDamage(unitdata.attackDamage); 
                        Debug.Log($"{gameObject.name}�� {target.name}���� {unitdata.attackDamage} �������� �־����ϴ�.");
                    }
                    attackCooldownTimer = 1f / unitdata.attackSpeed;
                }
            }
            else
            {
                Move();
            }
        }
        else if(unitdata.unitType == UnitType.Ranged)
        {
            rb.velocity = Vector2.zero;
            //������ ������ ���� ����
            if(distanceToTarget <= meleeSwitchRange && unitdata.attackDamage > 0)
            {
                if(attackCooldownTimer <= 0)
                {
                    UnitController targetUnit = target.GetComponent<UnitController>();
                    if (targetUnit != null)
                    {
                        targetUnit.TakeDamage(unitdata.attackDamage); // ��ǥ���� ������ ����
                        Debug.Log($"{gameObject.name}�� {target.name}���� {unitdata.attackDamage} �������� �־����ϴ�.");
                    }
                    attackCooldownTimer = 1f / unitdata.attackSpeed;
                }
            }
            else if(distanceToTarget <= unitdata.rangedrange)
            {
                if(attackCooldownTimer <= 0)
                {
                    //TODO: ���Ÿ� ���� ����
                    attackCooldownTimer = 1f/ unitdata.attackSpeed;
                }
            }
            else
            {
                Move();
            }
        }
    }
    private bool CanAttack()
    {
        return currentHealth > 0 && attackCooldownTimer <= 0 && currentTarget != null;
    }

    //-------------------------------------

    //--------- ü�� �� ��� --------------
    private void TakeDamage(int amount)
    {
        if (!IsMine) return;

        currentHealth -= amount;

        if(currentHealth < 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject, 2f);
    }

    

    
}
