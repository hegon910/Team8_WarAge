using Firebase.Auth;
using KYG;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;

public class InGameManager : MonoBehaviourPunCallbacks
{
    #region 변수
    public static InGameManager Instance { get; private set; }

    [Header("디버그 옵션")]
    public bool isDebugMode = false;
    public bool isDebugHost = true;

    [Header("참조")]
    public KYG.AgeManager ageManager;
    public PHK.UnitPanelManager unitPanelManager;
    public BaseController p1_Base { get; private set; }
    public BaseController p2_Base { get; private set; }


    [Header("게임 기본 설정")]
    [SerializeField] private int startingGold = 175;

    private const string PLAYER_EXP_KEY = "PlayerExp";

    // 게임 상태 관련 변수
    public int currentGold { get; private set; }
    private int p1_exp_debug; // 디버그 모드에서 사용할 플레이어(P1)의 경험치
    private PhotonView photonView;
    private string teamTag;
    private bool isGameOver = false;
    private AgeType p1_currentAge = AgeType.Ancient;
    private AgeType p2_currentAge = AgeType.Ancient;

    // --- 이벤트 ---
    public event Action<KYG.AgeData> OnAgeEvolved;
    public event Action<int, int> OnResourceChanged;
    public event Action<int, int> OnPlayerBaseHealthChanged;
    public event Action<int, int> OnOpponentBaseHealthChanged;
    public event Action<string> OnInfoMessage;
    public event Action<bool> OnEvolveStatusChanged;
    public event Action OnGameWon;
    public event Action OnGameLost;
    public event Action<float> OnUltimateSkillUsed;
    #endregion

    #region 초기화 및 Update()
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
            if (p1_Base != null) p1_Base.OnHpChanged += HandleP1BaseHpChanged;
        }
        else if (team == "BaseP2")
        {
            p2_Base = baseController;
            if (p2_Base != null) p2_Base.OnHpChanged += HandleP2BaseHpChanged;
        }
    }

    private void Start()
    {
        currentGold = startingGold;
        if (isDebugMode)
        {
            p1_exp_debug = 0; // 디버그 모드에서는 로컬 변수로 경험치 관리
            teamTag = isDebugHost ? "P1" : "P2";
        }
        else
        {
            // 네트워크 모드에서는 Photon Custom Properties 사용
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, 0 } });
            teamTag = PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2";
        }

        if (p1_Base != null)
        {
            p1_Base.InitializeTeam("P1");
            if (p1_Base.turretSlots != null)
            {
                foreach (var slot in p1_Base.turretSlots)
                {
                    slot.Init("BaseP1");
                }
            }
        }

        if (p2_Base != null)
        {
            p2_Base.InitializeTeam("P2");

            if (p2_Base.turretSlots != null)
            {
                foreach (var slot in p2_Base.turretSlots)
                {
                    slot.Init("BaseP2");
                }
            }
        }

        OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp());
        OnInfoMessage?.Invoke("게임 시작!");

        OnEvolveStatusChanged?.Invoke(false);
        StartCoroutine(PassiveGoldGeneration());
    }

    private void OnDestroy()
    {
        if (p1_Base != null) p1_Base.OnHpChanged -= HandleP1BaseHpChanged;
        if (p2_Base != null) p2_Base.OnHpChanged -= HandleP2BaseHpChanged;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PLAYER_EXP_KEY))
        {
            int updatedExp = (int)changedProps[PLAYER_EXP_KEY];
            if (targetPlayer == PhotonNetwork.LocalPlayer)
            {
                OnResourceChanged?.Invoke(currentGold, updatedExp);
                CheckForAgeUp();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) AddGold(50);
        if (Input.GetKeyDown(KeyCode.G)) AddExp("P1", 500); // 디버그 시 P1에게 경험치
    }
    #endregion

    #region 자원 및 체력 관리 함수

    public int GetLocalPlayerExp()
    {
        if (isDebugMode) return p1_exp_debug;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PLAYER_EXP_KEY, out object expValue))
        {
            return (int)expValue;
        }
        return 0;
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp());
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp());
            return true;
        }
        return false;
    }

    public void AddExp(string targetTeamTag, int amount)
    {
        if (isDebugMode)
        {
            if (targetTeamTag == "P1")
            {
                p1_exp_debug += amount;
                OnResourceChanged?.Invoke(currentGold, p1_exp_debug);
                CheckForAgeUp();
            }
            else if (targetTeamTag == "P2" && AIController.Instance != null)
            {
                AIController.Instance.AddExp(amount);
            }
            return;
        }

        photonView.RPC("RPC_AddExp", RpcTarget.MasterClient, targetTeamTag, amount);
    }

    [PunRPC]
    private void RPC_AddExp(string targetTeamTag, int amount, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int targetPlayerActorNumber = (targetTeamTag == "P1") ? 1 : 2;
        Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(targetPlayerActorNumber);
        if (targetPlayer == null) return;

        int currentExp = 0;
        if (targetPlayer.CustomProperties.TryGetValue(PLAYER_EXP_KEY, out object expValue))
        {
            currentExp = (int)expValue;
        }
        int newExp = currentExp + amount;
        targetPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, newExp } });
    }
    private void HandleP1BaseHpChanged(int currentHp, int maxHp)
    {
        OnPlayerBaseHealthChanged?.Invoke(currentHp, maxHp);

        bool canGameOver = isDebugMode || PhotonNetwork.IsMasterClient;
        if (currentHp <= 0 && canGameOver) GameOver("P1");
    }
    private void HandleP2BaseHpChanged(int currentHp, int maxHp)
    {
        OnOpponentBaseHealthChanged?.Invoke(currentHp, maxHp);

        bool canGameOver = isDebugMode || PhotonNetwork.IsMasterClient;
        if (currentHp <= 0 && canGameOver) GameOver("P2");
    }
    #endregion

    #region 시대 진화 관리
    public void AttemptEvolve()
    {
        string localPlayerTag = isDebugMode ? "P1" : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");

        // 디버그 모드에서는 P1(플레이어)만 이 버튼을 통해 진화할 수 있습니다.
        if (isDebugMode && !isDebugHost) return;

        AgeType currentAge = (localPlayerTag == "P1") ? p1_currentAge : p2_currentAge;
        int requiredExp = ageManager.GetRequiredExpForNextAge(currentAge);

        if (ageManager.CanUpgrade(currentAge, GetLocalPlayerExp()))
        {
            // 경험치 소모
            if (isDebugMode)
            {
                p1_exp_debug -= requiredExp;
            }
            else
            {
                int currentExp = GetLocalPlayerExp();
                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, currentExp - requiredExp } });
            }

            // 진화 실행
            if (isDebugMode)
            {
                EvolveTeam(localPlayerTag);
            }
            else
            {
                photonView.RPC("RPC_EvolveTeam", RpcTarget.All, localPlayerTag);
            }
        }
        else
        {
            OnInfoMessage?.Invoke("시대 진화 실패: 경험치 부족!");
        }
    }

    [PunRPC]
    private void RPC_EvolveTeam(string teamTag)
    {
        EvolveTeam(teamTag);
    }

    private void EvolveTeam(string teamTag)
    {
        AgeType currentAge = (teamTag == "P1") ? p1_currentAge : p2_currentAge;
        AgeData nextAgeData = ageManager.GetNextAgeData(currentAge);
        if (nextAgeData == null) return;

        BaseController targetBase = null;
        if (teamTag == "P1")
        {
            p1_currentAge = nextAgeData.ageType;
            targetBase = p1_Base;
        }
        else // teamTag == "P2"
        {
            p2_currentAge = nextAgeData.ageType;
            targetBase = p2_Base;
        }

        if (targetBase != null)
        {
            targetBase.UpgradeBaseByAge(nextAgeData);
        }

        string localPlayerTeamTag = isDebugMode ? "P1" : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");
        if (teamTag == localPlayerTeamTag)
        {
            unitPanelManager.UpdateAge(nextAgeData);
            OnAgeEvolved?.Invoke(nextAgeData);
            CheckForAgeUp();
            Debug.Log($"플레이어({localPlayerTeamTag})의 시대가 {nextAgeData.ageType}으로 진화했습니다.");
        }
    }

    private void CheckForAgeUp()
    {
        string localTeamTag = isDebugMode ? "P1" : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");
        AgeType localPlayerAge = (localTeamTag == "P1") ? p1_currentAge : p2_currentAge;
        bool canUpgrade = ageManager.CanUpgrade(localPlayerAge, GetLocalPlayerExp());
        OnEvolveStatusChanged?.Invoke(canUpgrade);

        if (canUpgrade)
        {
            OnInfoMessage?.Invoke("다음 시대로 진화할 수 있습니다!");
        }
    }
    #endregion

    public void NotifyUltimateSkillUsed(float cooldownTime)
    {
        // 구독된 모든 함수(InGameUIManager의 StartUltimateCooldownVisual)를 호출
        OnUltimateSkillUsed?.Invoke(cooldownTime);
    }

    public AgeType GetLocalPlayerCurrentAge()
    {
        string localTeamTag = isDebugMode ? "P1" : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");
        return (localTeamTag == "P1") ? p1_currentAge : p2_currentAge;
    }

    private IEnumerator PassiveGoldGeneration()
    {
        var fiveSecondWait = new WaitForSeconds(5f);
        while (true)
        {
            yield return fiveSecondWait;
            int goldToAdd = 0;
            switch (p2_currentAge) // 시대에 따라 골드량이 변함
            {
                case AgeType.Ancient: goldToAdd = 15; break;
                case AgeType.Medieval: goldToAdd = 40; break;
                case AgeType.Modern: goldToAdd = 100; break;
            }
            AddGold(goldToAdd);
        }
    }

    public BaseController GetLocalPlayerBase()
    {
        string myTeamTag = isDebugMode ? "BaseP1" : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "BaseP1" : "BaseP2");
        return myTeamTag == "BaseP1" ? p1_Base : p2_Base;
    }

    private void GameOver(string losingTeamTag)
    {
        if (isGameOver) return;
        isGameOver = true;

        if (isDebugMode)
        {
            RPC_ShowResultPanels(losingTeamTag);
        }
        else
        {
            photonView.RPC("RPC_ShowResultPanels", RpcTarget.All, losingTeamTag);
        }
    }
    

    [PunRPC]
    private void RPC_ShowResultPanels(string losingTeamTag)
    {
        Time.timeScale = 0f;
        OnInfoMessage?.Invoke("게임 오버");

        string myTeamTag = isDebugMode ? "P1" : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");

        if (myTeamTag == losingTeamTag)
        {
            OnGameLost?.Invoke();
            SendMatchResult(false);
        }
        else
        {
            OnGameWon?.Invoke();
            SendMatchResult(true);
        }
    }
    public string GetLocalPlayerBaseTag()
    {
        // 디버그 모드일 경우, 항상 P1을 기준으로 테스트합니다.
        if (isDebugMode)
        {
            return "BaseP1";
        }

        // 포톤 네트워크에 연결된 온라인 모드일 경우
        if (PhotonNetwork.IsConnected)
        {
            // 내가 마스터 클라이언트(방장)이면 "BaseP1", 아니면(게스트) "BaseP2"를 반환합니다.
            return PhotonNetwork.IsMasterClient ? "BaseP1" : "BaseP2";
        }

        // 예외적인 상황 (네트워크 연결도, 디버그 모드도 아닌 경우)
        Debug.LogWarning("GetLocalPlayerBaseTag: 네트워크 상태를 알 수 없습니다. 기본값 'BaseP1'을 반환합니다.");
        return "BaseP1";
    }
    #region 게임오버 시퀀스

    private void SendMatchResult(bool isWinner)
    {
        if (isDebugMode) return;

        FirebaseUser user = UserAuthService.Auth?.CurrentUser;
        if (user == null) return;

        string resultLog = isWinner ? "승리" : "패배";
        Debug.Log($"[게임 결과] 플레이어: {user.DisplayName}, 결과: {resultLog}");
    }

    #endregion

}
