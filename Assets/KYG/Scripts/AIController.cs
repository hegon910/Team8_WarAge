using KYG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    [Header("AI ����")]
    [SerializeField] private float decisionInterval = 2.0f;
    [SerializeField] private string aiTeamTag = "P2";
    // [�߰�] �ʹ� ��� ������ ���� �ð� ����
    [SerializeField] private float earlyGameDefenseDuration = 20.0f;

    [Header("AI �ڿ� (���� ����)")]
    private int p2_gold;
    private int p2_exp;
    private AgeType p2_currentAge = AgeType.Ancient;

    [Header("����")]
    private UnitSpawnManager unitSpawnManager;
    private AgeManager ageManager;
    private BaseController p2_Base;

    private bool isAIProducing = false;
    // [�߰�] ���� ���� �ð��� ����� ����
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

        // [�߰�] AI�� Ȱ��ȭ�Ǵ� ������ ���� �ð��� ���
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

            // [����] isAIProducing üũ�� ���⼭ �����Ͽ� ���� �߿��� �ٸ� ������ ���� �� �ְ� ��

            // 1����: �ô� ���� �õ�
            if (TryEvolve()) continue;

            // 2����: ��尡 500 �̻��̸� �ͷ� �ر� �õ�
            if (p2_gold >= 500)
            {
                if (TryUnlockTurretSlot(400))
                {
                    // �ͷ� �ر� ���� �� ���� �������� ���
                    continue;
                }
            }

            // 3����: ���� ���� �õ�
            TrySpawnUnit();
        }
    }

    private void TrySpawnUnit()
    {
        // [����] isAIProducing üũ�� �̰����� �̵�. ���� ������ ���ÿ� ���� �� �� �� ����.
        if (isAIProducing) return;

        if (!spawnableUnitsByAge.ContainsKey(p2_currentAge) || spawnableUnitsByAge[p2_currentAge].Count == 0) return;

        List<GameObject> availableUnits = spawnableUnitsByAge[p2_currentAge];

        // [�߰�] �ʹ� 20�� ���� ���� ����
        // ���� �ô뿡�� ���� ��� ������ ã��
        GameObject mostExpensiveUnit = GetMostExpensiveUnit(availableUnits);

        // ���� ������ ���� ����� ���� ������������ ����
        var sortedUnits = availableUnits.OrderByDescending(u => u.GetComponent<UnitController>().unitdata.goldCost).ToList();

        GameObject unitToProduce = null;

        // ���ĵ� ����� ��ȸ�ϸ� ������ ���� ����
        foreach (var unitPrefab in sortedUnits)
        {
            // [�߰�] ���� ���� �� 20�ʰ� ������ �ʾҰ�, ���� ������ ���� ��� �����̶�� �ǳʶ�
            if (Time.time - gameStartTime < earlyGameDefenseDuration && unitPrefab == mostExpensiveUnit)
            {
                Debug.Log($"AI ({aiTeamTag}) [����:�ʹ� ���] 20�ʰ� ������ �ʾ� �ֻ��� ����({unitPrefab.name}) ������ �����մϴ�.");
                continue;
            }

            // ���� ���� �� �� �ִ��� Ȯ��
            if (p2_gold >= unitPrefab.GetComponent<UnitController>().unitdata.goldCost)
            {
                unitToProduce = unitPrefab;
                break; // �� �� �ִ� ���� ��� ������ ã�����Ƿ� �ݺ� �ߴ�
            }
        }

        if (unitToProduce == null)
        {
            // ���� ���� ���� ������ ������ �ƹ��͵� ������ ����
            Debug.Log($"AI ({aiTeamTag}) [����:����] ���� ������ ������ ���� ��带 �����մϴ�. (���� ���: {p2_gold})");
            return;
        }

        Unit unitData = unitToProduce.GetComponent<UnitController>().unitdata;

        // ��尡 ������� ���� Ȯ�� �� ���� ��û
        if (p2_gold >= unitData.goldCost)
        {
            p2_gold -= unitData.goldCost;
            isAIProducing = true;
            unitSpawnManager.RequestUnitProduction(unitToProduce, aiTeamTag);
            Debug.Log($"AI ({aiTeamTag})�� '{unitToProduce.name}' ���� ����. (���� ���: {p2_gold})");
            StartCoroutine(WaitForProductionEnd(unitData.SpawnTime));
        }
    }

    // ... (���� �ڵ�� ������ �κ�) ...

    private IEnumerator WaitForProductionEnd(float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime);
        isAIProducing = false;
        Debug.Log("AI ���� ���� �Ϸ�. ���� ���� ����.");
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
                Debug.Log($"AI({aiTeamTag})�� {p2_currentAge} �ô�� �����߽��ϴ�.");
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
            Debug.Log($"AI({aiTeamTag})�� �ͷ� ������ �ر��߽��ϴ�.");
            return true;
        }
        return false;
    }

    // �� �Լ��� ���� ���� ������ ������, �ٸ� ������ ���� ���ܵ� �� �ֽ��ϴ�.
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
        Debug.Log($"AI({aiTeamTag})�� ����ġ {amount}�� ȹ��. ���� ����ġ: {p2_exp}");
    }
}