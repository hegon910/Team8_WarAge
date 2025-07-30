using KYG;
using Photon.Pun;
using System;
using System.Collections;
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
    public BaseController p1_Base { get; private set; }
    public BaseController p2_Base { get; private set; }



    [Header("���� �⺻ ����")]
    [SerializeField] private int startingGold = 175;

    // ���� ���� ���� ����
    private int currentGold;
    private int currentEXP;
    private PhotonView photonView;
    private string teamTag;

    // --- �̺�Ʈ ---
    public event Action<KYG.AgeData> OnAgeEvolved;
    public event Action<int, int> OnResourceChanged;
    public event Action<int, int> OnPlayerBaseHealthChanged;    // P1(�� ����)�� �̺�Ʈ
    public event Action<int, int> OnOpponentBaseHealthChanged;  // P2(��� ����)�� �̺�Ʈ
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
    public void RegisterBase(BaseController baseController, string team)
    {
        if (team == "BaseP1")
        {
            p1_Base = baseController;
            // �̺�Ʈ ������ ���⼭ ���� �մϴ�.
            if (p1_Base != null) p1_Base.OnHpChanged += HandleP1BaseHpChanged;
            Debug.Log("P1 Base�� InGameManager�� ��ϵǾ����ϴ�.");
        }
        else if (team == "BaseP2")
        {
            p2_Base = baseController;
            // �̺�Ʈ ������ ���⼭ ���� �մϴ�.
            if (p2_Base != null) p2_Base.OnHpChanged += HandleP2BaseHpChanged;
            Debug.Log("P2 Base�� InGameManager�� ��ϵǾ����ϴ�.");
        }
    }

    private void Start()
    {
        // ���� ���� �ʱ�ȭ
        currentGold = startingGold;
        currentEXP = 0;
        if (p1_Base != null) p1_Base.InitializeTeam("P1");
        if (p2_Base != null) p2_Base.InitializeTeam("P2");
        if (isDebugMode)
    {
        teamTag = isDebugHost ? "P1" : "P2";
    }
    else
    {
        teamTag = PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2";
    }


        // �ʱ� ���� ���¸� �̺�Ʈ�� UI�� �ݿ�
        OnResourceChanged?.Invoke(currentGold, currentEXP);
        OnInfoMessage?.Invoke("Game Started!");

        if (ageManager != null)
        {
            ageManager.OnAgeChangedByTeam += HandleAgeChanged;
        }
        else
        {
            Debug.LogError("AgeManager�� �Ҵ���� �ʾҽ��ϴ�! InGameManager�� Inspector���� �Ҵ����ּ���.");
        }
      //  if (p1_Base != null)
      //  {
      //      // p1_Base�� �̺�Ʈ�� P1�� �ڵ鷯�� ����
      //      p1_Base.OnHpChanged += HandleP1BaseHpChanged;
      //  }
      //  if (p2_Base != null)
      //  {
      //      // p2_Base�� �̺�Ʈ�� P2�� �ڵ鷯�� ����
      //      p2_Base.OnHpChanged += HandleP2BaseHpChanged;
      //  }
        // ���� ���� �� �ô� ���� ��ư�� ��Ȱ��ȭ ���·� ����
        OnEvolveStatusChanged?.Invoke(false);
        StartCoroutine(PassiveGoldGeneration());
    }

    private void OnDestroy()
    {
        if (ageManager != null)
        {
            ageManager.OnAgeChangedByTeam -= HandleAgeChanged;
        }
        if (p1_Base != null)
        {
            p1_Base.OnHpChanged -= HandleP1BaseHpChanged;
        }
        if (p2_Base != null)
        {
            p2_Base.OnHpChanged -= HandleP2BaseHpChanged;
        }
    
}

    private void Update()
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
    private void HandleP1BaseHpChanged(int currentHp, int maxHp)
    {
        // P1 ���� ü���� �ٲ�� P1�� �̺�Ʈ�� �߻���Ŵ
        OnPlayerBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0) GameOver();
    }
    private void HandleP2BaseHpChanged(int currentHp, int maxHp)
    {
        // P2 ���� ü���� �ٲ�� P2�� �̺�Ʈ�� �߻���Ŵ
        OnOpponentBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0) GameOver();
    }

    #endregion

    #region �ô� ���� ����
    public void AttemptEvolve()
    {
        if (isDebugMode)
        {
            ageManager.TryUpgradeAge(teamTag, currentEXP);
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
        string teamTag = (targetPlayerActorNumber == 1) ? "P1" : "P2";
        if (targetPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            ageManager.TryUpgradeAge(teamTag, currentEXP);
        }
        else
        {
            Debug.Log($"�ٸ� �÷��̾�({targetPlayerActorNumber}, ��: {teamTag})�� �ô� ������ Ȯ���߽��ϴ�.");
        }
    }

    private void HandleAgeChanged(string teamtag, KYG.AgeData newAgeData)
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
    private IEnumerator PassiveGoldGeneration()
    {
        // 5�� ��� �ð��� �̸� �����θ� ���ʿ��� �޸� �Ҵ��� ���� �� �ֽ��ϴ�.
        var fiveSecondWait = new WaitForSeconds(5f);

        while (true) // ������ ���� ������ ���� �ݺ�
        {
            yield return fiveSecondWait; // 5�ʰ� ���

            int goldToAdd = 0;

            // ageManager�� �Ҵ�Ǿ� �ִ��� Ȯ��
            if (ageManager != null)
            {
                // ���� �ô뿡 ���� ������ ��差�� �����մϴ�.
                // (AgeType �̸��� ���� ������Ʈ�� enum �̸��� �°� �����ؾ� �� �� �ֽ��ϴ�)
                switch (ageManager.CurrentAge)
                {
                    case AgeType.Ancient:
                        goldToAdd = 15;
                        break;

                    case AgeType.Medieval: // �߼� �ô밡 �ִٸ�
                        goldToAdd = 40;
                        break;

                    case AgeType.Modern: // ���� �ô밡 �ִٸ�
                        goldToAdd = 100;
                        break;

                    // �ʿ��ϴٸ� �ٸ� �ô뿡 ���� case �߰�
                    // ...

                    default:
                        goldToAdd = 0; // �ش��ϴ� �ô밡 ������ ���� ����
                        break;
                }
            }

            if (goldToAdd > 0)
            {
                AddGold(goldToAdd);
                // OnInfoMessage?.Invoke($"+{goldToAdd} Gold"); // ��� ȹ���� �˸��� �޽��� (���� ����)
            }
        }
    }
    private void GameOver()
    {
        Debug.Log("���� ���� ó�� �߰� �۾� �ʿ�");
        OnInfoMessage?.Invoke("GAME OVER");
        Time.timeScale = 0f;
    }
}
