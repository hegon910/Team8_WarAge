using KYG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    [Header("AI 설정")]
    [SerializeField] private float decisionInterval = 2.0f;
    [SerializeField] private string aiTeamTag = "P2";
    // [추가] 초반 방어 전략을 위한 시간 변수
    [SerializeField] private float earlyGameDefenseDuration = 20.0f;

    [Header("AI 자원 (내부 관리)")]
    private int p2_gold;
    private int p2_exp;
    private AgeType p2_currentAge = AgeType.Ancient;

    [Header("참조")]
    private UnitSpawnManager unitSpawnManager;
    private AgeManager ageManager;
    private BaseController p2_Base;

    private bool isAIProducing = false;
    // [추가] 게임 시작 시간을 기록할 변수
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

        // [추가] AI가 활성화되는 시점의 게임 시간을 기록
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

        StartCoroutine(DecisionLoop());
        StartCoroutine(PassiveGoldGeneration());
    }

    private IEnumerator DecisionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decisionInterval);

            // [수정] isAIProducing 체크를 여기서 제거하여 생산 중에도 다른 결정을 내릴 수 있게 함

            // 1순위: 시대 발전 시도
            if (TryEvolve()) continue;

            // 2순위: 골드가 500 이상이면 터렛 해금 시도
            if (p2_gold >= 500)
            {
                if (TryUnlockTurretSlot(400))
                {
                    // 터렛 해금 성공 시 다음 결정까지 대기
                    continue;
                }
            }

            // 3순위: 유닛 생산 시도
            TrySpawnUnit();
        }
    }

    private void TrySpawnUnit()
    {
        // [수정] isAIProducing 체크를 이곳으로 이동. 유닛 생산은 동시에 여러 개 할 수 없음.
        if (isAIProducing) return;

        if (!spawnableUnitsByAge.ContainsKey(p2_currentAge) || spawnableUnitsByAge[p2_currentAge].Count == 0) return;

        List<GameObject> availableUnits = spawnableUnitsByAge[p2_currentAge];

        // [추가] 초반 20초 룰을 위한 로직
        // 현재 시대에서 가장 비싼 유닛을 찾음
        GameObject mostExpensiveUnit = GetMostExpensiveUnit(availableUnits);

        // 생산 가능한 유닛 목록을 가격 내림차순으로 정렬
        var sortedUnits = availableUnits.OrderByDescending(u => u.GetComponent<UnitController>().unitdata.goldCost).ToList();

        GameObject unitToProduce = null;

        // 정렬된 목록을 순회하며 생산할 유닛 결정
        foreach (var unitPrefab in sortedUnits)
        {
            // [추가] 게임 시작 후 20초가 지나지 않았고, 현재 유닛이 가장 비싼 유닛이라면 건너뜀
            if (Time.time - gameStartTime < earlyGameDefenseDuration && unitPrefab == mostExpensiveUnit)
            {
                Debug.Log($"AI ({aiTeamTag}) [전략:초반 방어] 20초가 지나지 않아 최상위 유닛({unitPrefab.name}) 생산을 보류합니다.");
                continue;
            }

            // 현재 골드로 살 수 있는지 확인
            if (p2_gold >= unitPrefab.GetComponent<UnitController>().unitdata.goldCost)
            {
                unitToProduce = unitPrefab;
                break; // 살 수 있는 가장 비싼 유닛을 찾았으므로 반복 중단
            }
        }

        if (unitToProduce == null)
        {
            // 현재 골드로 생산 가능한 유닛이 아무것도 없으면 저축
            Debug.Log($"AI ({aiTeamTag}) [전략:저축] 생산 가능한 유닛이 없어 골드를 저축합니다. (현재 골드: {p2_gold})");
            return;
        }

        Unit unitData = unitToProduce.GetComponent<UnitController>().unitdata;

        // 골드가 충분한지 최종 확인 후 생산 요청
        if (p2_gold >= unitData.goldCost)
        {
            p2_gold -= unitData.goldCost;
            isAIProducing = true;
            unitSpawnManager.RequestUnitProduction(unitToProduce, aiTeamTag);
            Debug.Log($"AI ({aiTeamTag})가 '{unitToProduce.name}' 생산 시작. (남은 골드: {p2_gold})");
            StartCoroutine(WaitForProductionEnd(unitData.SpawnTime));
        }
    }

    // ... (기존 코드와 동일한 부분) ...

    private IEnumerator WaitForProductionEnd(float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime);
        isAIProducing = false;
        Debug.Log("AI 유닛 생산 완료. 다음 생산 가능.");
    }

    private bool TryEvolve()
    {
        if (ageManager.CanUpgrade(p2_currentAge, p2_exp))
        {
            AgeData nextAge = ageManager.GetNextAgeData(p2_currentAge);
            if (nextAge != null)
            {
                p2_currentAge = nextAge.ageType;
                p2_Base.UpgradeBaseByAge(nextAge);
                Debug.Log($"AI({aiTeamTag})가 {p2_currentAge} 시대로 발전했습니다.");
                return true;
            }
        }
        return false;
    }

    private bool TryUnlockTurretSlot(int cost)
    {
        if (p2_gold >= cost)
        {
            p2_gold -= cost;
            p2_Base.UnlockNextTurretSlot(0);
            Debug.Log($"AI({aiTeamTag})가 터렛 슬롯을 해금했습니다.");
            return true;
        }
        return false;
    }

    // 이 함수는 이제 직접 사용되지 않지만, 다른 로직을 위해 남겨둘 수 있습니다.
    private GameObject GetCheapestUnit(List<GameObject> units)
    {
        if (units == null || units.Count == 0) return null;
        return units.OrderBy(u => u.GetComponent<UnitController>().unitdata.goldCost).FirstOrDefault();
    }

    private GameObject GetMostExpensiveUnit(List<GameObject> units)
    {
        if (units == null || units.Count == 0) return null;
        return units.OrderByDescending(u => u.GetComponent<UnitController>().unitdata.goldCost).FirstOrDefault();
    }

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
        Debug.Log($"AI({aiTeamTag})가 경험치 {amount}를 획득. 현재 경험치: {p2_exp}");
    }
}