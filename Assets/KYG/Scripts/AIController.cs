using KYG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    #region AI ���� ���� (�ű� �߰�)
    /// <summary>
    /// AI�� �ൿ�� �����ϴ� �����Դϴ�.
    /// EvaluateCurrentState() �Լ��� ���� �켱������ ���� �����˴ϴ�.
    /// </summary>
    public enum AIState
    {
        Initializing,       // �ʱ�ȭ ��
        EarlyGameDefense,   // [����] ���� �ʹ�, �ֻ��� ���� ������ �����ϰ� �� ����
        SavingForEvolve,    // [����] �ô� ������ ���� ����ġ�� ������ �� ����
        AttemptToEvolve,    // [�ൿ] �ô� ������ �õ�
        AttemptToUnlockTurret, // [�ൿ] �ͷ� ���� �ر��� �õ�
        AggressivePush,     // [����] ������ ���� ���ַ� �������� ����
        StandardProduction, // [����] �Ϲ����� ��Ȳ������ ���� ���� (������ ����)
        SavingGold          // [����] ���� ������ ������ ���� ��� ����
    }

    [Header("AI ���� (������)")]
    [SerializeField] private AIState currentState = AIState.Initializing;
    #endregion

    [Header("AI ����")]
    [SerializeField] private float decisionInterval = 1.5f; // �Ǵ� �ֱ⸦ �ణ �ٿ� �������� ���Դϴ�.
    [SerializeField] private string aiTeamTag = "P2";
    [SerializeField] private float earlyGameDefenseDuration = 20.0f;
    [Tooltip("���� �ô� ������ �ʿ��� ����ġ�� �� �ۼ�Ʈ���� ���� ��忡 ���� �����մϴ�.")]
    [Range(0.5f, 0.9f)]
    [SerializeField] private float savingStartThreshold = 0.7f; // 70%
    [Tooltip("�������� Ǫ���� ������ ��差")]
    [SerializeField] private int aggressivePushGoldTrigger = 800;


    [Header("AI �ڿ� (���� ����)")]
    private int p2_gold;
    private int p2_exp;
    private AgeType p2_currentAge = AgeType.Ancient;

    [Header("����")]
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

        currentState = AIState.EarlyGameDefense; // �ʱ� ���� ����
        StartCoroutine(DecisionLoop());
        StartCoroutine(PassiveGoldGeneration());
    }

    /// <summary>
    /// AI�� ���� ����. ���¸� �Ǵ��ϰ�, �Ǵܿ� ���� �ൿ�մϴ�.
    /// </summary>
    private IEnumerator DecisionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decisionInterval);

            // 1. ���� ��Ȳ�� ���������� �Ǵ��Ͽ� '����(State)'�� �����մϴ�.
            EvaluateCurrentState();

            // 2. ������ '����(State)'�� ���� ������ �ൿ�� �����մϴ�.
            ExecuteActionForState();
        }
    }

    #region ���� �Ǵ� �� ���� (���ο� �ٽ� ����)
    /// <summary>
    /// ���� ���� ��Ȳ�� �м��Ͽ� AI�� �ൿ ����(State)�� �����մϴ�.
    /// �켱������ ���� ���Ǻ��� ������� �˻��մϴ�.
    /// </summary>
    private void EvaluateCurrentState()
    {
        // �ֿ켱���� 1: ���� ���� �� ���� �ð� ������ �ʹ� ��� ���� ����
        if (Time.time - gameStartTime < earlyGameDefenseDuration)
        {
            currentState = AIState.EarlyGameDefense;
            return;
        }

        // �ֿ켱���� 2: �ô� ������ �����Ѱ�?
        if (ageManager.CanUpgrade(p2_currentAge, p2_exp))
        {
            currentState = AIState.AttemptToEvolve;
            return;
        }

        // TODO: �÷��̾��� Ư�� ����(��: ������ ���� ����)�� ���� �� ī���� �������� ��ȯ�ϴ� ���� �߰�
        // if (IsPlayerUsingThreateningUnit()) { currentState = AIState.Countering; return; }

        // �켱���� 3: ���� �ô���� ����ġ�� ���� ���� �̻� �𿴴ٸ�, �ٸ� �ൿ�� ���߰� ����ġ ����
        AgeData nextAgeData = ageManager.GetNextAgeData(p2_currentAge);
        if (nextAgeData != null && p2_exp >= nextAgeData.requiredExp * savingStartThreshold)
        {
            currentState = AIState.SavingForEvolve;
            return;
        }

        // �켱���� 4: ��尡 Ư���� �̻� �׿��� ��, �ͷ� �ر� �õ�
        // (��, ����ġ ���� ���� �ƴ� ����)
        if (p2_gold >= 500)
        {
            currentState = AIState.AttemptToUnlockTurret;
            return;
        }

        // �켱���� 5: ��尡 �ſ� ���ٸ�, ������ ���� ���ַ� �������� ����
        if (p2_gold >= aggressivePushGoldTrigger)
        {
            currentState = AIState.AggressivePush;
            return;
        }

        // �⺻ ����: ���� � ���ǿ��� �ش����� ������, �Ϲ����� ���� ����
        currentState = AIState.StandardProduction;
    }

    /// <summary>
    /// ������ ����(State)�� ���� ���� �ൿ�� �����մϴ�.
    /// </summary>
    private void ExecuteActionForState()
    {
        Debug.Log($"AI ({aiTeamTag}) [����: {currentState}] ���: {p2_gold}, ����ġ: {p2_exp}");

        switch (currentState)
        {
            case AIState.EarlyGameDefense:
                DecideAndSpawnUnit(true); // '�ʹ� ��� ��' �����Ͽ� ���� ����
                break;

            case AIState.AttemptToEvolve:
                TryEvolve();
                break;

            case AIState.SavingForEvolve:
                // �ƹ��͵� ���� �ʰ� ����ġ�� �����ϴ�. ���� �����̳� �ͷ� ���Ÿ� ���� �ʽ��ϴ�.
                Debug.Log($"AI ({aiTeamTag}) [����: ����ġ ����] ���� �ô븦 ���� �ڿ��� �Ƴ��ϴ�.");
                break;

            case AIState.AttemptToUnlockTurret:
                // �ͷ� �ر��� �õ��ϰ�, �����ϸ� �Ϲ� ���� �������� �Ѿ�ϴ�.
                if (!TryUnlockTurretSlot(400))
                {
                    DecideAndSpawnUnit(false);
                }
                break;

            case AIState.AggressivePush:
                DecideAndSpawnUnit(false); // '�ʹ� ��� ��' ���� ���� ��� ���� ���ַ� ����
                break;

            case AIState.StandardProduction:
                DecideAndSpawnUnit(false); // '�ʹ� ��� ��' ���� ������ ���� ����
                break;
        }
    }
    #endregion

    #region �ൿ �Լ� (���� ���� �籸��)
    /// <summary>
    /// ������ ���� ������ ������ �����ϰ� ������ ��û�մϴ�.
    /// </summary>
    /// <param name="isEarlyGame">�ʹ� ��� ��(���� ��� ���� ���� ����)�� �������� ����</param>
    private void DecideAndSpawnUnit(bool isEarlyGame)
    {
        if (isAIProducing) return; // �̹� ���� ���̸� �ƹ��͵� ���� ����

        if (!spawnableUnitsByAge.ContainsKey(p2_currentAge) || spawnableUnitsByAge[p2_currentAge].Count == 0) return;

        List<GameObject> availableUnits = spawnableUnitsByAge[p2_currentAge];

        // ���� ������ ���� ����� ���� ������������ ����
        var sortedUnits = availableUnits.OrderByDescending(u => u.GetComponent<UnitController>().unitdata.goldCost).ToList();

        GameObject unitToProduce = null;

        foreach (var unitPrefab in sortedUnits)
        {
            // �ʹ� ��� �� ����: ���� ��� ������ �ǳʶ�
            if (isEarlyGame && unitPrefab == sortedUnits[0])
            {
                Debug.Log($"AI ({aiTeamTag}) [����:�ʹ� ���] �ֻ��� ����({unitPrefab.name}) ������ �����մϴ�.");
                continue;
            }

            if (p2_gold >= unitPrefab.GetComponent<UnitController>().unitdata.goldCost)
            {
                unitToProduce = unitPrefab;
                break; // ������ ������ ã�����Ƿ� �ݺ� �ߴ�
            }
        }

        if (unitToProduce != null)
        {
            Unit unitData = unitToProduce.GetComponent<UnitController>().unitdata;
            p2_gold -= unitData.goldCost;
            isAIProducing = true;
            unitSpawnManager.RequestUnitProduction(unitToProduce, aiTeamTag);
            Debug.Log($"AI ({aiTeamTag})�� '{unitToProduce.name}' ���� ����. (���� ���: {p2_gold})");
            StartCoroutine(WaitForProductionEnd(unitData.SpawnTime));
        }
        else
        {
            // �� �κ��� ���� ���� ȣ����� ������, ������ ���� ���ܵӴϴ�.
            // ��κ��� ���� �Ǵ��� EvaluateCurrentState���� �̷�����ϴ�.
            Debug.Log($"AI ({aiTeamTag}) [����:����] ���� ������ ������ ���� ��带 �����մϴ�. (���� ���: {p2_gold})");
        }
    }

    private bool TryEvolve()
    {
        // CanUpgrade üũ�� EvaluateCurrentState���� �̹� ����������, ������ ���� �ѹ� �� üũ
        if (ageManager.CanUpgrade(p2_currentAge, p2_exp))
        {
            int requiredExp = ageManager.GetRequiredExpForNextAge(p2_currentAge);
            p2_exp -= requiredExp; // ����ġ �Ҹ�

            AgeData nextAge = ageManager.GetNextAgeData(p2_currentAge);
            if (nextAge != null)
            {
                p2_currentAge = nextAge.ageType;
                p2_Base.UpgradeBaseByAge(nextAge);
                Debug.Log($"<color=cyan>AI({aiTeamTag})�� {p2_currentAge} �ô�� �����߽��ϴ�.</color>");
                return true;
            }
        }
        return false;
    }

    private bool TryUnlockTurretSlot(int cost)
    {
        if (p2_gold >= cost)
        {
            // TODO: BaseController�� �ͷ� �ر� ������ �÷��̾� ��带 ����Ѵٸ�, AI�� ���� �Լ� �ʿ�.
            // ����� InGameManager�� SpendGold�� ����ϹǷ�, AI ��带 ���� �����ϴ� ������� ����.
            p2_gold -= cost;
            p2_Base.UnlockNextTurretSlot(0); // ��� üũ�� �̹� �����Ƿ� 0�� ����
            Debug.Log($"<color=yellow>AI({aiTeamTag})�� �ͷ� ������ �ر��߽��ϴ�.</color>");
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

    #region �ڿ� �� ����ġ ���� (���� �ڵ� ����)
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