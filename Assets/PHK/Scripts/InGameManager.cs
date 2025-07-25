using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ������ �������� ���¿� �ٽ� ������ �����ϴ� �̱��� �޴��� Ŭ����
/// </summary>
public class InGameManager : MonoBehaviourPunCallbacks
{
    #region ����
    //����� �ɼ� ���Ž� ���� ����
    [Header("����� �ɼ�")]
    [Tooltip("üũ�ϸ� ��Ʈ��ũ ���� ���� P1(ȣ��Ʈ)�� �����ϰ� �׽�Ʈ")]
    public bool isDebugMode = false;
    [Tooltip("is_Debug_Mode�� ���� ���� ���� ����. P1(ȣ��Ʈ)�� �׽�Ʈ�Ϸ��� üũ, P2(Ŭ���̾�Ʈ)�� �׽�Ʈ�Ϸ��� üũ ����")]
    public bool isDebugHost = true; // P1 �������� P2 �������� ����
    //-----------------
    public static InGameManager Instance { get; private set; }

    [Header("�������")]

    public PHK.UnitPanelManager unitPanelManager;
    public KYG.BaseController p1_Base;
    public KYG.BaseController p2_Base;

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
    public event Action<PHK.UnitPanelManager.Age> OnAgeEvolved;

    //���� ���� ���� ����
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
    [Header("�÷��̾� �ൿ ����")]
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
        //���� ���� �ʱ�ȭ
        currentGold = startingGold;
        currentEXP = 0;
        currentBaseHealth = maxBaseHealth;

        //�ʱ� ���� ���¸� UI�� �ݿ�
        InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
        InGameUIManager.Instance.UpdateBaseHpUI(currentBaseHealth, maxBaseHealth);

        //���� ���۽� �ô� ���� ��ư�� ��Ȱ��ȭ ���·� ����
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
            Debug.LogError("P1 �Ǵ� P2 ���� ����Ʈ�� Inspector�� �Ҵ���� �ʾҽ��ϴ�!");
        }

        InGameUIManager.Instance.UnitInfoText.text = "Game Started!"; // ���� ���� �޽��� ǥ��
    }

    private void Update()
    {
        if (isDebugMode)
        {

            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddGold(50); //�׽�Ʈ������ Space Ű�� ������ 50 ��� �߰�
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                currentEXP += 500; //�׽�Ʈ������ 100 ����ġ �߰�
                InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
                CheckForAgeUp();
            }
        }
        HandleTurretActions();
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
    public bool SpendGold(int amount)
    {
        
        if (currentGold >= amount)
        {
            currentGold -= amount;
            InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
            Debug.Log($"{amount}��� �Ҹ�. ���� ��� : {currentGold}");

            return true;
        }
        else
        {
            Debug.Log("��尡 �����մϴ�.");
            //UIManager, InGameInfoText���� �˸� ���� �߰�
            InGameUIManager.Instance.inGameInfoText.text = $"Can't Spawn!! Not Enough Gold!";
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
        if (unitPanelManager.currentAge < PHK.UnitPanelManager.Age.Future) //������������ �ϸ� Age.Modern���� ����
        {
            currentEXP += amount;
            InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
            CheckForAgeUp();
            Debug.Log($"{amount} ����ġ ȹ��.");
        }


    }

    public bool CanAfford(int amount)
    {
        return currentGold >= amount;
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
    /// <summary>
    /// ���� ������ ���� Ư�� ����� ������ �� �ִ��� Ȯ���մϴ�.
    /// </summary>

    #endregion

    #region �ô� ���� ����
    ///<summary>
    ///�ô� ���� �õ�(UnitPanel�� �ô� ���� ��ư Onclick �̺�Ʈ)
    /// </summary>
    ///<summary>
    /// �ô� ������ '�õ�'�ϴ� �Լ�. ���� ������ ������ Ŭ���̾�Ʈ�� ��û
    /// (UnitPanel�� �ô� ���� ��ư OnClick �̺�Ʈ�� ����� �Լ�)
    /// </summary>
    public void AttemptEvolve()
    {
        // ����� ����� ���, ����ó�� ���ÿ��� �ٷ� ó��
        if (isDebugMode)
        {
            EvolveLocally();
        }
        else
        {
            // ������ Ŭ���̾�Ʈ���� �ô� ������ ��û�ϴ� RPC�� ����
            // ��û�ϴ� �÷��̾��� ActorNumber�� �Բ� ����
            photonView.RPC("RPC_RequestEvolve", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log("������ Ŭ���̾�Ʈ���� �ô� ������ ��û�մϴ�.");
        }
    }
    /// <summary>
    /// [MasterClient Only] Ŭ���̾�Ʈ�κ��� �ô� ���� ��û�� �޾� ó���ϴ� RPC �Լ�
    /// </summary>
    [PunRPC]
    private void RPC_RequestEvolve(int requestingPlayerActorNumber)
    {
        // �� �Լ��� ������ Ŭ���̾�Ʈ������ ����
        if (!PhotonNetwork.IsMasterClient) return;

        // �߿�: ���� ���������� �� Ŭ���̾�Ʈ�� �ڽ��� ����ġ(currentEXP)�� ����.
        // �̻������δ� ������ Ŭ���̾�Ʈ�� ��� �÷��̾��� ����ġ�� �����ؾ� ġ���� ���� �� �ִ�
        // ������ ��û�� Ŭ���̾�Ʈ�� ������ �����ߴٰ� '�ϰ�' ����������, ���� ������ �ʿ�

        // ���⿡�� �ش� �÷��̾ ��ȭ �������� ������ �˻�.
        // ��: int requiredExp = expForEvolve[currentAgeIndex];
        // if (playerExp >= requiredExp) { ... }
        // ������ ���� ������ �����Ƿ� �ٷ� Ȯ�� RPC�� ȣ��.

        Debug.Log($"{requestingPlayerActorNumber}�� �÷��̾��� �ô� ���� ��û�� ���� �� ���� (����� �ڵ� ���)");

        // ��� Ŭ���̾�Ʈ���� Ư�� �÷��̾ �ô븦 ���������� �˸�.
        // ������ �÷��̾��� ActorNumber�� ���ο� �ô��� �ε����� ����.
        int nextAgeIndex = (int)unitPanelManager.currentAge + 1; // ���� unitPanelManager�� �������� ���� �ô븦 ���
        photonView.RPC("RPC_ConfirmEvolve", RpcTarget.All, requestingPlayerActorNumber, nextAgeIndex);
    }
    /// <summary>
    /// [All Clients] ������ Ŭ���̾�Ʈ�κ��� �ô� ���� Ȯ���� �޾� ���� ���ӿ� �����ϴ� RPC �Լ�
    /// </summary>
    [PunRPC]
    private void RPC_ConfirmEvolve(int targetPlayerActorNumber, int newAgeIndex)
    {
        Debug.Log($"{targetPlayerActorNumber}�� �÷��̾ {(PHK.UnitPanelManager.Age)newAgeIndex} �ô�� ���������� ��ο��� �����մϴ�.");

        // �� RPC�� ������ Ŭ���̾�Ʈ�� �ٷ� �ô� ������ �� ������� ��쿡�� UI�� ���� ����.
        // �̷��� �ؾ� P1�� ȭ�鿡���� P1�� UI��, P2�� ȭ�鿡���� P2�� UI�� �ٲ�
        if (targetPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            EvolveLocally(newAgeIndex);
        }
        else
        {
            // �ٸ� �÷��̾ �ô� ������ �� ��쿡 ���� ó�� (��: ���� ���� UI ���� ��)
            // ���� ������ Ư���� ó���� ������ ���� ���� ������, �ʿ�� �߰� ����
            Debug.Log($"�ٸ� �÷��̾�({targetPlayerActorNumber})�� �ô� ������ Ȯ���߽��ϴ�.");
        }
    }
    /// <summary>
    /// ���� ���� ���� ������ �����ϴ� �Լ� (UI ���� ��)
    /// </summary>
    private void EvolveLocally(int? newAgeIndex = null)
    {
        PHK.UnitPanelManager.Age evolvedAge;
        // RPC�� ���� Ư�� �ô�� �ٷ� �̵��ؾ� �ϴ� ���
        if (newAgeIndex.HasValue)
        {
            evolvedAge = (PHK.UnitPanelManager.Age)newAgeIndex.Value;
            unitPanelManager.SetAge(evolvedAge); // SetAge�� ���� ����
        }
        else // ������ ���� ���� (���� �ô�� ���� ����)
        {
            // [����] EvolveToNextAge()�� �ѹ��� ȣ���ϵ��� ����
            int currentAgeIndex = (int)unitPanelManager.currentAge;
            if (currentAgeIndex < expForEvolve.Length && currentEXP >= expForEvolve[currentAgeIndex])
            {
                unitPanelManager.EvolveToNextAge(); // ���⼭�� ȣ��
            }
            else
            {
                // ��ȭ ������ �ȵǸ� �ƹ��͵� ���� ���� (�Ǵ� �˸� �޽���)
                return; // �Լ��� �׳� ����
            }
            evolvedAge = unitPanelManager.currentAge;
        }

        unitPanelManager.evolveButton.interactable = false;
        // �ô� ������ Ȯ���� ��, ��ϵ� ��� �����ʿ��� �̺�Ʈ�� ����մϴ�.
        OnAgeEvolved?.Invoke(evolvedAge);
        CheckForAgeUp(); // ���� �ô� ���� ���� ���� ���� ��Ȯ��
    }

    ///<summary>
    ///����ġ�� ȹ���� ������ �ô� ������ �������� Ȯ�� �� ��ư Ȱ��ȭ�� ����
    /// </summary>
    private void CheckForAgeUp()
    {
        int currentAgeIndex = (int)unitPanelManager.currentAge;

        //���� �ô�� ���� ��ư���� ��Ȱ��ȭ ������ �� üũ
        if (currentAgeIndex < expForEvolve.Length && !unitPanelManager.evolveButton.interactable)
        {
            //���� ����ġ�� �ʿ� ����ġ �̻��̸� ���� ��ư Ȱ��ȭ
            if (currentEXP >= expForEvolve[currentAgeIndex])
            {
                unitPanelManager.evolveButton.interactable = true;
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


        InGameUIManager.Instance.UnitInfoText.text = $"Producing {unitPrefab.name}..."; 
        // ���� ������ ���� ������ �ƴ����� ���� ������ �и�
        if (isProducing)
        {
            // --- �̹� �ٸ� ������ ���� ���� ���: ��⿭(Queue)�� �߰� ---
            if (productionQueue.Count < 5) // ť�� 5ĭ
            {

                InGameUIManager.Instance.UnitInfoText.text = $"{unitPrefab.name} is Ready to Spawn...";
                Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
                if (SpendGold(unitData.goldCost))
                {
                    productionQueue.Enqueue((unitPrefab, ownerTag));
                    // ��⿭ UI ������Ʈ (���� ������� ���� ���� ����)
                    OnQueueChanged?.Invoke(productionQueue.Count);
                }
            }
            else
            {
                InGameUIManager.Instance.inGameInfoText.text = "Production Queue is Full!"; // ��⿭�� ���� á�� �� �˸�
            }
        }
        else
        {
            // --- ���� ������ ������� ���: �ٷ� ���� ���� ---
            Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
            if (SpendGold(unitData.goldCost))
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

       
        Vector3 initialMoveDirection = (ownerTag == "P1") ? Vector3.right : Vector3.left;
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
                int percent = (int)((timer / unitData.SpawnTime) * 100f);
                InGameUIManager.Instance.UnitInfoText.text = $" Spawning : {prefabToProduce.name}. . . .{percent}%";
                yield return null;
            }
        }
        OnProductionProgress?.Invoke(1f); // 100% ä���

        Transform spawnPoint = (ownerTag == "P1") ? p1_spawnPoint : p2_spawnPoint;
        if (spawnPoint != null)
        {
            // ����� ���� ���� ��Ʈ��ũ ȯ���� �б�
            if (isDebugMode)
            {
                // --- ����� ���: �Ϲ� Instantiate ��� ---
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
                // UnitController�� public ������ ���� �����Ͽ� ������ ����
                UnitController controller = newUnit.GetComponent<UnitController>();
                if (controller != null)
                {
                    controller.moveDirection = initialMoveDirection;
                }

            }
            else
            {
                // --- ���� ��Ʈ��ũ ȯ��: PhotonNetwork.Instantiate ��� ---
                object[] data = new object[] { ownerTag, initialMoveDirection };
                PhotonNetwork.Instantiate(prefabToProduce.name, spawnPoint.position, spawnPoint.rotation, 0, data);
            }
        }

        // --- �ļ� ó��: ��⿭�� ���� ������ �ִ��� Ȯ�� ---
        if (productionQueue.Count > 0)
        {
            var nextUnit = productionQueue.Dequeue();
            OnQueueChanged?.Invoke(productionQueue.Count);
            StartCoroutine(ProcessSingleUnit(nextUnit.prefab, nextUnit.ownerTag));
        }
        else
        {
            isProducing = false;
            OnProductionStatusChanged?.Invoke(false); // �����̴� ��Ȱ��ȭ
            OnProductionProgress?.Invoke(0f);         // �����̴� 0%�� ����
        }

        InGameUIManager.Instance.UnitInfoText.text = $"{prefabToProduce.name} has Spawned!!";
    }

    ///<summary>
    ///��� Ŭ���̾�Ʈ���� Ư�� ������ �±׸� ������
    /// </summary>
    [PunRPC]
    private void RPC_SetUnitTag(int viewID, string tag)
    {
        PhotonView targetPV = PhotonView.Find(viewID);
        if (targetPV != null)
        {
            targetPV.gameObject.tag = tag;
            // *** START: ��û�� ���� ���̾� ���� �߰� ***
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

    #region /// �ͷ� ���� ���� (������ �غ�, ������ ���� �� ���� ����) ///
    /// <summary>
    /// [UI ��ư �����] �ͷ� �Ǽ� ���� ����.
    /// </summary>
    /// <param name="turretPrefab">�Ǽ��� �ͷ��� ������. ��� ������ ��� ���� ���.</param>
    public void EnterTurretPlaceMode(GameObject turretPrefab)
    {
        currentState = PlayerActionState.PlacingTurret;
        turretPrefabToPlace = turretPrefab;
        Debug.Log($"<color=cyan>�Ǽ� ��� ����:</color> {turretPrefab.name}. �Ǽ��� ��ġ�� Ŭ���ϼ���.");
        InGameUIManager.Instance.inGameInfoText.text = "Click on a turret slot to build.";
        InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
    }

    /// <summary>
    /// [UI ��ư �����] �ͷ� �Ǹ� ���� ����. (�� �Լ��� ���� ����)
    /// </summary>
    public void EnterTurretSellMode()
    {
        currentState = PlayerActionState.SellingTurret;
        Debug.Log("<color=yellow>�Ǹ� ��� ����:</color> �Ǹ��� �ͷ��� Ŭ���ϼ���.");
        InGameUIManager.Instance.inGameInfoText.text = "Select a turret to sell.";
        InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
    }

    /// <summary>
    /// ��� �ൿ(�Ǽ�/�Ǹ�)�� ����ϰ� �⺻ ���·�. (�� �Լ��� ���� ����)
    /// </summary>
    public void CancelPlayerAction()
    {
        currentState = PlayerActionState.None;
        turretPrefabToPlace = null;
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.HideInfoText();
        }
        Debug.Log("�ൿ�� ��ҵǾ����ϴ�.");
    }


    /// <summary>
    /// Update �Լ����� ȣ��Ǿ� �ͷ� ���� �Է��� ó��.
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
    /// [UI ��ư �����] ���̽��� �ͷ��� �Ǽ��� �� �ִ� ���ο� ����(����)�� �߰��ϵ��� ��û�մϴ�.
    /// </summary>
    public void OnClick_RequestAddTurretSlot()
    {
        // ���⼭ '�ͷ� ����'�� �����ϴ� ����� �����մϴ�.
        int slotCost = 100; // ���� ���

        Debug.Log($"<color=green>�ͷ� ��ġ ���� �߰� �õ�.</color> �ʿ� ���: {slotCost}");

        // 1. InGameManager�� ����� ó���մϴ�.
        if (SpendGold(slotCost))
        {
            Debug.Log("��� ���� ����. ���̽� ��Ʈ�ѷ��� ���� ������ ��û�մϴ�.");
            // 2. ��� ���ҿ� �����ϸ�, '���̽� �����'�� ���� ��ũ��Ʈ�� ���� ������ ��û�մϴ�.
            // �� ������ ���� �÷��̾��� ���̽��� ã�� �ش� ���̽��� ��Ʈ�ѷ� ��ũ��Ʈ�� ȣ���ؾ� �մϴ�.

            /* --- �Ʒ��� ���̽� ����ڰ� ������ �����ڵ� �����Դϴ� --- */
            //
            // BaseController playerBase = GetPlayerBase(); // ���� �÷��̾��� ���̽��� ã�� �Լ� (���� �ʿ�)
            // if (playerBase != null)
            // {
            //     // ���̽� ��Ʈ�ѷ��� �ִ� 'CreateNewTurretSlot' �Լ��� ȣ���մϴ�.
            //     // �� �Լ��� ���̽� ���� ���ο� �ͷ� ��ġ ������ �ð������� �����ϰ� Ȱ��ȭ�ϴ� ������ �մϴ�.
            //     playerBase.CreateNewTurretSlot(); 
            // }
            // else
            // {
            //     Debug.LogError("���� �÷��̾��� ���̽��� ã�� �� �����ϴ�!");
            // }
            /* --- �����ڵ� ���� �� --- */

        }
        else
        {
            Debug.Log("��尡 �����Ͽ� �ͷ� ��ġ ������ �߰��� �� �����ϴ�.");
        }
    }


    // 'TryPlaceTurretOnBase' �Լ��� �̸��� 'TryPlaceTurretInSlot'���� �����ϰ� ������ �����մϴ�.
    // ���� ���̽��� �ƴ�, ���̽� ���� '����'�� Ŭ������ �� �����մϴ�.
    /// <summary>
    /// �ͷ��� '����' ���� �Ǽ��ϴ� ���� �õ��մϴ�.
    /// </summary>
    private void TryPlaceTurretInSlot()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 1. Ŭ���� ���� '�ͷ� ����'���� Ȯ���մϴ�. (����ڰ� TurretSlot ��ũ��Ʈ �Ǵ� �±׸� ������ ��)
            // var slot = hit.collider.GetComponent<TurretSlot>();
            // if (slot == null || slot.isOccupied) 
            // {
            //     Debug.Log("�ͷ��� ���� �� ���� �����̰ų� �̹� �ٸ� �ͷ��� �ֽ��ϴ�.");
            //     return; 
            // }

            // 2. �ش� ������ ���� �÷��̾��� ������ Ȯ�� (���̽� ����ڰ� ����)
            // if (!slot.IsOwnedByLocalPlayer()) return;

            // --- ���⼭���Ͱ� InGameManager�� �ٽ� ���� ---
            // 3. ��� �������� (�ͷ� �����տ� TurretController ���� ��ũ��Ʈ�� �ְ� ��� ������ �ִٰ� ����)
            // int turretCost = turretPrefabToPlace.GetComponent<TurretController>().goldCost;

            // 4. ��� ���� �õ�
            // if (SpendGold(turretCost))
            // {
            //    // 5. ��� ������ �����ϸ�, �ش� ���Կ� �ͷ��� ��ġ�϶�� '��û'
            //    Debug.Log($"��� ���� ����. {slot.name}�� �ͷ� ��ġ�� ��û�մϴ�.");
            //    // slot.PlaceTurret(turretPrefabToPlace); // ���� ��ġ�� ������ ���
            // }

            // 6. �۾��� �����ߵ� �����ߵ�, �Ǽ� ���� ����
            Debug.Log("�ͷ� ���� ����ڰ� ���� PlaceTurret �Լ��� ȣ���� �غ� �Ǿ����ϴ�.");
            CancelPlayerAction();
        }
    }

    /// <summary>
    /// '���̽�' ���� �ͷ��� �Ǹ��ϴ� ���� �õ�.
    /// </summary>
    private void TrySellTurretFromBase()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // �ͷ� ����ڰ� ���� ��ũ��Ʈ (��: TurretController)�� ������.
            // var turretController = hit.collider.GetComponent<TurretController>();
            // if (turretController == null) return; // �ͷ��� �ƴϸ� ����

            // �� Ŭ���̾�Ʈ�� �ش� �ͷ��� �������� Ȯ���ϴ� ���� (����ڰ� ����)
            // if (!turretController.IsOwnedByLocalPlayer())
            // {
            //     Debug.Log("�ڽ��� �ͷ��� �Ǹ��� �� �ֽ��ϴ�.");
            //     return;
            // }

            // --- ������Ͱ� InGameManager�� �ٽ� ���� ---
            // 1. �Ǹ� ���� ��������
            // int refund = turretController.sellPrice;

            // 2. ��� ��ȯ
            // AddGold(refund);

            // 3. ���̽� ��Ʈ�ѷ����� �ͷ��� ���ŵǾ����� �˸���, �ͷ� �ı� '��û'
            // turretController.GetOwnerBase().RemoveTurret(turretController.gameObject);

            // 4. �۾� �Ϸ� �� �Ǹ� ��� ����
            Debug.Log("�ͷ� �Ǹ� �� ��� ȯ�� �Լ��� ȣ���� �غ� �Ǿ����ϴ�.");
            CancelPlayerAction();
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
