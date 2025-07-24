using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
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

    private void Awake()
    {
        currentHealth = unitdata.health;

        if (CompareTag("P1"))
        {
            moveDirection = Vector3.right;
        }
        else if (CompareTag("P2"))
        {
            moveDirection = Vector3.left;
        }

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


    //---------- 이동 -----------------
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

        //앞의 유닛이 있을때 멈추는 거리 표현
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


    //----------- 공격 ------------------

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
                // 공격 쿨다운이 끝났을 때만 공격
                if (attackCooldownTimer <= 0)
                {
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
            //가까이 왔을때 근접 공격
            if(distanceToTarget <= meleeSwitchRange && unitdata.attackDamage > 0)
            {
                if(attackCooldownTimer <= 0)
                {
                    attackCooldownTimer = 1f / unitdata.attackSpeed;
                }
            }
            else if(distanceToTarget <= unitdata.rangedrange)
            {
                if(attackCooldownTimer <= 0)
                {
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

    //--------- 체력 및 사망 --------------
    private void TakeDamage(int amount)
    {
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
