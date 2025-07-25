using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임의 전반적인 상태와 핵심 로직을 관리하는 싱글턴 메니저 클래스
/// </summary>
public class InGameManager : MonoBehaviourPunCallbacks
{
    //디버그 옵션 제거시 전부 제거
    [Header("디버그 옵션")]
    [Tooltip("체크하면 네트워크 연결 없이 P1(호스트)로 간주하고 테스트")]
    public bool isDebugMode = false;
    [Tooltip("is_Debug_Mode가 켜져 있을 때만 동작. P1(호스트)로 테스트하려면 체크, P2(클라이언트)로 테스트하려면 체크 해제")]
    public bool isDebugHost = true; // P1 역할인지 P2 역할인지 선택
    //-----------------
    public static InGameManager Instance { get; private set; }

    [Header("관리대상")]

    public PHK.UnitPanelManager unitPanelManager;

    [Header("게임 기본 설정")]
    [SerializeField] private int startingGold = 175; //게임 시작시 보유 골드
    [SerializeField] private int maxBaseHealth = 1000; //기지의 최대 체력
    [Tooltip("시대 발전에 필요한 누적 경험치")]
    [SerializeField] private int[] expForEvolve;

    [Header("유닛 생성 관련")]
    [SerializeField] Transform p1_spawnPoint;
    [SerializeField] Transform p2_spawnPoint;
    //대기열
    private Queue<(GameObject prefab, string ownerTag)> productionQueue = new Queue<(GameObject prefab, string ownerTag)>();
    //현재 생산 라인이 가동 중인지
    private bool isProducing = false;


    public event Action<int> OnQueueChanged;
    public event Action<float> OnProductionProgress;
    public event Action<bool> OnProductionStatusChanged;
    public event Action<PHK.UnitPanelManager.Age> OnAgeEvolved;

    //현재 게임 상태 변수
    private int currentGold;
    private int currentEXP;
    private int currentBaseHealth;
    private PhotonView photonView;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            photonView = GetComponent<PhotonView>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        //게임 상태 초기화
        currentGold = startingGold;
        currentEXP = 0;
        currentBaseHealth = maxBaseHealth;

        //초기 게임 상태를 UI에 반영
        InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
        InGameUIManager.Instance.UpdateBaseHpUI(currentBaseHealth, maxBaseHealth);

        //게임 시작시 시대 발전 버튼은 비활성화 상태로 시작
        if (unitPanelManager != null && unitPanelManager.evolveButton != null)
        {
            unitPanelManager.evolveButton.interactable = false;
        }

        if (unitPanelManager != null && unitPanelManager.evolveButton != null)
        {
            unitPanelManager.evolveButton.interactable = false;
        }
        if (p1_spawnPoint == null || p2_spawnPoint == null)
        {
            Debug.LogError("P1 또는 P2 스폰 포인트가 Inspector에 할당되지 않았습니다!");
        }
    }

    private void Update()
    {
        if (isDebugMode)
        {

            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddGold(50); //테스트용으로 Space 키를 누르면 50 골드 추가
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                currentEXP += 500; //테스트용으로 100 경험치 추가
                InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
                CheckForAgeUp();
            }
        }
    }
    #region 자원 및 체력 관리 함수
    ///<summary>
    ///골드 획득 (현재는 유닛 처치 시)
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold += amount;
        InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
        Debug.Log($"{amount} 골드 획득. 현재 골드 : {currentGold}");
    }
    ///<summary>
    ///골드 소모(유닛 및 터렛 생성 시)
    /// </summary>
    public bool SpendGold(Unit unitToPurchase)
    {
        int amount = unitToPurchase.goldCost;
        if (currentGold >= amount)
        {
            currentGold -= amount;
            InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
            Debug.Log($"{amount}골드 소모. 현재 골드 : {currentGold}");

            return true;
        }
        else
        {
            Debug.Log("골드가 부족합니다.");
            //UIManager, InGameInfoText에서 알림 로직 추가
            InGameUIManager.Instance.inGameInfoText.text = $"Can't Spawn {unitToPurchase.unitName} !! Not Enough Gold!";
            InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
            return false;
        }
    }

    ///<summary>
    ///경험치 획득 (주로 적 유닛 처치)
    /// </summary>
    /// 

    public void AddExp(int amount)
    {
        //마지막 시대가 아닐 경우에만 경험치 획득하며, 시대 발전을 체크
        if (unitPanelManager.currentAge < PHK.UnitPanelManager.Age.Future) //현대전까지만 하면 Age.Modern으로 변경
        {
            currentEXP += amount;
            InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
            CheckForAgeUp();
            Debug.Log($"{amount} 경험치 획득.");
        }


    }
    ///<summary>
    ///기지 체력 감소(공격 받았을 시)
    /// </summary>

    public void TakeBaseDamage(int damage)
    {
        currentBaseHealth -= damage;
        currentBaseHealth = Mathf.Max(currentBaseHealth, 0); //체력이 0 미만으로 내려가지 않도록 보정
        InGameUIManager.Instance.UpdateBaseHpUI(currentBaseHealth, maxBaseHealth);

        if (currentBaseHealth <= 0) GameOver();
    }
    #endregion

    #region 시대 발전 관련
    ///<summary>
    ///시대 발전 시도(UnitPanel의 시대 발전 버튼 Onclick 이벤트)
    /// </summary>
    ///<summary>
    /// 시대 발전을 '시도'하는 함수. 실제 로직은 마스터 클라이언트에 요청
    /// (UnitPanel의 시대 발전 버튼 OnClick 이벤트에 연결될 함수)
    /// </summary>
    public void AttemptEvolve()
    {
        // 디버그 모드일 경우, 이전처럼 로컬에서 바로 처리
        if (isDebugMode)
        {
            EvolveLocally();
        }
        else
        {
            // 마스터 클라이언트에게 시대 발전을 요청하는 RPC를 보냄
            // 요청하는 플레이어의 ActorNumber를 함께 보냄
            photonView.RPC("RPC_RequestEvolve", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log("마스터 클라이언트에게 시대 발전을 요청합니다.");
        }
    }
    /// <summary>
    /// [MasterClient Only] 클라이언트로부터 시대 발전 요청을 받아 처리하는 RPC 함수
    /// </summary>
    [PunRPC]
    private void RPC_RequestEvolve(int requestingPlayerActorNumber)
    {
        // 이 함수는 마스터 클라이언트에서만 실행
        if (!PhotonNetwork.IsMasterClient) return;

        // 중요: 현재 구조에서는 각 클라이언트가 자신의 경험치(currentEXP)를 관리.
        // 이상적으로는 마스터 클라이언트가 모든 플레이어의 경험치를 관리해야 치팅을 막을 수 있다
        // 지금은 요청한 클라이언트가 조건을 만족했다고 '믿고' 진행하지만, 추후 개선이 필요

        // 여기에서 해당 플레이어가 진화 가능한지 조건을 검사.
        // 예: int requiredExp = expForEvolve[currentAgeIndex];
        // if (playerExp >= requiredExp) { ... }
        // 지금은 검증 로직이 없으므로 바로 확정 RPC를 호출.

        Debug.Log($"{requestingPlayerActorNumber}번 플레이어의 시대 발전 요청을 수신 및 검증 (현재는 자동 통과)");

        // 모든 클라이언트에게 특정 플레이어가 시대를 발전했음을 알림.
        // 발전한 플레이어의 ActorNumber와 새로운 시대의 인덱스를 보냄.
        int nextAgeIndex = (int)unitPanelManager.currentAge + 1; // 로컬 unitPanelManager를 기준으로 다음 시대를 계산
        photonView.RPC("RPC_ConfirmEvolve", RpcTarget.All, requestingPlayerActorNumber, nextAgeIndex);
    }
    /// <summary>
    /// [All Clients] 마스터 클라이언트로부터 시대 발전 확정을 받아 실제 게임에 적용하는 RPC 함수
    /// </summary>
    [PunRPC]
    private void RPC_ConfirmEvolve(int targetPlayerActorNumber, int newAgeIndex)
    {
        Debug.Log($"{targetPlayerActorNumber}번 플레이어가 {(PHK.UnitPanelManager.Age)newAgeIndex} 시대로 발전했음을 모두에게 적용합니다.");

        // 이 RPC를 수신한 클라이언트가 바로 시대 발전을 한 당사자인 경우에만 UI를 직접 조작.
        // 이렇게 해야 P1의 화면에서는 P1의 UI가, P2의 화면에서는 P2의 UI가 바뀜
        if (targetPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            EvolveLocally(newAgeIndex);
        }
        else
        {
            // 다른 플레이어가 시대 발전을 한 경우에 대한 처리 (예: 상대방 정보 UI 갱신 등)
            // 지금 당장은 특별히 처리할 내용이 없을 수도 있지만, 필요시 추가 가능
            Debug.Log($"다른 플레이어({targetPlayerActorNumber})의 시대 발전을 확인했습니다.");
        }
    }
    /// <summary>
    /// 실제 로컬 게임 로직을 실행하는 함수 (UI 변경 등)
    /// </summary>
    private void EvolveLocally(int? newAgeIndex = null)
    {
        PHK.UnitPanelManager.Age evolvedAge;
        // RPC를 통해 특정 시대로 바로 이동해야 하는 경우
        if (newAgeIndex.HasValue)
        {
            evolvedAge = (PHK.UnitPanelManager.Age)newAgeIndex.Value;
            unitPanelManager.SetAge((PHK.UnitPanelManager.Age)newAgeIndex.Value);
        }
        else // 기존의 로컬 로직 (다음 시대로 순차 발전)
        {
            int currentAgeIndex = (int)unitPanelManager.currentAge;
            if (currentAgeIndex < expForEvolve.Length && currentEXP >= expForEvolve[currentAgeIndex])
            {
                unitPanelManager.EvolveToNextAge();
            }
            unitPanelManager.EvolveToNextAge();
            evolvedAge = unitPanelManager.currentAge;
        }

        unitPanelManager.evolveButton.interactable = false;
        // 시대 발전이 확정된 후, 등록된 모든 리스너에게 이벤트를 방송합니다.
        OnAgeEvolved?.Invoke(evolvedAge);
        CheckForAgeUp(); // 다음 시대 발전 조건 만족 여부 재확인
    }

    ///<summary>
    ///경험치를 획득할 때마다 시대 발전이 가능한지 확인 후 버튼 활성화를 결정
    /// </summary>
    private void CheckForAgeUp()
    {
        int currentAgeIndex = (int)unitPanelManager.currentAge;

        //다음 시대와 발전 버튼ㅇ니 비활성화 상태일 때 체크
        if (currentAgeIndex < expForEvolve.Length && !unitPanelManager.evolveButton.interactable)
        {
            //현재 경험치가 필요 경험치 이상이면 발전 버튼 활성화
            if (currentEXP >= expForEvolve[currentAgeIndex])
            {
                unitPanelManager.evolveButton.interactable = true;
                InGameUIManager.Instance.inGameInfoText.text = "You Can Evolve Age!";
                InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
            }
        }
    }
    #endregion

    #region 유닛 생산 로직
    ///<summary>
    ///프리팹을 생산 큐에 추가하는 함수
    /// </summary>
    /// 
    public void RequestUnitProduction(GameObject unitPrefab, string ownerTag)
    {
        // 현재 유닛이 생산 중인지 아닌지에 따라 로직을 분리
        if (isProducing)
        {
            // --- 이미 다른 유닛이 생산 중일 경우: 대기열(Queue)에 추가 ---
            if (productionQueue.Count < 5) // 큐는 5칸
            {
                Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
                if (SpendGold(unitData))
                {
                    productionQueue.Enqueue((unitPrefab, ownerTag));
                    // 대기열 UI 업데이트 (현재 대기중인 유닛 수만 전달)
                    OnQueueChanged?.Invoke(productionQueue.Count);
                }
            }
            else
            {
                // 대기열이 꽉 찼을 때의 처리 (메시지 등)
            }
        }
        else
        {
            // --- 생산 라인이 비어있을 경우: 바로 생산 시작 ---
            Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
            if (SpendGold(unitData))
            {
                // 이 유닛은 큐를 거치지 않고 바로 생산 코루틴으로 전달
                StartCoroutine(ProcessSingleUnit(unitPrefab, ownerTag));
            }
        }
    }

    /// <summary>
    /// 유닛 '한 개'를 생산하고, 완료되면 대기열에서 다음 유닛을 가져와 다시 이 코루틴을 실행
    /// </summary>
    private IEnumerator ProcessSingleUnit(GameObject prefabToProduce, string ownerTag)
    {
        Vector3 initialMoveDirection = (ownerTag == "P1") ? Vector3.right : Vector3.left;
        // --- 생산 시작 처리 ---
        isProducing = true;
        OnProductionStatusChanged?.Invoke(true); // 슬라이더 활성화
        OnProductionProgress?.Invoke(0f);      // 슬라이더 0%에서 시작

        Unit unitData = prefabToProduce.GetComponent<UnitController>().unitdata;

        // --- 생산 시간 동안 대기 ---
        if (unitData.SpawnTime > 0)
        {
            float timer = 0f;
            while (timer < unitData.SpawnTime)
            {
                timer += Time.deltaTime;
                OnProductionProgress?.Invoke(Mathf.Clamp01(timer / unitData.SpawnTime));
                yield return null;
            }
        }
        OnProductionProgress?.Invoke(1f); // 100% 채우기

        Transform spawnPoint = (ownerTag == "P1") ? p1_spawnPoint : p2_spawnPoint;
        if (spawnPoint != null)
        {
            // 디버그 모드와 실제 네트워크 환경을 분기
            if (isDebugMode)
            {
                // --- 디버그 모드: 일반 Instantiate 사용 ---
                GameObject newUnit = Instantiate(prefabToProduce, spawnPoint.position, spawnPoint.rotation);
                newUnit.tag = ownerTag;
                if (ownerTag == "P1")
                {
                    newUnit.layer = LayerMask.NameToLayer("P1Unit");
                }
                else if (ownerTag == "P2")
                {
                    newUnit.layer = LayerMask.NameToLayer("P2Unit");
                }
                // UnitController의 public 변수에 직접 접근하여 방향을 설정
                UnitController controller = newUnit.GetComponent<UnitController>();
                if (controller != null)
                {
                    controller.moveDirection = initialMoveDirection;
                }

            }
            else
            {
                // --- 실제 네트워크 환경: PhotonNetwork.Instantiate 사용 ---
                object[] data = new object[] { ownerTag, initialMoveDirection };
                PhotonNetwork.Instantiate(prefabToProduce.name, spawnPoint.position, spawnPoint.rotation, 0, data);
            }
        }

        // --- 후속 처리: 대기열에 다음 유닛이 있는지 확인 ---
        if (productionQueue.Count > 0)
        {
            var nextUnit = productionQueue.Dequeue();
            OnQueueChanged?.Invoke(productionQueue.Count);
            StartCoroutine(ProcessSingleUnit(nextUnit.prefab, nextUnit.ownerTag));
        }
        else
        {
            isProducing = false;
            OnProductionStatusChanged?.Invoke(false); // 슬라이더 비활성화
            OnProductionProgress?.Invoke(0f);         // 슬라이더 0%로 리셋
        }
    }

    ///<summary>
    ///모든 클라이언트에서 특정 유닛의 태그를 설정함
    /// </summary>
    [PunRPC]
    private void RPC_SetUnitTag(int viewID, string tag)
    {
        PhotonView targetPV = PhotonView.Find(viewID);
        if (targetPV != null)
        {
            targetPV.gameObject.tag = tag;
            // *** START: 요청에 따른 레이어 설정 추가 ***
            if (tag == "P1")
            {
                targetPV.gameObject.layer = LayerMask.NameToLayer("P1Unit");
            }
            else if (tag == "P2")
            {
                targetPV.gameObject.layer = LayerMask.NameToLayer("P2Unit");
            }
        }
        else
        {
            Debug.LogWarning($"PhotonView with ID {viewID} not found for setting tag to {tag}.");
        }
    }
    #endregion



    ///<summary>
    ///게임 오버 처리
    /// </summary>
    private void GameOver()
    {
        Debug.Log("게임 오버 처리 추가 작업 필요");
        Time.timeScale = 0f;
    }
}
