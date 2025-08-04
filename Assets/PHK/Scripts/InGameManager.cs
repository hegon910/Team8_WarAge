using Firebase.Auth;
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
    private AgeType p1_currentAge = AgeType.Ancient;
    private AgeType p2_currentAge = AgeType.Ancient;

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

    #region �ʱ⼳�� �� Update()
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


        // ���� ���� �� �ô� ���� ��ư�� ��Ȱ��ȭ ���·� ����
        OnEvolveStatusChanged?.Invoke(false);
        StartCoroutine(PassiveGoldGeneration());
    }

    private void OnDestroy()
    {

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
    #endregion

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
        // ���� �÷��̾��� �� �±׿� �ô븦 �����ɴϴ�.
        string localPlayerTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");
        AgeType localPlayerAge = (localPlayerTag == "P1") ? p1_currentAge : p2_currentAge;

        // ���� ���� ���θ� Ȯ���մϴ�.
        if (ageManager.CanUpgrade(localPlayerAge, GetLocalPlayerExp()))
        {
            // ����� ����� ���
            if (isDebugMode)
            {
                Debug.Log($"����� ���: {localPlayerTag}�� �ô� ������ ���� �����մϴ�.");
                // isDebugHost�� ������� �ùٸ� �÷��̾� ActorNumber�� �ùķ��̼��Ͽ� RPC�� ȣ���մϴ�.
                int localActorNumber = isDebugHost ? 1 : 2;
                RPC_ConfirmEvolve(localActorNumber);
            }
            // ��Ʈ��ũ ����� ���
            else
            {
                photonView.RPC("RPC_RequestEvolve", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
        else
        {
            Debug.Log("�ô� ���� ����: ����ġ ����");
        }
    }

    [PunRPC]
    private void RPC_RequestEvolve(int requestingPlayerActorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player requestingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(requestingPlayerActorNumber);
        if (requestingPlayer == null) return;

        string teamTag = (requestingPlayer.ActorNumber == 1) ? "P1" : "P2";
        AgeType playerAge = (teamTag == "P1") ? p1_currentAge : p2_currentAge;

        int actualExp = 0;
        if (requestingPlayer.CustomProperties.TryGetValue(PLAYER_EXP_KEY, out object expValue))
        {
            actualExp = (int)expValue;
        }

        // ������ Ŭ���̾�Ʈ�� �ش� �÷��̾��� ���� �ô�� ����ġ�� �ٽ� �ѹ� ����
        if (ageManager.CanUpgrade(playerAge, actualExp))
        {
            photonView.RPC("RPC_ConfirmEvolve", RpcTarget.All, requestingPlayerActorNumber);
        }
        else
        {
            Debug.LogWarning($"�÷��̾� {requestingPlayer.NickName} �ô� ���� ���� (������ Ŭ���̾�Ʈ ����)");
        }
    }



    [PunRPC]
    private void RPC_ConfirmEvolve(int targetPlayerActorNumber)
    {
        Debug.Log($"{targetPlayerActorNumber}�� �÷��̾��� �ô� ���� Ȯ�� RPC ����");
        string teamTag = (targetPlayerActorNumber == 1) ? "P1" : "P2";

        // 1. InGameManager�� �ش� ���� �ô� ���¸� ���� ������Ʈ
        AgeData nextAgeData = null;
        if (teamTag == "P1")
        {
            nextAgeData = ageManager.GetNextAgeData(p1_currentAge);
            if (nextAgeData != null) p1_currentAge = nextAgeData.ageType;
        }
        else // teamTag == "P2"
        {
            nextAgeData = ageManager.GetNextAgeData(p2_currentAge);
            if (nextAgeData != null) p2_currentAge = nextAgeData.ageType;
        }

        if (nextAgeData == null)
        {
            Debug.LogError("���� �ô� �����͸� ã�� �� �����ϴ�!");
            return;
        }

        // 2. �ش��ϴ� ����(BaseController)�� ã�� �� ���� �Լ��� '����' ȣ��
        BaseController targetBase = (teamTag == "P1") ? p1_Base : p2_Base;
        if (targetBase != null)
        {
            targetBase.UpgradeBaseByAge(nextAgeData);
        }
        else
        {
            Debug.LogError($"{teamTag}�� ������ ã�� �� ���� ���� ������ �� �����ϴ�.");
        }

        // 3. UI ������Ʈ �� �ļ� ó�� (���� �÷��̾��� ��쿡��)
        string localPlayerTeamTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");

        // �ô� ������ �� �÷��̾ '��' �ڽ����� �� �±׷� Ȯ���մϴ�.
        if (teamTag == localPlayerTeamTag)
        {
            // ����ġ ���� �Ǵ� �ʱ�ȭ ������ �ʿ��ϴٸ� ���⿡ �߰�
            // ��: SpendExp(ageManager.GetRequiredExpForNextAge(...));

            // UI ������Ʈ
            unitPanelManager.UpdateAge(nextAgeData);
            OnAgeEvolved?.Invoke(nextAgeData);
            CheckForAgeUp(); // ���� ��ư ���� �ٽ� üũ

            Debug.Log($"���� �÷��̾�({localPlayerTeamTag})�� �ô밡 {nextAgeData.ageType}���� �����Ͽ� UI�� ������Ʈ�մϴ�.");
        }
    }
    //private void HandleAgeChanged(string teamtag, KYG.AgeData newAgeData)
    //{
    //    Debug.Log($"[InGameManager] AgeData ����. ���� ��: {newAgeData.spawnableUnits.Count}");
    //
    //    // UnitPanelManager�� UI ����̹Ƿ�, ���� �����ϱ⺸�� �̺�Ʈ�� ó���ϴ� ���� �̻����̳�,
    //    // ���� ������ AgeData�� ���� �����ؾ� �ϹǷ� �� �κ��� �����մϴ�.
    //    unitPanelManager.UpdateAge(newAgeData);
    //
    //    OnAgeEvolved?.Invoke(newAgeData);
    //    CheckForAgeUp();
    //}

    private void CheckForAgeUp()
    {
        // ���� �÷��̾��� ���� �ô븦 �������� ���� ���� ���� üũ
        string localTeamTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");
        AgeType localPlayerAge = (localTeamTag == "P1") ? p1_currentAge : p2_currentAge;
        bool canUpgrade = ageManager.CanUpgrade(localPlayerAge, GetLocalPlayerExp());
        OnEvolveStatusChanged?.Invoke(canUpgrade);

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
                // --- �� �κ��� �ٽ����� �������Դϴ� ---
                // 1. ���� �÷��̾�(��)�� �� �±׸� Ȯ���մϴ�.
                string localTeamTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");

                // 2. �� �±׿� �´� ���� �ô� ������ �����ɴϴ�.
                AgeType localPlayerAge = (localTeamTag == "P1") ? p1_currentAge : p2_currentAge;

                // 3. ���� �÷��̾��� ���� �ô븦 �������� ��� ���޷��� �����մϴ�.
                switch (localPlayerAge)
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

                    default:
                        goldToAdd = 0; // �ش��ϴ� �ô밡 ������ ���� ����
                        break;
                }
            }

            if (goldToAdd > 0)
            {
                AddGold(goldToAdd);
            }
        }
    }

    
    public BaseController GetLocalPlayerBase()
    {
        string myTeamTag = isDebugMode 
            ? (isDebugHost ? "BaseP1" : "BaseP2")
            : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "BaseP1" : "BaseP2");

        return myTeamTag == "BaseP1" ? p1_Base : p2_Base;
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
        // �й��� ���� �ڽ��� ���� ���Ͽ� ��/�� �̺�Ʈ ȣ�� �� ���� ��� ��ȣ ����
        if (myTeamTag == losingTeamTag)
        {
            OnGameLost?.Invoke();
            SendMatchResult(false); // �й� ��� ����
        }
        else
        {
            OnGameWon?.Invoke();
            SendMatchResult(true); // �¸� ��� ����
        }
    }

    /// <summary>
    /// ���� ����� ����ϱ� ���� ��Ʈ��ũ ���񽺿� ��ȣ
    /// �� �Լ��� �� Ŭ���̾�Ʈ(�÷��̾�)���� �ڽ��� ����� ���� �� ���� ȣ��
    /// </summary>
    /// <param name="isWinner">���� �÷��̾��� �¸� ����.</param>
    private void SendMatchResult(bool isWinner)
    {
        // ��Ʈ��ũ ����ڰ� ���� ��� �ý����� ����
        // ����� �÷��̾� ������ ���� ����� �α׷� ���
        FirebaseUser user = UserAuthService.Auth?.CurrentUser;
        if (user == null)
        {
            Debug.LogError("���� ��� ����: ���� �α��ε� Firebase ������ �����ϴ�.");
            return;
        }

        string resultLog = isWinner ? "�¸�" : "�й�";
        Debug.Log($"[���� ��� ��ȣ] �÷��̾�: {user.DisplayName} ({user.UserId}), ���: {resultLog}");

        // TODO: ��Ʈ��ũ ����ڴ� �� ��ȣ�� �޾� DB�� ������ ���
        // ���� ������ ������Ʈ�ϴ� ������ ����

    }
    
}
