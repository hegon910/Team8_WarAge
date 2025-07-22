using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    [SerializeField] Unit unitdata;
    [SerializeField] private Rigidbody rb;

    private int currentHealth;
    private Transform currentTarget;
    private float attackCooldownTimer;

    public float meleeSwitchRange = 1.5f;

    public Vector3 moveDirection = Vector3.right;

    private void Awake()
    {
        currentHealth = unitdata.health;

        if(rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if(attackCooldownTimer > 0)
        {
            attackCooldownTimer = Time.deltaTime;
        }

        if (currentTarget == null || !CanAttack())
        {
            Move();
        }
        else
        {
            Attack(currentTarget);
        }
    }


    //---------- �̵� -----------------
    private void Move()
    {
        if (!CanMove()) return;

        rb.MovePosition(transform.position + moveDirection * unitdata.moveSpeed * Time.deltaTime);
    }

    private bool CanMove()
    {
        return currentHealth > 0;
    }

    //-----------------------------------


    //----------- ���� ------------------
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
                // ���� ��ٿ��� ������ ���� ����
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
            //������ ������ ���� ����
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

    //--------- ü�� �� ��� --------------
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
