using KYG;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class DebugUnitController : MonoBehaviour
{
    // 원본 및 데이터 참조
    private UnitController originalController;
    private Unit unitdata;

    // 컴포넌트 참조
    private Rigidbody2D rb;
    private Animator animator;

    // 유닛 상태 변수
    public int currentHealth;
    private Transform currentTarget;
    private float attackCooldownTimer;
    private Vector3 moveDirection;

    // UnitController와 로직을 맞추기 위한 변수
    private LayerMask unitLayer; // ★ 아군 유닛을 감지할 레이어
    private float stopDistance = 0.1f;

    // 애니메이터 파라미터 해시
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsAttack = Animator.StringToHash("isAttack");
    private static readonly int IsDead = Animator.StringToHash("isDead");

    // 초기화
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

        // ★★★ UnitController와 동일하게 '아군' 레이어를 설정합니다. ★★★
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

    // 매 프레임 업데이트
    void Update()
    {
        if (currentHealth <= 0) return;

        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // UnitController와 동일한 로직 흐름
        if (currentTarget == null || !IsTargetInRange())
        {
            FindTarget();
            if (currentTarget == null)
            {
                Move(); // 타겟 없으면 이동
            }
            else
            {
                // 타겟이 생겼지만 아직 사거리 밖이면 이동
                Move();
            }
        }
        else
        {
            // 타겟이 있고 사거리 안이면 공격
            Attack(currentTarget);
        }
    }

    // 이동 (UnitController의 Raycast 로직 ★정확히★ 복제)
    private void Move()
    {
        if (currentHealth <= 0) return;

        Collider2D myCollider = GetComponent<Collider2D>();
        Vector2 raycastOrigin = (Vector2)transform.position + new Vector2(myCollider.bounds.extents.x * moveDirection.x, 0);

        // ★★★ Raycast가 '아군' 레이어(unitLayer)를 보도록 수정 ★★★
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, moveDirection, stopDistance, unitLayer);

        // 앞에 아군이 있으면 멈춤 (겹침 방지)
        if (hit.collider != null && hit.collider.gameObject != this.gameObject)
        {
            rb.velocity = Vector2.zero;
            animator?.SetBool(IsMoving, false);
            return;
        }

        // 앞에 아무도 없으면 계속 전진
        animator?.SetBool(IsMoving, true); // 애니메이터가 있으면 애니메이션만 실행
        rb.velocity = moveDirection * unitdata.moveSpeed; // ★ 이동 코드는 조건문 밖에서 항상 실행
    }
    // 타겟 탐색 (UnitController의 단순한 방식으로 통일)
    private void FindTarget()
    {
        string opponentUnitTag = CompareTag("P1") ? "P2" : "P1";
        string opponentBaseTag = CompareTag("P1") ? "BaseP2" : "BaseP1";
        Transform newTarget = null;
        float closestDistance = Mathf.Infinity;

        // 1순위: 가장 가까운 적 유닛 찾기
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

        // 2순위: 유닛이 없으면 적 기지 찾기
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

    // 공격
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

    // 사거리 체크
    private bool IsTargetInRange()
    {
        if (currentTarget == null) return false;
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        float attackRange = unitdata.unitType == UnitType.Melee ? unitdata.MeleeRange : unitdata.rangedrange;
        return distanceToTarget <= attackRange;
    }

    // 데미지 처리
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

    // 죽음 처리
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