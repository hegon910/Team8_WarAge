using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 게임의 전반적인 상태와 핵심 로직을 관리하는 싱글턴 메니저 클래스
/// </summary>
public class InGameManager : MonoBehaviour
{
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

    //현재 게임 상태 변수
    private int currentGold;
    private int currentEXP;
    private int currentBaseHealth;


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
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
        if(unitPanelManager != null && unitPanelManager.evolveButton !=null)
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
        if(currentGold>=amount)
        {
            currentGold -= amount;
            InGameUIManager.Instance.UpdateResourceUI(currentGold,currentEXP);
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
        if(unitPanelManager.currentAge < PHK.UnitPanelManager.Age.Future) //현대전까지만 하면 Age.Modern으로 변경
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
    public void AttemptEvolve()
    {
        int currentAgeIndex = (int)unitPanelManager.currentAge;
        //발전 가능한 다음 시대, 필요 경험치 충족을 확인
        if(currentAgeIndex < expForEvolve.Length && currentEXP >= expForEvolve[currentAgeIndex])
        {
            //UnitPanelManager의 시대 발전 함수를 호출하여 UI를 다음 시대 패널로 교체
            unitPanelManager.EvolveToNextAge();
            //시대 발전 후 버튼 비활성화
            unitPanelManager.evolveButton.interactable = false;

            //혹시 다음 시대 발전 조건 만족했을 경우를 대비해 다시 한 번 체크
            CheckForAgeUp();
        }
    }

    ///<summary>
    ///경험치를 획득할 때마다 시대 발전이 가능한지 확인 후 버튼 활성화를 결정
    /// </summary>
    private void CheckForAgeUp()
    { 
        int currentAgeIndex = (int )unitPanelManager.currentAge;

        //다음 시대와 발전 버튼ㅇ니 비활성화 상태일 때 체크
        if(currentAgeIndex < expForEvolve.Length && !unitPanelManager.evolveButton.interactable)
        {
            //현재 경험치가 필요 경험치 이상이면 발전 버튼 활성화
            if(currentEXP >= expForEvolve[currentAgeIndex])
            {
                unitPanelManager.evolveButton.interactable=true;
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

        // --- 유닛 생성 ---
        Transform spawnPoint = (ownerTag == "P1") ? p1_spawnPoint : p2_spawnPoint;
        if (spawnPoint != null)
        {
            Instantiate(prefabToProduce, spawnPoint.position, spawnPoint.rotation);
        }

        // --- 후속 처리: 대기열에 다음 유닛이 있는지 확인 ---
        if (productionQueue.Count > 0)
        {
            // 대기열에서 다음 유닛을 꺼내옴
            var nextUnit = productionQueue.Dequeue();
            // 대기열 UI 업데이트 (1개 줄었으므로)
            OnQueueChanged?.Invoke(productionQueue.Count);
            // 다음 유닛 생산을 위해 자기 자신을 다시 호출 (재귀적 호출)
            StartCoroutine(ProcessSingleUnit(nextUnit.prefab, nextUnit.ownerTag));
        }
        else
        {
            // 대기열이 비어있으면 생산을 완전히 종료
            isProducing = false;
            OnProductionStatusChanged?.Invoke(false); // 슬라이더 비활성화
            OnProductionProgress?.Invoke(0f);         // 슬라이더 0%로 리셋
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
