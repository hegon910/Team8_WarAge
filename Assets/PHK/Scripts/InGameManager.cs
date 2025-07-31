using KYG;
using Photon.Pun;
using Photon.Realtime;
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

    private const string PLAYER_EXP_KEY = "PlayerExp"; // Photon Custom Properties Ű

    // ���� ���� ���� ����
    private int currentGold;
    //private int currentEXP; // ���� ���� ��� Photon Custom Properties ���
    private PhotonView photonView;
    private string teamTag;
    private bool isGameOver = false;

    // --- �̺�Ʈ ---
    public event Action<KYG.AgeData> OnAgeEvolved;
    public event Action<int, int> OnResourceChanged;
    public event Action<int, int> OnPlayerBaseHealthChanged;    // P1(�� ����)�� �̺�Ʈ
    public event Action<int, int> OnOpponentBaseHealthChanged;  // P2(��� ����)�� �̺�Ʈ
    public event Action<string> OnInfoMessage; // UI�� �Ϲ� �޽����� �����ϱ� ���� �̺�Ʈ
    public event Action<bool> OnEvolveStatusChanged; // �ô� ���� ���� ���� ���� �̺�Ʈ
    public event Action OnGameWon;
    public event Action OnGameLost;
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
        // �÷��̾� Ŀ���� ������Ƽ �ʱ�ȭ (������ 0���� ����)
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PLAYER_EXP_KEY))
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, 0 } });
        }
        else
        {
            // �̹� Ű�� �ִٸ� ��������� 0���� �ʱ�ȭ
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, 0 } });
        }
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


        // �ʱ� ��� �� ����ġ ������Ʈ (����ġ�� CustomProperties���� ������)
        OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp());
        OnInfoMessage?.Invoke("Game Started!");

        if (ageManager != null)
        {
            ageManager.OnAgeChangedByTeam += HandleAgeChanged;
        }
        else
        {
            Debug.LogError("AgeManager�� �Ҵ���� �ʾҽ��ϴ�! InGameManager�� Inspector���� �Ҵ����ּ���.");
        }
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

    // �÷��̾� Ŀ���� ������Ƽ ������Ʈ �ݹ� ������
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // �÷��̾��� ����ġ ������Ƽ�� ����Ǿ��� ��
        if (changedProps.ContainsKey(PLAYER_EXP_KEY))
        {
            int updatedExp = (int)changedProps[PLAYER_EXP_KEY];

            if (targetPlayer == PhotonNetwork.LocalPlayer)
            {
                // �� ����ġ�� ����� ��� UI ������Ʈ �� �ô� ���� üũ
                OnResourceChanged?.Invoke(currentGold, updatedExp); // ���� �Բ� ����ġ UI ������Ʈ
                CheckForAgeUp();
                Debug.Log($"�� ����ġ ������Ʈ: {updatedExp}");
            }
            else
            {
                // �ٸ� �÷��̾��� ����ġ ������ ���� InGameManager���� ���� ó������ ���� (�ʿ� �� �߰�)
                Debug.Log($"�ٸ� �÷��̾�({targetPlayer.NickName})�� ����ġ ������Ʈ: {updatedExp}");
            }
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
            // 'G' Ű�� ����ġ �߰� �� ���� �÷��̾��� ActorNumber ���
            AddExp(this.teamTag, 500); // �׽�Ʈ�� ����ġ �߰�
        }
    }

    #region �ڿ� �� ü�� ���� �Լ�

    public int GetLocalPlayerExp() // ���� �÷��̾��� ����ġ�� CustomProperties���� ������
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PLAYER_EXP_KEY, out object expValue))
        {
            return (int)expValue;
        }
        return 0;
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp()); // ����ġ UI�� �Բ� ������Ʈ
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp()); // ����ġ UI�� �Բ� ������Ʈ
            return true;
        }
        return false;
    }
    /// <summary>
    /// Ư�� �÷��̾��� ����ġ�� �߰��ϰ� ����ȭ.
    /// �� �Լ��� �ش� ����ġ ȹ���� ��ü�� �Ǵ� Ŭ���̾�Ʈ���� ȣ��Ǿ�� ��.
    /// ��: ���� óġ �� �ش� ������ óġ�� �÷��̾� Ŭ���̾�Ʈ���� ȣ��
    /// </summary>
    /// <param name="targetPlayerActorNumber">����ġ�� ȹ���� �÷��̾��� ActorNumber.</param>
    /// <param name="amount">�߰��� ����ġ ��.</param>
    public void AddExp(string targetTeamTag, int amount)
    {
        // ��Ʈ��ũ ��忡���� RPC�� ���� ����ġ ������Ʈ ��û
        if (!isDebugMode)
        {
            // ������ Ŭ���̾�Ʈ���� ����ġ ������Ʈ ��û
            photonView.RPC("RPC_AddExp", RpcTarget.MasterClient, targetTeamTag, amount);
            Debug.Log($"����ġ �߰� ��û: �� {targetTeamTag}, ��: {amount}");
        }
        else // ����� ��忡���� ���ÿ��� ���� ó��
        {
            // ����� ��忡���� ��û�� ���� ���� �÷��̾��� ���� ���� ���� ����ġ�� �߰��մϴ�.
            string myTeamTag = isDebugHost ? "P1" : "P2";
            if (myTeamTag == targetTeamTag)
            {
                int newExp = GetLocalPlayerExp() + amount;
                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, newExp } });
                Debug.Log($"[DebugMode] �� {targetTeamTag}�� ����ġ {amount} �߰�. ���� ����ġ: {newExp}");
            }
        }
    }

    [PunRPC]
    private void RPC_AddExp(string targetTeamTag, int amount, PhotonMessageInfo info)
    {
        // ������ Ŭ���̾�Ʈ�� �� RPC�� �����մϴ�.
        if (!PhotonNetwork.IsMasterClient) return;

        // �� �±׸� ������� �÷��̾��� ActorNumber�� �����մϴ�. (P1=1, P2=2)
        int targetPlayerActorNumber = (targetTeamTag == "P1") ? 1 : 2;

        Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(targetPlayerActorNumber);
        if (targetPlayer == null)
        {
            Debug.LogError($"RPC_AddExp: ��� �÷��̾� (��: {targetTeamTag}, ActorNumber: {targetPlayerActorNumber})�� ã�� �� �����ϴ�.");
            return;
        }

        // ���� ����ġ�� �����ͼ� ������Ʈ.
        int currentExp = 0;
        if (targetPlayer.CustomProperties.TryGetValue(PLAYER_EXP_KEY, out object expValue))
        {
            currentExp = (int)expValue;
        }

        int newExp = currentExp + amount;

        // ����ġ ������Ʈ (�� �ڵ尡 ����Ǹ� ��� Ŭ���̾�Ʈ�� OnPlayerPropertiesUpdate�� ȣ��˴ϴ�)
        targetPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, newExp } });

        Debug.Log($"������ Ŭ���̾�Ʈ: �� {targetTeamTag}���� {amount} ����ġ �߰�, �� ����ġ: {newExp}");
    }
    private void HandleP1BaseHpChanged(int currentHp, int maxHp)
    {
        // P1 ���� ü���� �ٲ�� P1�� �̺�Ʈ�� �߻���Ŵ
        OnPlayerBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0 && PhotonNetwork.IsMasterClient)
        {
            GameOver("P1");
        }
    }
    private void HandleP2BaseHpChanged(int currentHp, int maxHp)
    {
        // P2 ���� ü���� �ٲ�� P2�� �̺�Ʈ�� �߻���Ŵ
        OnOpponentBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0 && PhotonNetwork.IsMasterClient)
        {
            GameOver("P2");

        }
    }
    #endregion

    #region �ô� ���� ����
    public void AttemptEvolve()
    {
        if (isDebugMode)
        {
            // ����� ��忡���� ���� �÷��̾��� ����ġ�� ����Ͽ� ���� �ô븦 ����
            string debugTeamTag = isDebugHost ? "P1" : "P2"; // ����� ȣ��Ʈ ���η� �� ����
            ageManager.TryUpgradeAge(debugTeamTag, GetLocalPlayerExp());
        }
        else
        {
            photonView.RPC("RPC_RequestEvolve", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, GetLocalPlayerExp()); // ����ġ�� �Բ� ����
            Debug.Log("������ Ŭ���̾�Ʈ���� �ô� ������ ��û�մϴ�.");
        }
    }

    [PunRPC]
    private void RPC_RequestEvolve(int requestingPlayerActorNumber, int playerExp, PhotonMessageInfo info) // playerExp �Ű����� �߰�
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"{requestingPlayerActorNumber}�� �÷��̾��� �ô� ���� ��û�� ���� (���� ����ġ: {playerExp})");

        Player requestingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(requestingPlayerActorNumber);
        if (requestingPlayer == null)
        {
            Debug.LogError($"RPC_RequestEvolve: ��û �÷��̾� (ActorNumber: {requestingPlayerActorNumber})�� ã�� �� �����ϴ�.");
            return;
        }

        // ������ Ŭ���̾�Ʈ���� �ش� �÷��̾��� ���� ����ġ�� �ô� ���� ������ �ٽ� ����
        // �÷��̾��� CustomProperties���� �ֽ� ����ġ�� �����ɴϴ�.
        int actualExp = 0;
        if (requestingPlayer.CustomProperties.TryGetValue(PLAYER_EXP_KEY, out object expValue))
        {
            actualExp = (int)expValue;
        }

        // �ô� ���� ���� ���� ��Ȯ��
        bool canUpgrade = ageManager.CanUpgrade(actualExp); // ���� ����ġ�� ����

        if (canUpgrade)
        {
            // �ô� ���� ���� ��
            photonView.RPC("RPC_ConfirmEvolve", RpcTarget.All, requestingPlayerActorNumber);
            // �ô� ���� �� ����ġ�� �ʱ�ȭ�ؾ� �Ѵٸ� ���⼭ �ʱ�ȭ ���� �߰�
            // ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
            // playerProps[PLAYER_EXP_KEY] = 0; // ��: ����ġ �ʱ�ȭ
            // requestingPlayer.SetCustomProperties(playerProps);
        }
        else
        {
            // �ô� ���� ���� �� (��: ����ġ ����)
            Debug.LogWarning($"�÷��̾� {requestingPlayerActorNumber} �ô� ���� ����: ����ġ ���� ({actualExp} / {ageManager.GetRequiredExpForNextAge()})");
            // ���� �޽����� �ش� �÷��̾�Ը� �����ϰų� ó���ϴ� ���� �߰� ����
        }
    }

    [PunRPC]
    private void RPC_ConfirmEvolve(int targetPlayerActorNumber)
    {
        Debug.Log($"{targetPlayerActorNumber}�� �÷��̾��� �ô� ���� Ȯ�� RPC ����");
        string teamTag = (targetPlayerActorNumber == 1) ? "P1" : "P2";
        if (targetPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            ageManager.TryUpgradeAge(teamTag, GetLocalPlayerExp()); // ���� �÷��̾� ����ġ�� �ô� ���� �õ�

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
        bool canUpgrade = ageManager.CanUpgrade(GetLocalPlayerExp()); // ���� �÷��̾� ����ġ�� üũ
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
    private void GameOver(string losingTeamTag)
    {
        if (isGameOver) return; // ���ӿ����� �̹� ó���Ǿ��ٸ� �ߺ� ���� ����
        isGameOver = true;

        Debug.Log($"Game Over. Losing team: {losingTeamTag}. MasterClient will send RPC.");
        // ��� Ŭ���̾�Ʈ���� ���� ��� RPC�� ����
        photonView.RPC("RPC_ShowResultPanels", RpcTarget.All, losingTeamTag);
    }

    [PunRPC]
    private void RPC_ShowResultPanels(string losingTeamTag)
    {
        // ��� Ŭ���̾�Ʈ���� ���� �ð��� ����
        Time.timeScale = 0f;
        OnInfoMessage?.Invoke("GAME OVER"); // ���� GameOver �޽��� ǥ��

        // ����� ���� ��Ʈ��ũ ��忡 ���� �ڽ��� �� �±׸� ����
        string myTeamTag;
        if (isDebugMode)
        {
            myTeamTag = isDebugHost ? "P1" : "P2";
        }
        else
        {
            myTeamTag = PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2";
        }

        // �й��� ���� �ڽ��� ���� ���Ͽ� ��/�� �̺�Ʈ ȣ��
        if (myTeamTag == losingTeamTag)
        {
            OnGameLost?.Invoke();
            Debug.Log("Result: You Lost");
        }
        else
        {
            OnGameWon?.Invoke();
            Debug.Log("Result: You Won");
        }
    }
}