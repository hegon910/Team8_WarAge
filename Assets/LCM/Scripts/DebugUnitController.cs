using KYG;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class DebugUnitController : MonoBehaviour
{
    // ���� �� ������ ����
    private UnitController originalController;
    private Unit unitdata;

    // ������Ʈ ����
    private Rigidbody2D rb;
    private Animator animator;

    // ���� ���� ����
    public int currentHealth;
    private Transform currentTarget;
    private float attackCooldownTimer;
    private Vector3 moveDirection;

    // UnitController�� ������ ���߱� ���� ����
    private LayerMask unitLayer; // �� �Ʊ� ������ ������ ���̾�
    private float stopDistance = 0.1f;

    // �ִϸ����� �Ķ���� �ؽ�
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsAttack = Animator.StringToHash("isAttack");
    private static readonly int IsDead = Animator.StringToHash("isDead");

    // �ʱ�ȭ
    void Start()
    {
        originalController = GetComponent<UnitController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        if (InGameManager.Instance == null)
        {
            this.enabled = false;
            return;
        }

        if (InGameManager.Instance.isDebugMode)
        {
            originalController.enabled = false;
            this.enabled = true;
            InitializeDebugUnit();
        }
        else
        {
            this.enabled = false;
        }
    }

    private void InitializeDebugUnit()
    {
        unitdata = originalController.unitdata;
        currentHealth = originalController.currentHealth = unitdata.health;
        moveDirection = gameObject.CompareTag("P1") ? Vector3.right : Vector3.left;

        // �ڡڡ� UnitController�� �����ϰ� '�Ʊ�' ���̾ �����մϴ�. �ڡڡ�
        if (gameObject.CompareTag("P1"))
        {
            unitLayer = LayerMask.GetMask("P1Unit");
        }
        else if (gameObject.CompareTag("P2"))
        {
            unitLayer = LayerMask.GetMask("P2Unit");
        }

        animator?.SetBool(IsMoving, true);
    }

    // �� ������ ������Ʈ
    void Update()
    {
        if (currentHealth <= 0) return;

        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // UnitController�� ������ ���� �帧
        if (currentTarget == null || !IsTargetInRange())
        {
            FindTarget();
            if (currentTarget == null)
            {
                Move(); // Ÿ�� ������ �̵�
            }
            else
            {
                // Ÿ���� �������� ���� ��Ÿ� ���̸� �̵�
                Move();
            }
        }
        else
        {
            // Ÿ���� �ְ� ��Ÿ� ���̸� ����
            Attack(currentTarget);
        }
    }

    // �̵� (UnitController�� Raycast ���� ����Ȯ���� ����)
    private void Move()
    {
        if (currentHealth <= 0) return;

        Collider2D myCollider = GetComponent<Collider2D>();
        Vector2 raycastOrigin = (Vector2)transform.position + new Vector2(myCollider.bounds.extents.x * moveDirection.x, 0);

        // �ڡڡ� Raycast�� '�Ʊ�' ���̾�(unitLayer)�� ������ ���� �ڡڡ�
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, moveDirection, stopDistance, unitLayer);

        // �տ� �Ʊ��� ������ ���� (��ħ ����)
        if (hit.collider != null && hit.collider.gameObject != this.gameObject)
        {
            rb.velocity = Vector2.zero;
            animator?.SetBool(IsMoving, false);
            return;
        }

        // �տ� �ƹ��� ������ ��� ����
        animator?.SetBool(IsMoving, true); // �ִϸ����Ͱ� ������ �ִϸ��̼Ǹ� ����
        rb.velocity = moveDirection * unitdata.moveSpeed; // �� �̵� �ڵ�� ���ǹ� �ۿ��� �׻� ����
    }
    // Ÿ�� Ž�� (UnitController�� �ܼ��� ������� ����)
    private void FindTarget()
    {
        string opponentUnitTag = CompareTag("P1") ? "P2" : "P1";
        string opponentBaseTag = CompareTag("P1") ? "BaseP2" : "BaseP1";
        Transform newTarget = null;
        float closestDistance = Mathf.Infinity;

        // 1����: ���� ����� �� ���� ã��
        GameObject[] enemyUnits = GameObject.FindGameObjectsWithTag(opponentUnitTag);
        foreach (GameObject go in enemyUnits)
        {
            var debugCtrl = go.GetComponent<DebugUnitController>();
            if (debugCtrl != null && debugCtrl.currentHealth > 0)
            {
                float distance = Vector3.Distance(transform.position, go.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    newTarget = go.transform;
                }
            }
        }

        // 2����: ������ ������ �� ���� ã��
        if (newTarget == null)
        {
            GameObject enemyBaseGO = GameObject.FindGameObjectWithTag(opponentBaseTag);
            if (enemyBaseGO != null)
            {
                newTarget = enemyBaseGO.transform;
            }
        }
        currentTarget = newTarget;
    }

    // ����
    private void Attack(Transform target)
    {
        rb.velocity = Vector2.zero;
        animator?.SetBool(IsMoving, false);

        if (attackCooldownTimer > 0) return;

        animator?.SetTrigger(IsAttack);

        if (unitdata.unitType == UnitType.Ranged)
        {
            Vector3 projectileDirection = (target.position - transform.position).normalized;
            Vector3 spawnPos = transform.position + (projectileDirection * 0.5f);
            GameObject arrowGo = Instantiate(unitdata.ArrowPrefab, spawnPos, Quaternion.identity);
            Arrow arrow = arrowGo.GetComponent<Arrow>();
            if (arrow != null)
            {
                arrow.InitializeArrow(gameObject.tag, projectileDirection, unitdata.attackDamage, unitdata.rangedrange);
            }
        }
        else if (unitdata.unitType == UnitType.Melee)
        {
            var targetBase = target.GetComponentInChildren<BaseController>();
            if (targetBase != null)
            {
                targetBase.TakeDamage(unitdata.attackDamage, this.tag);
            }
            else
            {
                var targetDebugController = target.GetComponent<DebugUnitController>();
                if (targetDebugController != null)
                {
                    targetDebugController.TakeDamage(unitdata.attackDamage);
                }
            }
        }
        attackCooldownTimer = 1f / unitdata.attackSpeed;
    }

    // ��Ÿ� üũ
    private bool IsTargetInRange()
    {
        if (currentTarget == null) return false;
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        float attackRange = unitdata.unitType == UnitType.Melee ? unitdata.MeleeRange : unitdata.rangedrange;
        return distanceToTarget <= attackRange;
    }

    // ������ ó��
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;
        currentHealth -= amount;
        originalController.currentHealth = this.currentHealth;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ���� ó��
    private void Die()
    {
        string giveExpTag = CompareTag("P1") ? "P2" : "P1";
        InGameManager.Instance.AddExp(giveExpTag, unitdata.unitExp);
        currentHealth = -1;
        originalController.currentHealth = -1;
        GetComponent<Collider2D>().enabled = false;
        rb.velocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool(IsDead, true);
        }
        Destroy(gameObject, 2f);
    }
}