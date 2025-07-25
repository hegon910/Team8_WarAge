using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 게임의 전반적인 상태와 핵심 로직을 관리하는 싱글턴 메니저 클래스
/// </summary>
public class InGameManager : MonoBehaviourPunCallbacks
{
    #region 변수
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
    public KYG.BaseController p1_Base;
    public KYG.BaseController p2_Base;

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
    public enum PlayerActionState
    {
        None,
        PlacingTurret,
        SellingTurret
    }
    [Header("플레이어 행동 상태")]
    public PlayerActionState currentState = PlayerActionState.None;
    private GameObject turretPrefabToPlace;
    #endregion

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

        InGameUIManager.Instance.UnitInfoText.text = "Game Started!"; // 게임 시작 메시지 표시
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
        HandleTurretActions();
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
    public bool SpendGold(int amount)
    {
        
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
            InGameUIManager.Instance.inGameInfoText.text = $"Can't Spawn!! Not Enough Gold!";
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

    public bool CanAfford(int amount)
    {
        return currentGold >= amount;
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
    /// <summary>
    /// 현재 보유한 골드로 특정 비용을 감당할 수 있는지 확인합니다.
    /// </summary>

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
            unitPanelManager.SetAge(evolvedAge); // SetAge로 직접 설정
        }
        else // 기존의 로컬 로직 (다음 시대로 순차 발전)
        {
            // [수정] EvolveToNextAge()를 한번만 호출하도록 수정
            int currentAgeIndex = (int)unitPanelManager.currentAge;
            if (currentAgeIndex < expForEvolve.Length && currentEXP >= expForEvolve[currentAgeIndex])
            {
                unitPanelManager.EvolveToNextAge(); // 여기서만 호출
            }
            else
            {
                // 진화 조건이 안되면 아무것도 하지 않음 (또는 알림 메시지)
                return; // 함수를 그냥 종료
            }
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


        InGameUIManager.Instance.UnitInfoText.text = $"Producing {unitPrefab.name}..."; 
        // 현재 유닛이 생산 중인지 아닌지에 따라 로직을 분리
        if (isProducing)
        {
            // --- 이미 다른 유닛이 생산 중일 경우: 대기열(Queue)에 추가 ---
            if (productionQueue.Count < 5) // 큐는 5칸
            {

                InGameUIManager.Instance.UnitInfoText.text = $"{unitPrefab.name} is Ready to Spawn...";
                Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
                if (SpendGold(unitData.goldCost))
                {
                    productionQueue.Enqueue((unitPrefab, ownerTag));
                    // 대기열 UI 업데이트 (현재 대기중인 유닛 수만 전달)
                    OnQueueChanged?.Invoke(productionQueue.Count);
                }
            }
            else
            {
                InGameUIManager.Instance.inGameInfoText.text = "Production Queue is Full!"; // 대기열이 가득 찼을 때 알림
            }
        }
        else
        {
            // --- 생산 라인이 비어있을 경우: 바로 생산 시작 ---
            Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
            if (SpendGold(unitData.goldCost))
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
                int percent = (int)((timer / unitData.SpawnTime) * 100f);
                InGameUIManager.Instance.UnitInfoText.text = $" Spawning : {prefabToProduce.name}. . . .{percent}%";
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

        InGameUIManager.Instance.UnitInfoText.text = $"{prefabToProduce.name} has Spawned!!";
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

    #region /// 터렛 관리 로직 (연동할 준비만, 언제든 변경 및 제거 가능) ///
    /// <summary>
    /// [UI 버튼 연결용] 터렛 건설 모드로 진입.
    /// </summary>
    /// <param name="turretPrefab">건설할 터렛의 프리팹. 비용 정보를 얻기 위해 사용.</param>
    public void EnterTurretPlaceMode(GameObject turretPrefab)
    {
        currentState = PlayerActionState.PlacingTurret;
        turretPrefabToPlace = turretPrefab;
        Debug.Log($"<color=cyan>건설 모드 시작:</color> {turretPrefab.name}. 건설할 위치를 클릭하세요.");
        InGameUIManager.Instance.inGameInfoText.text = "Click on a turret slot to build.";
        InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
    }

    /// <summary>
    /// [UI 버튼 연결용] 터렛 판매 모드로 진입. (이 함수는 변경 없음)
    /// </summary>
    public void EnterTurretSellMode()
    {
        currentState = PlayerActionState.SellingTurret;
        Debug.Log("<color=yellow>판매 모드 시작:</color> 판매할 터렛을 클릭하세요.");
        InGameUIManager.Instance.inGameInfoText.text = "Select a turret to sell.";
        InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
    }

    /// <summary>
    /// 모든 행동(건설/판매)을 취소하고 기본 상태로. (이 함수는 변경 없음)
    /// </summary>
    public void CancelPlayerAction()
    {
        currentState = PlayerActionState.None;
        turretPrefabToPlace = null;
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.HideInfoText();
        }
        Debug.Log("행동이 취소되었습니다.");
    }


    /// <summary>
    /// Update 함수에서 호출되어 터렛 관련 입력을 처리.
    /// </summary>
    private void HandleTurretActions()
    {
        if (currentState == PlayerActionState.None) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlayerAction();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (currentState == PlayerActionState.PlacingTurret)
            {
                TryPlaceTurretInSlot();
            }
            else if (currentState == PlayerActionState.SellingTurret)
            {
                TrySellTurretFromBase();
            }
        }
    }

    /// <summary>
    /// [UI 버튼 연결용] 베이스에 터렛을 건설할 수 있는 새로운 공간(슬롯)을 추가하도록 요청합니다.
    /// </summary>
    public void OnClick_RequestAddTurretSlot()
    {
        // 여기서 '터렛 슬롯'을 구매하는 비용을 정의합니다.
        int slotCost = 100; // 예시 비용

        Debug.Log($"<color=green>터렛 설치 공간 추가 시도.</color> 필요 골드: {slotCost}");

        // 1. InGameManager가 비용을 처리합니다.
        if (SpendGold(slotCost))
        {
            Debug.Log("비용 지불 성공. 베이스 컨트롤러에 슬롯 생성을 요청합니다.");
            // 2. 비용 지불에 성공하면, '베이스 담당자'가 만들 스크립트에 슬롯 생성을 요청합니다.
            // 이 로직은 현재 플레이어의 베이스를 찾아 해당 베이스의 컨트롤러 스크립트를 호출해야 합니다.

            /* --- 아래는 베이스 담당자가 참고할 수도코드 예시입니다 --- */
            //
            // BaseController playerBase = GetPlayerBase(); // 현재 플레이어의 베이스를 찾는 함수 (구현 필요)
            // if (playerBase != null)
            // {
            //     // 베이스 컨트롤러에 있는 'CreateNewTurretSlot' 함수를 호출합니다.
            //     // 이 함수는 베이스 위에 새로운 터렛 설치 공간을 시각적으로 생성하고 활성화하는 역할을 합니다.
            //     playerBase.CreateNewTurretSlot(); 
            // }
            // else
            // {
            //     Debug.LogError("현재 플레이어의 베이스를 찾을 수 없습니다!");
            // }
            /* --- 수도코드 예시 끝 --- */

        }
        else
        {
            Debug.Log("골드가 부족하여 터렛 설치 공간을 추가할 수 없습니다.");
        }
    }


    // 'TryPlaceTurretOnBase' 함수의 이름을 'TryPlaceTurretInSlot'으로 변경하고 내용을 수정합니다.
    // 이제 베이스가 아닌, 베이스 위의 '슬롯'을 클릭했을 때 반응합니다.
    /// <summary>
    /// 터렛을 '슬롯' 위에 건설하는 것을 시도합니다.
    /// </summary>
    private void TryPlaceTurretInSlot()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 1. 클릭한 곳이 '터렛 슬롯'인지 확인합니다. (담당자가 TurretSlot 스크립트 또는 태그를 만들어야 함)
            // var slot = hit.collider.GetComponent<TurretSlot>();
            // if (slot == null || slot.isOccupied) 
            // {
            //     Debug.Log("터렛을 지을 수 없는 공간이거나 이미 다른 터렛이 있습니다.");
            //     return; 
            // }

            // 2. 해당 슬롯이 현재 플레이어의 것인지 확인 (베이스 담당자가 구현)
            // if (!slot.IsOwnedByLocalPlayer()) return;

            // --- 여기서부터가 InGameManager의 핵심 역할 ---
            // 3. 비용 가져오기 (터렛 프리팹에 TurretController 같은 스크립트가 있고 비용 정보가 있다고 가정)
            // int turretCost = turretPrefabToPlace.GetComponent<TurretController>().goldCost;

            // 4. 골드 차감 시도
            // if (SpendGold(turretCost))
            // {
            //    // 5. 골드 차감에 성공하면, 해당 슬롯에 터렛을 설치하라고 '요청'
            //    Debug.Log($"골드 차감 성공. {slot.name}에 터렛 설치를 요청합니다.");
            //    // slot.PlaceTurret(turretPrefabToPlace); // 실제 설치는 슬롯이 담당
            // }

            // 6. 작업이 성공했든 실패했든, 건설 모드는 종료
            Debug.Log("터렛 슬롯 담당자가 만들 PlaceTurret 함수를 호출할 준비가 되었습니다.");
            CancelPlayerAction();
        }
    }

    /// <summary>
    /// '베이스' 위의 터렛을 판매하는 것을 시도.
    /// </summary>
    private void TrySellTurretFromBase()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 터렛 담당자가 만들 스크립트 (예: TurretController)를 가져옴.
            // var turretController = hit.collider.GetComponent<TurretController>();
            // if (turretController == null) return; // 터렛이 아니면 무시

            // 이 클라이언트가 해당 터렛의 주인인지 확인하는 로직 (담당자가 구현)
            // if (!turretController.IsOwnedByLocalPlayer())
            // {
            //     Debug.Log("자신의 터렛만 판매할 수 있습니다.");
            //     return;
            // }

            // --- 여기부터가 InGameManager의 핵심 역할 ---
            // 1. 판매 가격 가져오기
            // int refund = turretController.sellPrice;

            // 2. 골드 반환
            // AddGold(refund);

            // 3. 베이스 컨트롤러에게 터렛이 제거되었음을 알리고, 터렛 파괴 '요청'
            // turretController.GetOwnerBase().RemoveTurret(turretController.gameObject);

            // 4. 작업 완료 후 판매 모드 종료
            Debug.Log("터렛 판매 및 골드 환불 함수를 호출할 준비가 되었습니다.");
            CancelPlayerAction();
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
