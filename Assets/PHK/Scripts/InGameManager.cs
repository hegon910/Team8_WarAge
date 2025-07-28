using KYG;
using Photon.Pun;
using System;
using UnityEngine;

/// <summary>
/// ������ �ٽ� ��Ģ(�ڿ�, ü��, �ô�)�� ���¸� �����ϴ� �̱��� �Ŵ��� Ŭ����.
/// UI�� ���� �������� �ʰ�, ���� ���� �� �̺�Ʈ�� ���� �ܺο� �˸��ϴ�.
/// </summary>
public class InGameManager : MonoBehaviourPunCallbacks
{
    #region ����
    public static InGameManager Instance { get; private set; }

    [Header("����� �ɼ�")]
    public bool isDebugMode = false;
    public bool isDebugHost = true;

    [Header("�������")]
    public KYG.AgeManager ageManager;
    public PHK.UnitPanelManager unitPanelManager; // �ô� ���� ��ư ���� ������ ���� ���� ����
    public KYG.BaseController p1_Base;
    public KYG.BaseController p2_Base;

    [Header("���� �⺻ ����")]
    [SerializeField] private int startingGold = 175;
    [SerializeField] private int maxBaseHealth = 1000;

    // ���� ���� ���� ����
    private int currentGold;
    private int currentEXP;
    private int currentBaseHealth;
    private PhotonView photonView;

    // --- �̺�Ʈ ---
    public event Action<KYG.AgeData> OnAgeEvolved;
    public event Action<int, int> OnResourceChanged;
    public event Action<int, int> OnBaseHealthChanged;
    public event Action<string> OnInfoMessage; // UI�� �Ϲ� �޽����� �����ϱ� ���� �̺�Ʈ
    public event Action<bool> OnEvolveStatusChanged; // �ô� ���� ���� ���� ���� �̺�Ʈ
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
        // ���� ���� �ʱ�ȭ
        currentGold = startingGold;
        currentEXP = 0;
        currentBaseHealth = maxBaseHealth;

        // �ʱ� ���� ���¸� �̺�Ʈ�� UI�� �ݿ�
        OnResourceChanged?.Invoke(currentGold, currentEXP);
        OnBaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);
        OnInfoMessage?.Invoke("Game Started!");

        if (ageManager != null)
        {
            ageManager.OnAgeChanged += HandleAgeChanged;
        }
        else
        {
            Debug.LogError("AgeManager�� �Ҵ���� �ʾҽ��ϴ�! InGameManager�� Inspector���� �Ҵ����ּ���.");
        }

        // ���� ���� �� �ô� ���� ��ư�� ��Ȱ��ȭ ���·� ����
        OnEvolveStatusChanged?.Invoke(false);
    }

    private void OnDestroy()
    {
        if (ageManager != null)
        {
            ageManager.OnAgeChanged -= HandleAgeChanged;
        }
    }

    private void Update()
    {
        if (isDebugMode)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddGold(50); // �׽�Ʈ�� ��� �߰�
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                AddExp(500); // �׽�Ʈ�� ����ġ �߰�
            }
        }
    }

    #region �ڿ� �� ü�� ���� �Լ�
    public void AddGold(int amount)
    {
        currentGold += amount;
        OnResourceChanged?.Invoke(currentGold, currentEXP);
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            OnResourceChanged?.Invoke(currentGold, currentEXP);
            return true;
        }
        return false;
    }

    public void AddExp(int amount)
    {
        if (ageManager.GetNextAgeData() != null)
        {
            currentEXP += amount;
            OnResourceChanged?.Invoke(currentGold, currentEXP);
            CheckForAgeUp();
            Debug.Log($"{amount} ����ġ ȹ��. ���� ����ġ : {currentEXP}");
        }
    }

    public void TakeBaseDamage(int damage)
    {
        currentBaseHealth -= damage;
        currentBaseHealth = Mathf.Max(currentBaseHealth, 0);
        OnBaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);

        if (currentBaseHealth <= 0) GameOver();
    }
    #endregion

    #region �ô� ���� ����
    public void AttemptEvolve()
    {
        if (isDebugMode)
        {
            ageManager.TryUpgradeAge(currentEXP);
        }
        else
        {
            photonView.RPC("RPC_RequestEvolve", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log("������ Ŭ���̾�Ʈ���� �ô� ������ ��û�մϴ�.");
        }
    }

    [PunRPC]
    private void RPC_RequestEvolve(int requestingPlayerActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Debug.Log($"{requestingPlayerActorNumber}�� �÷��̾��� �ô� ���� ��û�� ���� �� ���� (����� �ڵ� ���)");
        photonView.RPC("RPC_ConfirmEvolve", RpcTarget.All, requestingPlayerActorNumber);
    }

    [PunRPC]
    private void RPC_ConfirmEvolve(int targetPlayerActorNumber)
    {
        Debug.Log($"{targetPlayerActorNumber}�� �÷��̾��� �ô� ���� Ȯ�� RPC ����");
        if (targetPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            ageManager.TryUpgradeAge(currentEXP);
        }
        else
        {
            Debug.Log($"�ٸ� �÷��̾�({targetPlayerActorNumber})�� �ô� ������ Ȯ���߽��ϴ�.");
        }
    }

    private void HandleAgeChanged(KYG.AgeData newAgeData)
    {
        Debug.Log($"[InGameManager] AgeData ����. ���� ��: {newAgeData.spawnableUnits.Count}");

        // UnitPanelManager�� UI ����̹Ƿ�, ���� �����ϱ⺸�� �̺�Ʈ�� ó���ϴ� ���� �̻����̳�,
        // ���� ������ AgeData�� ���� �����ؾ� �ϹǷ� �� �κ��� �����մϴ�.
        unitPanelManager.UpdateAge(newAgeData);

        OnAgeEvolved?.Invoke(newAgeData);
        CheckForAgeUp();
    }

    private void CheckForAgeUp()
    {
        bool canUpgrade = ageManager.CanUpgrade(currentEXP);
        OnEvolveStatusChanged?.Invoke(canUpgrade); // �ô� ���� ���� ���θ� �̺�Ʈ�� �˸�

        if (canUpgrade)
        {
            OnInfoMessage?.Invoke("You can evolve to the next age!");
        }
    }
    #endregion

    private void GameOver()
    {
        Debug.Log("���� ���� ó�� �߰� �۾� �ʿ�");
        OnInfoMessage?.Invoke("GAME OVER");
        Time.timeScale = 0f;
    }
}
