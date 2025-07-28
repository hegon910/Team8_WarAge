using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] public Unit unitdata;
    [SerializeField] private Rigidbody2D rb;

    [SerializeField] private SpriteRenderer spriteRenderer;

    private int currentHealth;
    private Transform currentTarget;
    private float attackCooldownTimer;

    public float meleeSwitchRange = 1.5f;
    public LayerMask unitLayer;
    public float stopDistance = 1f;

    public Vector3 moveDirection = Vector3.right;

    public bool IsMine => photonView.IsMine;

    private InGameManager gm;

    private void Awake()
    {
        currentHealth = unitdata.health;
        gm = InGameManager.Instance;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }


        if (spriteRenderer == null) 
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        PhotonView photonView = GetComponent<PhotonView>();
        
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
            spriteRenderer.flipX = !spriteRenderer.flipX; // P2 유닛은 스프라이트를 뒤집음
        }

        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {

            if (gameObject.CompareTag("P1"))
            {
                unitLayer = LayerMask.GetMask("P1Unit");
            }
            else if (gameObject.CompareTag("P2"))
            {
                unitLayer = LayerMask.GetMask("P2Unit");
            }
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

    // ---------- 네트워크 구현 -------------
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

    //[PunRPC]
    //public void RpcSetPlayerProps(string playerTag, Vector3 initialMoveDirection)
    //{
    //    gameObject.tag = playerTag; 
    //    moveDirection = initialMoveDirection; 

    //    Debug.Log($"{gameObject.name}의 태그가 {playerTag}로 설정되고, 방향은 {moveDirection}입니다.");

    //    if (spriteRenderer != null)
    //    {
    //        if (playerTag == "P1")
    //        {
    //            spriteRenderer.flipX = true; // P1은 기본 방향 (오른쪽)
    //        }
    //        else if (playerTag == "P2")
    //        {
    //            spriteRenderer.flipX = false; // P2는 이미지 플립 (왼쪽)
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning($"{gameObject.name}에 SpriteRenderer가 없습니다. 이미지를 플립할 수 없습니다.");
    //    }

    //}

    // --------------------------------------------------------


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
        Debug.DrawRay(raycastOrigin, checkDirection * stopDistance, Color.red, 0.1f);

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
                    UnitController targetUnit = target.GetComponent<UnitController>();
                    if (targetUnit != null)
                    {
                        targetUnit.TakeDamage(unitdata.attackDamage); 
                        Debug.Log($"{gameObject.name}이 {target.name}에게 {unitdata.attackDamage} 데미지를 주었습니다.");
                        Debug.Log($"남은 체력은 {currentHealth}");
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
            //가까이 왔을때 근접 공격
            if(distanceToTarget <= meleeSwitchRange && unitdata.attackDamage > 0)
            {
                if(attackCooldownTimer <= 0)
                {
                    UnitController targetUnit = target.GetComponent<UnitController>();
                    if (targetUnit != null)
                    {
                        targetUnit.TakeDamage(unitdata.attackDamage); // 목표에게 데미지 적용
                        Debug.Log($"{gameObject.name}이 {target.name}에게 {unitdata.attackDamage} 데미지를 주었습니다.");
                    }
                    attackCooldownTimer = 1f / unitdata.attackSpeed;
                }
            }
            else if(distanceToTarget <= unitdata.rangedrange)
            {
                if(attackCooldownTimer <= 0)
                {
                    string spawnerTag = gameObject.tag;
                    Vector3 ArrowSpawnPos = transform.position + (moveDirection.normalized * 0.5f);

                    GameObject ArrowGo = PhotonNetwork.Instantiate("Arrow", ArrowSpawnPos, Quaternion.identity);
                    Debug.Log("화살생성됨");

                    Arrow arrow = ArrowGo.GetComponent<Arrow>();
                    if (arrow != null)
                    {
                        arrow.photonView.RPC("InitializeArrow", RpcTarget.All, spawnerTag, moveDirection, unitdata.attackDamage, unitdata.rangedrange);
                    }
                    Debug.Log($"{gameObject.name}이 원거리 공격을 시작합니다. 발사 유닛 태그: {spawnerTag}");
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
    public void TakeDamage(int amount)
    {
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            // 디버그 모드에서는 소유권 검사 없이 바로 데미지 적용
            currentHealth -= amount;
            Debug.Log($"[DebugMode] {gameObject.name}의 체력 감소: {amount}, 현재 체력: {currentHealth}");
        }
        else // 실제 네트워크 모드에서는 기존의 소유권 검사를 유지하거나 RPC 방식으로 전환
        {
            if (!IsMine) return; // 유닛의 소유자만 체력 감소 가능
            currentHealth -= amount;
            Debug.Log($"[NetworkMode] {gameObject.name}의 체력 감소: {amount}, 현재 체력: {currentHealth}");
        }

        if (currentHealth < 0)
        {
            if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
            {
                if (gm != null)
                {
                    gm.AddExp(unitdata.unitExp);
                }
                Destroy(gameObject, 1f);
                Debug.Log($"[DebugMode] {gameObject.name} 사망 처리");
            }
            else
            {
                // 네트워크 모드에서는 RPC 호출
                photonView.RPC("RpcDie", RpcTarget.All);
            }
        }
    }


    [PunRPC]
    private void RpcDie()
    {
        gm.AddExp(unitdata.unitExp);
        Debug.Log($"유닛 경험치 추가{unitdata.unitExp}");
        Destroy(gameObject, 2f);
    }

    

    
}
