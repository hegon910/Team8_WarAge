using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// ������ �������� ���¿� �ٽ� ������ �����ϴ� �̱��� �޴��� Ŭ����
/// </summary>
public class InGameManager : MonoBehaviour
{
    public static InGameManager Instance { get; private set; }

    [Header("�������")]

    public PHK.UnitPanelManager unitPanelManager;

    [Header("���� �⺻ ����")]
    [SerializeField] private int startingGold = 175; //���� ���۽� ���� ���
    [SerializeField] private int maxBaseHealth = 1000; //������ �ִ� ü��
    [Tooltip("�ô� ������ �ʿ��� ���� ����ġ")]
    [SerializeField] private int[] expForEvolve;

    [Header("���� ���� ����")]
    [SerializeField] Transform p1_spawnPoint;
    [SerializeField] Transform p2_spawnPoint;
    //��⿭
    private Queue<(GameObject prefab, string ownerTag)> productionQueue = new Queue<(GameObject prefab, string ownerTag)>();  
    //���� ���� ������ ���� ������
    private bool isProducing = false;


    public event Action<int> OnQueueChanged;
    public event Action<float> OnProductionProgress;
    public event Action<bool> OnProductionStatusChanged;

    //���� ���� ���� ����
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
        //���� ���� �ʱ�ȭ
        currentGold = startingGold;
        currentEXP = 0;
        currentBaseHealth = maxBaseHealth;

        //�ʱ� ���� ���¸� UI�� �ݿ�
        InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
        InGameUIManager.Instance.UpdateBaseHpUI(currentBaseHealth, maxBaseHealth);

        //���� ���۽� �ô� ���� ��ư�� ��Ȱ��ȭ ���·� ����
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
            Debug.LogError("P1 �Ǵ� P2 ���� ����Ʈ�� Inspector�� �Ҵ���� �ʾҽ��ϴ�!");
        }
    }

    #region �ڿ� �� ü�� ���� �Լ�
    ///<summary>
    ///��� ȹ�� (����� ���� óġ ��)
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold += amount;
        InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
        Debug.Log($"{amount} ��� ȹ��. ���� ��� : {currentGold}");
    }
    ///<summary>
    ///��� �Ҹ�(���� �� �ͷ� ���� ��)
    /// </summary>
    public bool SpendGold(Unit unitToPurchase)
    {
        int amount = unitToPurchase.goldCost;
        if(currentGold>=amount)
        {
            currentGold -= amount;
            InGameUIManager.Instance.UpdateResourceUI(currentGold,currentEXP);
            Debug.Log($"{amount}��� �Ҹ�. ���� ��� : {currentGold}");
         
            return true;
        }
        else
        {
            Debug.Log("��尡 �����մϴ�.");
            //UIManager, InGameInfoText���� �˸� ���� �߰�
            InGameUIManager.Instance.inGameInfoText.text = $"Can't Spawn {unitToPurchase.unitName} !! Not Enough Gold!";
            InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
            return false;
        }        
    }

    ///<summary>
    ///����ġ ȹ�� (�ַ� �� ���� óġ)
    /// </summary>
    /// 
    
    public void AddExp(int amount)
    {
        //������ �ô밡 �ƴ� ��쿡�� ����ġ ȹ���ϸ�, �ô� ������ üũ
        if(unitPanelManager.currentAge < PHK.UnitPanelManager.Age.Future) //������������ �ϸ� Age.Modern���� ����
        {
            currentEXP += amount;
            InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
            CheckForAgeUp();
            Debug.Log($"{amount} ����ġ ȹ��.");
        }
    }
    ///<summary>
    ///���� ü�� ����(���� �޾��� ��)
    /// </summary>

    public void TakeBaseDamage(int damage)
    {
        currentBaseHealth -= damage;
        currentBaseHealth = Mathf.Max(currentBaseHealth, 0); //ü���� 0 �̸����� �������� �ʵ��� ����
        InGameUIManager.Instance.UpdateBaseHpUI(currentBaseHealth, maxBaseHealth);

        if (currentBaseHealth <= 0) GameOver();
    }
    #endregion

    #region �ô� ���� ����
    ///<summary>
    ///�ô� ���� �õ�(UnitPanel�� �ô� ���� ��ư Onclick �̺�Ʈ)
    /// </summary>
    public void AttemptEvolve()
    {
        int currentAgeIndex = (int)unitPanelManager.currentAge;
        //���� ������ ���� �ô�, �ʿ� ����ġ ������ Ȯ��
        if(currentAgeIndex < expForEvolve.Length && currentEXP >= expForEvolve[currentAgeIndex])
        {
            //UnitPanelManager�� �ô� ���� �Լ��� ȣ���Ͽ� UI�� ���� �ô� �гη� ��ü
            unitPanelManager.EvolveToNextAge();
            //�ô� ���� �� ��ư ��Ȱ��ȭ
            unitPanelManager.evolveButton.interactable = false;

            //Ȥ�� ���� �ô� ���� ���� �������� ��츦 ����� �ٽ� �� �� üũ
            CheckForAgeUp();
        }
    }

    ///<summary>
    ///����ġ�� ȹ���� ������ �ô� ������ �������� Ȯ�� �� ��ư Ȱ��ȭ�� ����
    /// </summary>
    private void CheckForAgeUp()
    { 
        int currentAgeIndex = (int )unitPanelManager.currentAge;

        //���� �ô�� ���� ��ư���� ��Ȱ��ȭ ������ �� üũ
        if(currentAgeIndex < expForEvolve.Length && !unitPanelManager.evolveButton.interactable)
        {
            //���� ����ġ�� �ʿ� ����ġ �̻��̸� ���� ��ư Ȱ��ȭ
            if(currentEXP >= expForEvolve[currentAgeIndex])
            {
                unitPanelManager.evolveButton.interactable=true;
                InGameUIManager.Instance.inGameInfoText.text = "You Can Evolve Age!";
                InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
            }
        }
    }
    #endregion

    #region ���� ���� ����
    ///<summary>
    ///�������� ���� ť�� �߰��ϴ� �Լ�
    /// </summary>
    /// 
    public void RequestUnitProduction(GameObject unitPrefab, string ownerTag)
    {
        // ���� ������ ���� ������ �ƴ����� ���� ������ �и�
        if (isProducing)
        {
            // --- �̹� �ٸ� ������ ���� ���� ���: ��⿭(Queue)�� �߰� ---
            if (productionQueue.Count < 5) // ť�� 5ĭ
            {
                Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
                if (SpendGold(unitData))
                {
                    productionQueue.Enqueue((unitPrefab, ownerTag));
                    // ��⿭ UI ������Ʈ (���� ������� ���� ���� ����)
                    OnQueueChanged?.Invoke(productionQueue.Count);
                }
            }
            else
            {
                // ��⿭�� �� á�� ���� ó�� (�޽��� ��)
            }
        }
        else
        {
            // --- ���� ������ ������� ���: �ٷ� ���� ���� ---
            Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
            if (SpendGold(unitData))
            {
                // �� ������ ť�� ��ġ�� �ʰ� �ٷ� ���� �ڷ�ƾ���� ����
                StartCoroutine(ProcessSingleUnit(unitPrefab, ownerTag));
            }
        }
    }

    /// <summary>
    /// ���� '�� ��'�� �����ϰ�, �Ϸ�Ǹ� ��⿭���� ���� ������ ������ �ٽ� �� �ڷ�ƾ�� ����
    /// </summary>
    private IEnumerator ProcessSingleUnit(GameObject prefabToProduce, string ownerTag)
    {
        // --- ���� ���� ó�� ---
        isProducing = true;
        OnProductionStatusChanged?.Invoke(true); // �����̴� Ȱ��ȭ
        OnProductionProgress?.Invoke(0f);      // �����̴� 0%���� ����

        Unit unitData = prefabToProduce.GetComponent<UnitController>().unitdata;

        // --- ���� �ð� ���� ��� ---
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
        OnProductionProgress?.Invoke(1f); // 100% ä���

        // --- ���� ���� ---
        Transform spawnPoint = (ownerTag == "P1") ? p1_spawnPoint : p2_spawnPoint;
        if (spawnPoint != null)
        {
            Instantiate(prefabToProduce, spawnPoint.position, spawnPoint.rotation);
        }

        // --- �ļ� ó��: ��⿭�� ���� ������ �ִ��� Ȯ�� ---
        if (productionQueue.Count > 0)
        {
            // ��⿭���� ���� ������ ������
            var nextUnit = productionQueue.Dequeue();
            // ��⿭ UI ������Ʈ (1�� �پ����Ƿ�)
            OnQueueChanged?.Invoke(productionQueue.Count);
            // ���� ���� ������ ���� �ڱ� �ڽ��� �ٽ� ȣ�� (����� ȣ��)
            StartCoroutine(ProcessSingleUnit(nextUnit.prefab, nextUnit.ownerTag));
        }
        else
        {
            // ��⿭�� ��������� ������ ������ ����
            isProducing = false;
            OnProductionStatusChanged?.Invoke(false); // �����̴� ��Ȱ��ȭ
            OnProductionProgress?.Invoke(0f);         // �����̴� 0%�� ����
        }
    }

    #endregion

   

    ///<summary>
    ///���� ���� ó��
    /// </summary>
    private void GameOver()
    {
        Debug.Log("���� ���� ó�� �߰� �۾� �ʿ�");
        Time.timeScale = 0f;
    }
}
