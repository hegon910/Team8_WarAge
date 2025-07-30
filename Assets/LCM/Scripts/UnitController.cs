using KYG;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] public Unit unitdata;
    [SerializeField] private Rigidbody2D rb;

    [SerializeField] private SpriteRenderer spriteRenderer;

    public int currentHealth;
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
            if (currentTarget != null)
            {
                Debug.Log($"{currentTarget.name}");
            }
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
    //----------- 공격 ------------------
    private void FindTarget()
    {
        Debug.Log("FindTarget 호출");
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
                Debug.Log("유닛 설정");
                closestDistance = distance;
                newTarget = go.transform;
            }
        }

        if (newTarget == null)
        {
            GameObject enemyBaseGO = GameObject.FindGameObjectWithTag(opponentBaseTag);
            if (enemyBaseGO != null)
            {
                float distanceToBase = Vector3.Distance(transform.position, enemyBaseGO.transform.position);
                if (unitdata.unitType == UnitType.Melee)
                {
                    if (distanceToBase <= unitdata.MeleeRange)
                    {
                        newTarget = enemyBaseGO.transform;
                        closestDistance = distanceToBase;
                    }
                }
                else if (unitdata.unitType == UnitType.Ranged)
                {
                    if (distanceToBase <= unitdata.rangedrange)
                    {
                        newTarget = enemyBaseGO.transform;
                        closestDistance = distanceToBase;
                    }
                }
            }
        }
        
        SetTarget(newTarget);
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

        if (unitdata.unitType == UnitType.Melee)
        {
            if (distanceToTarget <= unitdata.MeleeRange)
            {
                rb.velocity = Vector2.zero;
                // 공격 쿨다운이 끝났을 때만 공격
                if (attackCooldownTimer <= 0)
                {
                    UnitController targetUnit = target.GetComponent<UnitController>();
                    BaseController targetBase = target.GetComponent<BaseController>();
                    if (targetUnit != null)
                    {
                        targetUnit.TakeDamage(unitdata.attackDamage);
                        Debug.Log($"{gameObject.name}이 {target.name}에게 {unitdata.attackDamage} 데미지를 주었습니다.");
                        Debug.Log($"남은 체력은 {currentHealth}");
                    }
                    else if (targetBase != null)
                    {
                        targetBase.TakeDamage(unitdata.attackDamage, this.tag);
                        Debug.Log($"{gameObject.name}이 베이스 {target.name}에게 {unitdata.attackDamage} 데미지를 주었습니다.");
                    }
                    attackCooldownTimer = 1f / unitdata.attackSpeed;
                }
            }
            else
            {
                Move();
            }
        }
        else if (unitdata.unitType == UnitType.Ranged)
        {
            rb.velocity = Vector2.zero;
            //가까이 왔을때 근접 공격
            if (distanceToTarget <= meleeSwitchRange && unitdata.attackDamage > 0)
            {
                if (attackCooldownTimer <= 0)
                {
                    UnitController targetUnit = target.GetComponent<UnitController>();
                    BaseController targetBase = target.GetComponent<BaseController>();
                    if (targetUnit != null)
                    {
                        targetUnit.TakeDamage(unitdata.attackDamage); // 목표에게 데미지 적용
                        Debug.Log($"{gameObject.name}이 {target.name}에게 {unitdata.attackDamage} 데미지를 주었습니다.");
                    }
                    else if (targetBase != null)
                    {
                        targetBase.TakeDamage(unitdata.attackDamage, this.tag);
                        Debug.Log($"{gameObject.name}이 베이스 {target.name}에게 {unitdata.attackDamage} 데미지를 주었습니다.");
                    }
                    attackCooldownTimer = 1f / unitdata.attackSpeed;
                }
            }
            else if (distanceToTarget <= unitdata.rangedrange)
            {
                // 원거리 공격 쿨다운 체크를 PhotonNetwork.Instantiate 전에 추가
                if (attackCooldownTimer <= 0) // 이 조건이 추가됩니다.
                {
                    if (photonView.IsMine)
                    {
                        string spawnerTag = gameObject.tag;
                        Vector3 ArrowSpawnPos = transform.position + (moveDirection.normalized * 0.5f);
                        string arrowPrefabName = unitdata.ArrowPrefab.name;
                        GameObject ArrowGo = PhotonNetwork.Instantiate(arrowPrefabName, ArrowSpawnPos, Quaternion.identity);
                        Arrow arrow = ArrowGo.GetComponent<Arrow>();

                        if (arrow != null)
                        {
                            // Instantiate를 호출한 클라이언트가 소유자가 되므로, 이 RPC는 RpcTarget.All로 보내도 됩니다.
                            arrow.photonView.RPC("InitializeArrow", RpcTarget.All, spawnerTag, moveDirection, unitdata.attackDamage, unitdata.rangedrange);
                        }

                        Debug.Log($"{gameObject.name}이 원거리 공격을 시작합니다. 발사 유닛 태그: {spawnerTag}");
                    }
                    attackCooldownTimer = 1f / unitdata.attackSpeed; // 화살 생성 후 쿨타임 설정
                }
            }
            else
            {
                Move();
            }
        }
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
                int playerActorNumber = (gameObject.CompareTag("P1") && gm.isDebugHost) || (gameObject.CompareTag("P2") && !gm.isDebugHost) ? PhotonNetwork.LocalPlayer.ActorNumber : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? 2 : 1);
                if (gm != null)
                {
                    gm.AddExp(playerActorNumber, unitdata.unitExp);
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
    public void RpcTakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth < 0)
        {
            photonView.RPC("RpcDie", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RpcDie(int ownerActorNumber) // ownerActorNumber 매개변수 추가
    {
        // 마스터 클라이언트만 경험치를 추가하도록 처리
        if (PhotonNetwork.IsMasterClient || InGameManager.Instance.isDebugMode) // 디버그 모드에서도 로컬에서 처리
        {
            if (gm != null)
            {
                gm.AddExp(ownerActorNumber, unitdata.unitExp); // 전달받은 ownerActorNumber에게 경험치 추가
            }
            Debug.Log($"유닛 경험치 추가 {unitdata.unitExp} (대상 플레이어 ActorNumber: {ownerActorNumber})");
        }
        else
        {
            Debug.Log($"슬레이브 클라이언트: 유닛 사망 확인. 마스터 클라이언트가 경험치 처리");
        }

        Destroy(gameObject, 2f);
    }

}
