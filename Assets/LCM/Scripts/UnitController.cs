using KYG;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UnitController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] public Unit unitdata;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    public int currentHealth;
    private Transform currentTarget;
    private float attackCooldownTimer;

    public float meleeSwitchRange = 1.5f;
    public LayerMask unitLayer;
    public float stopDistance = 1f;

    public Vector3 moveDirection = Vector3.right;
    public bool IsMine => photonView.IsMine;
    private InGameManager gm;

    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsDead = Animator.StringToHash("isDead"); // isDead bool 파라미터
    private static readonly int IsAttack = Animator.StringToHash("isAttack"); // isAttack Trigger 파라미터

    private void Awake()
    {
        currentHealth = unitdata.health;
        gm = InGameManager.Instance;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 1)
        {
            this.gameObject.tag = (string)photonView.InstantiationData[0];
            this.moveDirection = (Vector3)photonView.InstantiationData[1];
        }
    }

    private void Start()
    {
        if (gameObject.tag == "P2")
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            if (gameObject.CompareTag("P1")) unitLayer = LayerMask.GetMask("P1Unit");
            else if (gameObject.CompareTag("P2")) unitLayer = LayerMask.GetMask("P2Unit");
        }

        if (animator != null)
        {
            animator.SetBool(IsMoving, true); 
            animator.SetBool(IsDead, false);   
        }
    }

    private void Update()
    {
        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (!photonView.IsMine) return; // 내가 소유한 유닛만 행동 로직 실행

        if (currentTarget == null || !IsTargetInRange())
        {
            FindTarget();
            if (currentTarget == null)
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

            if (animator != null)
            {
                stream.SendNext(animator.GetBool(IsMoving)); 
            }
        }
        else
        {
            currentHealth = (int)stream.ReceiveNext();
            int targetViewID = (int)stream.ReceiveNext();
            if (targetViewID != -1)
            {
                PhotonView targetPV = PhotonNetwork.GetPhotonView(targetViewID);
                currentTarget = (targetPV != null) ? targetPV.transform : null;
            }
            else
            {
                currentTarget = null;
            }

            if (animator != null)
            {
                if (stream.Count >= 3) 
                {
                    animator.SetBool(IsMoving, (bool)stream.ReceiveNext()); 
                }
            }
        }
    }

    private void Move()
    {
        if (currentHealth <= 0) return;
        if (animator != null) animator.SetBool(IsMoving, true);
        Vector2 checkDirection = moveDirection;
        Collider2D myCollider = GetComponent<Collider2D>();
        Vector2 raycastOrigin = (Vector2)transform.position + checkDirection * (myCollider.bounds.extents.x + 0.05f);

        Debug.DrawRay(raycastOrigin, checkDirection * stopDistance, Color.red, 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, checkDirection, stopDistance, unitLayer);

        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        rb.MovePosition(transform.position + moveDirection * unitdata.moveSpeed * Time.deltaTime);
    }

    private void FindTarget()
    {
        string opponentUnitTag = CompareTag("P1") ? "P2" : "P1";
        string opponentBaseTag = CompareTag("P1") ? "BaseP2" : "BaseP1";

        Transform newTarget = null;
        float closestDistance = Mathf.Infinity;

        GameObject[] enemyUnits = GameObject.FindGameObjectsWithTag(opponentUnitTag);
        foreach (GameObject go in enemyUnits)
        {
            float distance = Vector3.Distance(transform.position, go.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                newTarget = go.transform;
            }
        }

        if (newTarget == null)
        {
            GameObject enemyBaseGO = GameObject.FindGameObjectWithTag(opponentBaseTag);
            if (enemyBaseGO != null)
            {
                newTarget = enemyBaseGO.transform;
            }
        }
        SetTarget(newTarget);
    }

    private bool IsTargetInRange()
    {
        if (currentTarget == null) return false;
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        return unitdata.unitType == UnitType.Melee ? distanceToTarget <= unitdata.MeleeRange : distanceToTarget <= unitdata.rangedrange;
    }

    private void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    private void Attack(Transform target)
    {
        if (attackCooldownTimer > 0) return;
        rb.velocity = Vector2.zero;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        photonView.RPC("RpcPlayAttackAnimation", RpcTarget.All);

        // 근접 공격
        if (unitdata.unitType == UnitType.Melee && distanceToTarget <= unitdata.MeleeRange)
        {
            var targetUnit = target.GetComponent<UnitController>();
            var targetBase = target.GetComponent<BaseController>();

            if (targetUnit != null)
                targetUnit.photonView.RPC("RpcTakeDamage", RpcTarget.All, unitdata.attackDamage);
            else if (targetBase != null)
            {
                // [HOTFIX] 베이스 공격을 RPC로 변경
                PhotonView basePV = targetBase.GetComponent<PhotonView>();
                if (basePV != null)
                {
                    basePV.RPC("RpcTakeDamage", RpcTarget.All, unitdata.attackDamage,this.tag);
                }
            }
            attackCooldownTimer = 1f / unitdata.attackSpeed;
        }
        // 원거리 공격
        else if (unitdata.unitType == UnitType.Ranged && distanceToTarget <= unitdata.rangedrange)
        {
            // 근접으로 붙었을 때의 처리
            if (distanceToTarget <= meleeSwitchRange && unitdata.attackDamage > 0)
            {
                var targetUnit = target.GetComponent<UnitController>();
                var targetBase = target.GetComponent<BaseController>();

                if (targetUnit != null)
                    targetUnit.photonView.RPC("RpcTakeDamage", RpcTarget.All, unitdata.attackDamage);
                else if (targetBase != null)
                {
                    // [HOTFIX] 베이스 공격을 RPC로 변경
                    PhotonView basePV = targetBase.GetComponent<PhotonView>();
                    if (basePV != null)
                    {
                        basePV.RPC("RpcTakeDamage", RpcTarget.All, unitdata.attackDamage,this.tag);
                    }
                }
            }
            // 원거리 공격
            else
            {
                string spawnerTag = gameObject.tag;
                Vector3 ArrowSpawnPos = transform.position + (moveDirection.normalized * 0.5f);
                GameObject ArrowGo = PhotonNetwork.Instantiate(unitdata.ArrowPrefab.name, ArrowSpawnPos, Quaternion.identity);
                Arrow arrow = ArrowGo.GetComponent<Arrow>();

                if (arrow != null)
                {
                    arrow.photonView.RPC("InitializeArrow", RpcTarget.All, spawnerTag, moveDirection, unitdata.attackDamage, unitdata.rangedrange);
                }
            }
            attackCooldownTimer = 1f / unitdata.attackSpeed;
        }
        else
        {
            Move();
        }
    }

    [PunRPC]
    public void RpcTakeDamage(int amount)
    {
        if (currentHealth <= 0) return; // 이미 죽었으면 중복 실행 방지

        currentHealth -= amount;
        if (currentHealth <= 0 && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RpcDie", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RpcPlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(IsAttack);
        }
    }

    [PunRPC]
    private void RpcDie()
    {
        string GiveExpTag = CompareTag("P1") ? "P2" : "P1";
        if (currentHealth > 0 && currentHealth != -1) // 아직 살아있으면 return (중복 실행 방지용)
        {
            // 이 조건은 RpcTakeDamage에서 이미 처리하므로 사실상 불필요하지만 안전장치로 둡니다.
        }

        currentHealth = -1; // 사망 상태로 확실히 변경 (중복 실행 방지)
        GetComponent<Collider2D>().enabled = false;
        rb.velocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool(IsDead, true); 
        }

        

        if (PhotonNetwork.IsMasterClient && gm != null)
        {
            gm.AddExp(GiveExpTag, unitdata.unitExp);
            Debug.Log($"유닛 사망. 경험치 {unitdata.unitExp}를 팀 {GiveExpTag}에게 지급");
        }

        Destroy(gameObject, 2f);
    }
}