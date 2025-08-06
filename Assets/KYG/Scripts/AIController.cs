using KYG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    #region AI 상태 정의 (신규 추가)
    /// <summary>
    /// AI의 행동을 결정하는 상태입니다.
    /// EvaluateCurrentState() 함수에 의해 우선순위에 따라 결정됩니다.
    /// </summary>
    public enum AIState
    {
        Initializing,       // 초기화 중
        EarlyGameDefense,   // [전략] 게임 초반, 최상위 유닛 생산을 보류하고 방어에 집중
        SavingForEvolve,    // [전략] 시대 발전을 위해 경험치를 모으는 데 집중
        AttemptToEvolve,    // [행동] 시대 발전을 시도
        AttemptToUnlockTurret, // [행동] 터렛 슬롯 해금을 시도
        AggressivePush,     // [전략] 강력한 유닛 위주로 공격적인 생산
        StandardProduction, // [전략] 일반적인 상황에서의 유닛 생산 (가성비 위주)
        SavingGold          // [전략] 생산 가능한 유닛이 없어 골드 저축
    }

    [Header("AI 상태 (디버깅용)")]
    [SerializeField] private AIState currentState = AIState.Initializing;
    #endregion

    [Header("AI 설정")]
    [SerializeField] private float decisionInterval = 1.5f; // 판단 주기를 약간 줄여 반응성을 높입니다.
    [SerializeField] private string aiTeamTag = "P2";
    [SerializeField] private float earlyGameDefenseDuration = 20.0f;
    [Tooltip("다음 시대 발전에 필요한 경험치의 몇 퍼센트부터 저축 모드에 들어갈지 결정합니다.")]
    [Range(0.5f, 0.9f)]
    [SerializeField] private float savingStartThreshold = 0.7f; // 70%
    [Tooltip("공격적인 푸쉬를 시작할 골드량")]
    [SerializeField] private int aggressivePushGoldTrigger = 800;


    [Header("AI 자원 (내부 관리)")]
    private int p2_gold;
    private int p2_exp;
    private AgeType p2_currentAge = AgeType.Ancient;

    [Header("참조")]
    private UnitSpawnManager unitSpawnManager;
    private AgeManager ageManager;
    private BaseController p2_Base;

    private bool isAIProducing = false;
    private float gameStartTime;

    private Dictionary<AgeType, List<GameObject>> spawnableUnitsByAge = new Dictionary<AgeType, List<GameObject>>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (!InGameManager.Instance.isDebugMode || !InGameManager.Instance.isDebugHost)
        {
            gameObject.SetActive(false);
            return;
        }

        unitSpawnManager = UnitSpawnManager.Instance;
        ageManager = AgeManager.Instance;
        gameStartTime = Time.time;

        StartCoroutine(FindP2BaseAndStart());
    }

    private IEnumerator FindP2BaseAndStart()
    {
        while (InGameManager.Instance.p2_Base == null)
        {
            yield return null;
        }
        p2_Base = InGameManager.Instance.p2_Base;
        p2_gold = 50;
        p2_exp = 0;

        for (AgeType age = AgeType.Ancient; age <= AgeType.Modern; age++)
        {
            AgeData data = ageManager.GetAgeData(age);
            if (data != null)
            {
                spawnableUnitsByAge[age] = data.spawnableUnits;
            }
        }

        currentState = AIState.EarlyGameDefense; // 초기 상태 설정
        StartCoroutine(DecisionLoop());
        StartCoroutine(PassiveGoldGeneration());
    }

    /// <summary>
    /// AI의 메인 로직. 상태를 판단하고, 판단에 따라 행동합니다.
    /// </summary>
    private IEnumerator DecisionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decisionInterval);

            // 1. 현재 상황을 종합적으로 판단하여 '전략(State)'을 결정합니다.
            EvaluateCurrentState();

            // 2. 결정된 '전략(State)'에 따라 적절한 행동을 실행합니다.
            ExecuteActionForState();
        }
    }

    #region 상태 판단 및 실행 (새로운 핵심 로직)
    /// <summary>
    /// 현재 게임 상황을 분석하여 AI의 행동 상태(State)를 결정합니다.
    /// 우선순위가 높은 조건부터 순서대로 검사합니다.
    /// </summary>
    private void EvaluateCurrentState()
    {
        // 최우선순위 1: 게임 시작 후 일정 시간 동안은 초반 방어 전략 유지
        if (Time.time - gameStartTime < earlyGameDefenseDuration)
        {
            currentState = AIState.EarlyGameDefense;
            return;
        }

        // 최우선순위 2: 시대 발전이 가능한가?
        if (ageManager.CanUpgrade(p2_currentAge, p2_exp))
        {
            currentState = AIState.AttemptToEvolve;
            return;
        }

        // TODO: 플레이어의 특정 유닛(예: 강력한 광역 유닛)을 감지 시 카운터 전략으로 전환하는 로직 추가
        // if (IsPlayerUsingThreateningUnit()) { currentState = AIState.Countering; return; }

        // 우선순위 3: 다음 시대까지 경험치가 일정 비율 이상 모였다면, 다른 행동을 멈추고 경험치 저축
        AgeData nextAgeData = ageManager.GetNextAgeData(p2_currentAge);
        if (nextAgeData != null && p2_exp >= nextAgeData.requiredExp * savingStartThreshold)
        {
            currentState = AIState.SavingForEvolve;
            return;
        }

        // 우선순위 4: 골드가 특정량 이상 쌓였을 때, 터렛 해금 시도
        // (단, 경험치 저축 중이 아닐 때만)
        if (p2_gold >= 500)
        {
            currentState = AIState.AttemptToUnlockTurret;
            return;
        }

        // 우선순위 5: 골드가 매우 많다면, 강력한 유닛 위주로 공격적인 생산
        if (p2_gold >= aggressivePushGoldTrigger)
        {
            currentState = AIState.AggressivePush;
            return;
        }

        // 기본 상태: 위의 어떤 조건에도 해당하지 않으면, 일반적인 유닛 생산
        currentState = AIState.StandardProduction;
    }

    /// <summary>
    /// 결정된 상태(State)에 따라 실제 행동을 실행합니다.
    /// </summary>
    private void ExecuteActionForState()
    {
        Debug.Log($"AI ({aiTeamTag}) [상태: {currentState}] 골드: {p2_gold}, 경험치: {p2_exp}");

        switch (currentState)
        {
            case AIState.EarlyGameDefense:
                DecideAndSpawnUnit(true); // '초반 방어 룰' 적용하여 유닛 생산
                break;

            case AIState.AttemptToEvolve:
                TryEvolve();
                break;

            case AIState.SavingForEvolve:
                // 아무것도 하지 않고 경험치를 모읍니다. 유닛 생산이나 터렛 구매를 하지 않습니다.
                Debug.Log($"AI ({aiTeamTag}) [전략: 경험치 저축] 다음 시대를 위해 자원을 아낍니다.");
                break;

            case AIState.AttemptToUnlockTurret:
                // 터렛 해금을 시도하고, 실패하면 일반 유닛 생산으로 넘어갑니다.
                if (!TryUnlockTurretSlot(400))
                {
                    DecideAndSpawnUnit(false);
                }
                break;

            case AIState.AggressivePush:
                DecideAndSpawnUnit(false); // '초반 방어 룰' 없이 가장 비싼 유닛 위주로 생산
                break;

            case AIState.StandardProduction:
                DecideAndSpawnUnit(false); // '초반 방어 룰' 없이 가성비 유닛 생산
                break;
        }
    }
    #endregion

    #region 행동 함수 (기존 로직 재구성)
    /// <summary>
    /// 전략에 따라 생산할 유닛을 결정하고 생산을 요청합니다.
    /// </summary>
    /// <param name="isEarlyGame">초반 방어 룰(가장 비싼 유닛 생산 보류)을 적용할지 여부</param>
    private void DecideAndSpawnUnit(bool isEarlyGame)
    {
        if (isAIProducing) return; // 이미 생산 중이면 아무것도 하지 않음

        if (!spawnableUnitsByAge.ContainsKey(p2_currentAge) || spawnableUnitsByAge[p2_currentAge].Count == 0) return;

        List<GameObject> availableUnits = spawnableUnitsByAge[p2_currentAge];

        // 생산 가능한 유닛 목록을 가격 내림차순으로 정렬
        var sortedUnits = availableUnits.OrderByDescending(u => u.GetComponent<UnitController>().unitdata.goldCost).ToList();

        GameObject unitToProduce = null;

        foreach (var unitPrefab in sortedUnits)
        {
            // 초반 방어 룰 적용: 가장 비싼 유닛은 건너뜀
            if (isEarlyGame && unitPrefab == sortedUnits[0])
            {
                Debug.Log($"AI ({aiTeamTag}) [전략:초반 방어] 최상위 유닛({unitPrefab.name}) 생산을 보류합니다.");
                continue;
            }

            if (p2_gold >= unitPrefab.GetComponent<UnitController>().unitdata.goldCost)
            {
                unitToProduce = unitPrefab;
                break; // 생산할 유닛을 찾았으므로 반복 중단
            }
        }

        if (unitToProduce != null)
        {
            Unit unitData = unitToProduce.GetComponent<UnitController>().unitdata;
            p2_gold -= unitData.goldCost;
            isAIProducing = true;
            unitSpawnManager.RequestUnitProduction(unitToProduce, aiTeamTag);
            Debug.Log($"AI ({aiTeamTag})가 '{unitToProduce.name}' 생산 시작. (남은 골드: {p2_gold})");
            StartCoroutine(WaitForProductionEnd(unitData.SpawnTime));
        }
        else
        {
            // 이 부분은 이제 거의 호출되지 않지만, 만약을 위해 남겨둡니다.
            // 대부분의 저축 판단은 EvaluateCurrentState에서 이루어집니다.
            Debug.Log($"AI ({aiTeamTag}) [전략:저축] 생산 가능한 유닛이 없어 골드를 저축합니다. (현재 골드: {p2_gold})");
        }
    }

    private bool TryEvolve()
    {
        // CanUpgrade 체크는 EvaluateCurrentState에서 이미 수행했지만, 안전을 위해 한번 더 체크
        if (ageManager.CanUpgrade(p2_currentAge, p2_exp))
        {
            int requiredExp = ageManager.GetRequiredExpForNextAge(p2_currentAge);
            p2_exp -= requiredExp; // 경험치 소모

            AgeData nextAge = ageManager.GetNextAgeData(p2_currentAge);
            if (nextAge != null)
            {
                p2_currentAge = nextAge.ageType;
                p2_Base.UpgradeBaseByAge(nextAge);
                Debug.Log($"<color=cyan>AI({aiTeamTag})가 {p2_currentAge} 시대로 발전했습니다.</color>");
                return true;
            }
        }
        return false;
    }

    private bool TryUnlockTurretSlot(int cost)
    {
        if (p2_gold >= cost)
        {
            // TODO: BaseController의 터렛 해금 로직이 플레이어 골드를 사용한다면, AI용 별도 함수 필요.
            // 현재는 InGameManager의 SpendGold를 사용하므로, AI 골드를 직접 차감하는 방식으로 수정.
            p2_gold -= cost;
            p2_Base.UnlockNextTurretSlot(0); // 비용 체크는 이미 했으므로 0을 전달
            Debug.Log($"<color=yellow>AI({aiTeamTag})가 터렛 슬롯을 해금했습니다.</color>");
            return true;
        }
        return false;
    }

    private IEnumerator WaitForProductionEnd(float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime);
        isAIProducing = false;
    }
    #endregion

    #region 자원 및 경험치 관리 (기존 코드 유지)
    private IEnumerator PassiveGoldGeneration()
    {
        var fiveSecondWait = new WaitForSeconds(5f);
        while (true)
        {
            yield return fiveSecondWait;
            int goldToAdd = 0;
            switch (p2_currentAge)
            {
                case AgeType.Ancient: goldToAdd = 15; break;
                case AgeType.Medieval: goldToAdd = 40; break;
                case AgeType.Modern: goldToAdd = 100; break;
            }
            AddGold(goldToAdd);
        }
    }

    public void AddGold(int amount) { p2_gold += amount; }
    public void AddExp(int amount)
    {
        p2_exp += amount;
    }
    #endregion
}