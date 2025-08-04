using Firebase.Auth;
using KYG;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 게임의 자원(골드, 경험치, 기지 체력) 및 게임 상태를 관리하는 싱글톤 매니저 클래스.
/// UI에 직접 접근하지 않고, 상태 변경 시 이벤트를 통해 외부에 알립니다.
/// </summary>
public class InGameManager : MonoBehaviourPunCallbacks
{
    #region 변수
    public static InGameManager Instance { get; private set; }

    [Header("디버그 옵션")]
    public bool isDebugMode = false;
    public bool isDebugHost = true;

    [Header("참조")]
    public KYG.AgeManager ageManager;
    public PHK.UnitPanelManager unitPanelManager; // 유닛 패널 버튼 상태 변경에 사용
    public BaseController p1_Base { get; private set; }
    public BaseController p2_Base { get; private set; }


    [Header("게임 기본 설정")]
    [SerializeField] private int startingGold = 175;

    private const string PLAYER_EXP_KEY = "PlayerExp"; // Photon Custom Properties 키

    // 게임 상태 관련 변수
    private int currentGold;
    //private int currentEXP; // 현재 경험치 값은 Photon Custom Properties를 통해 관리
    private PhotonView photonView;
    private string teamTag;
    private bool isGameOver = false;
    private AgeType p1_currentAge = AgeType.Ancient;
    private AgeType p2_currentAge = AgeType.Ancient;

    // --- 이벤트 ---
    public event Action<KYG.AgeData> OnAgeEvolved;
    public event Action<int, int> OnResourceChanged;
    public event Action<int, int> OnPlayerBaseHealthChanged;    // P1(내 기지) 이벤트
    public event Action<int, int> OnOpponentBaseHealthChanged;  // P2(상대 기지) 이벤트
    public event Action<string> OnInfoMessage; // UI에 일반 메시지를 표시하기 위한 이벤트
    public event Action<bool> OnEvolveStatusChanged; // 시대 진화 버튼 상태 변경 이벤트
    public event Action OnGameWon;
    public event Action OnGameLost;
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
            // 이벤트 구독은 여기서 합니다.
            if (p1_Base != null) p1_Base.OnHpChanged += HandleP1BaseHpChanged;
            Debug.Log("P1 기지가 InGameManager에 등록되었습니다.");
        }
        else if (team == "BaseP2")
        {
            p2_Base = baseController;
            // 이벤트 구독은 여기서 합니다.
            if (p2_Base != null) p2_Base.OnHpChanged += HandleP2BaseHpChanged;
            Debug.Log("P2 기지가 InGameManager에 등록되었습니다.");
        }
    }

    private void Start()
    {
        // 골드 자원 초기화
        currentGold = startingGold;
        // 플레이어 커스텀 프로퍼티 초기화 (시작 시 0으로 설정)
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PLAYER_EXP_KEY))
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, 0 } });
        }
        else
        {
            // 이미 키가 있다면 현재 값을 0으로 초기화
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


        // 초기 골드 및 경험치 UI 업데이트 (경험치는 CustomProperties에서 가져옴)
        OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp());
        OnInfoMessage?.Invoke("게임 시작!");


        // 게임 시작 시 시대 진화 버튼을 비활성화 상태로 설정
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

    // 플레이어 커스텀 프로퍼티 업데이트 콜백 메서드
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 플레이어의 경험치 프로퍼티가 변경되었을 때
        if (changedProps.ContainsKey(PLAYER_EXP_KEY))
        {
            int updatedExp = (int)changedProps[PLAYER_EXP_KEY];

            if (targetPlayer == PhotonNetwork.LocalPlayer)
            {
                // 내 경험치가 업데이트되면 UI 업데이트 및 시대 진화 체크
                OnResourceChanged?.Invoke(currentGold, updatedExp); // 골드와 함께 경험치 UI 업데이트
                CheckForAgeUp();
                Debug.Log($"내 경험치 업데이트: {updatedExp}");
            }
            else
            {
                // 다른 플레이어의 경험치 변경은 InGameManager에서 직접 처리할 필요 없음 (필요 시 추가)
                Debug.Log($"다른 플레이어({targetPlayer.NickName})의 경험치 업데이트: {updatedExp}");
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddGold(50); // 테스트용 골드 추가
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            // 'G' 키로 경험치 추가 및 현재 플레이어의 ActorNumber 사용
            AddExp(this.teamTag, 500); // 테스트용 경험치 추가
        }
    }
    #endregion

    #region 자원 및 체력 관리 함수

    public int GetLocalPlayerExp() // 현재 플레이어의 경험치를 CustomProperties에서 가져옴
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
        OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp()); // 경험치 UI와 함께 업데이트
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp()); // 경험치 UI와 함께 업데이트
            return true;
        }
        return false;
    }
    /// <summary>
    /// 특정 플레이어의 경험치를 추가하고 동기화.
    /// 이 함수는 해당 경험치 획득이 발생한 개체 또는 클라이언트에서 호출되어야 함.
    /// 예: 적 처치 시 해당 적을 처치한 플레이어 클라이언트에서 호출
    /// </summary>
    /// <param name="targetTeamTag">경험치를 획득할 플레이어의 팀 태그.</param>
    /// <param name="amount">추가할 경험치 양.</param>
    public void AddExp(string targetTeamTag, int amount)
    {
        // 네트워크 환경에서 RPC를 통해 경험치 업데이트 요청
        if (!isDebugMode)
        {
            // 요청하는 클라이언트에서 경험치 업데이트 요청
            photonView.RPC("RPC_AddExp", RpcTarget.MasterClient, targetTeamTag, amount);
            Debug.Log($"경험치 추가 요청: 팀 {targetTeamTag}, 양: {amount}");
        }
        else // 디버그 환경에서 즉시 처리
        {
            // 디버그 환경에서 요청에 따라 현재 플레이어의 경험치를 직접 추가합니다.
            string myTeamTag = isDebugHost ? "P1" : "P2";
            if (myTeamTag == targetTeamTag)
            {
                int newExp = GetLocalPlayerExp() + amount;
                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, newExp } });
                Debug.Log($"[DebugMode] 팀 {targetTeamTag}에 경험치 {amount} 추가. 현재 경험치: {newExp}");
            }
        }
    }

    [PunRPC]
    private void RPC_AddExp(string targetTeamTag, int amount, PhotonMessageInfo info)
    {
        // 마스터 클라이언트만 이 RPC를 실행합니다.
        if (!PhotonNetwork.IsMasterClient) return;

        // 팀 태그에 해당하는 플레이어의 ActorNumber를 찾습니다. (P1=1, P2=2)
        int targetPlayerActorNumber = (targetTeamTag == "P1") ? 1 : 2;

        Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(targetPlayerActorNumber);
        if (targetPlayer == null)
        {
            Debug.LogError($"RPC_AddExp: 대상 플레이어 (팀: {targetTeamTag}, ActorNumber: {targetPlayerActorNumber})를 찾을 수 없습니다.");
            return;
        }

        // 현재 경험치를 가져와서 업데이트.
        int currentExp = 0;
        if (targetPlayer.CustomProperties.TryGetValue(PLAYER_EXP_KEY, out object expValue))
        {
            currentExp = (int)expValue;
        }

        int newExp = currentExp + amount;

        // 경험치 업데이트 (이 코드가 실행되면 모든 클라이언트의 OnPlayerPropertiesUpdate가 호출됩니다)
        targetPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, newExp } });

        Debug.Log($"마스터 클라이언트: 팀 {targetTeamTag}에 {amount} 경험치 추가, 총 경험치: {newExp}");
    }
    private void HandleP1BaseHpChanged(int currentHp, int maxHp)
    {
        // P1 기지 체력이 바뀌면 P1 이벤트 발생
        OnPlayerBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0 && PhotonNetwork.IsMasterClient)
        {
            GameOver("P1");
        }
    }
    private void HandleP2BaseHpChanged(int currentHp, int maxHp)
    {
        // P2 기지 체력이 바뀌면 P2 이벤트 발생
        OnOpponentBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0 && PhotonNetwork.IsMasterClient)
        {
            GameOver("P2");

        }
    }
    #endregion

    #region 시대 진화 관리
    public void AttemptEvolve()
    {
        // 현재 플레이어의 팀 태그와 시대를 가져옵니다.
        string localPlayerTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");
        AgeType localPlayerAge = (localPlayerTag == "P1") ? p1_currentAge : p2_currentAge;

        // 현재 시대 진화 조건을 확인합니다.
        if (ageManager.CanUpgrade(localPlayerAge, GetLocalPlayerExp()))
        {
            // 디버그 모드인 경우
            if (isDebugMode)
            {
                Debug.Log($"디버그 모드: {localPlayerTag}팀의 시대 진화를 시도합니다.");
                // isDebugHost에 따라 올바른 플레이어 ActorNumber를 시뮬레이션하여 RPC를 호출합니다.
                int localActorNumber = isDebugHost ? 1 : 2;
                RPC_ConfirmEvolve(localActorNumber);
            }
            // 네트워크 모드인 경우
            else
            {
                photonView.RPC("RPC_RequestEvolve", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
        else
        {
            OnInfoMessage?.Invoke("시대 진화 실패: 경험치 부족!");
            Debug.Log("시대 진화 실패: 경험치 부족");
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

        // 마스터 클라이언트에서 해당 플레이어의 현재 시대와 경험치를 다시 한번 검증
        if (ageManager.CanUpgrade(playerAge, actualExp))
        {
            photonView.RPC("RPC_ConfirmEvolve", RpcTarget.All, requestingPlayerActorNumber);
        }
        else
        {
            Debug.LogWarning($"플레이어 {requestingPlayer.NickName} 시대 진화 실패 (마스터 클라이언트 검증)");
            photonView.RPC("RPC_ShowInfoMessage", requestingPlayer, "시대 진화 실패: 경험치 부족!"); // 특정 클라이언트에게 메시지 전송
        }
    }

    [PunRPC]
    private void RPC_ConfirmEvolve(int targetPlayerActorNumber)
    {
        Debug.Log($"{targetPlayerActorNumber}번 플레이어의 시대 진화 확인 RPC 수신");
        string teamTag = (targetPlayerActorNumber == 1) ? "P1" : "P2";

        // 1. InGameManager의 해당 플레이어 시대 상태를 다음 시대로 업데이트
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
            Debug.LogError("다음 시대 데이터를 찾을 수 없습니다!");
            return;
        }

        // 2. 해당하는 기지(BaseController)를 찾아 업그레이드 함수를 '호출'
        BaseController targetBase = (teamTag == "P1") ? p1_Base : p2_Base;
        if (targetBase != null)
        {
            targetBase.UpgradeBaseByAge(nextAgeData);
        }
        else
        {
            Debug.LogError($"{teamTag}번 기지를 찾을 수 없어 시대 업그레이드를 할 수 없습니다.");
        }

        // 3. UI 업데이트 및 후처리 (현재 플레이어인 경우에만)
        string localPlayerTeamTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");

        // 시대 진화를 한 플레이어가 '내' 자신인지 팀 태그로 확인합니다.
        if (teamTag == localPlayerTeamTag)
        {
            // 경험치 소모 또는 초기화 로직이 필요하다면 여기에 추가
            // 예: SpendExp(ageManager.GetRequiredExpForNextAge(...));

            // UI 업데이트
            unitPanelManager.UpdateAge(nextAgeData);
            OnAgeEvolved?.Invoke(nextAgeData);
            CheckForAgeUp(); // 시대 버튼 상태 다시 체크

            Debug.Log($"현재 플레이어({localPlayerTeamTag})의 시대가 {nextAgeData.ageType}으로 진화하여 UI를 업데이트합니다.");
        }
    }

    [PunRPC]
    private void RPC_ShowInfoMessage(string message)
    {
        OnInfoMessage?.Invoke(message);
    }

    private void CheckForAgeUp()
    {
        // 현재 플레이어의 현재 시대를 기반으로 다음 시대 진화 가능 여부 체크
        string localTeamTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");
        AgeType localPlayerAge = (localTeamTag == "P1") ? p1_currentAge : p2_currentAge;
        bool canUpgrade = ageManager.CanUpgrade(localPlayerAge, GetLocalPlayerExp());
        OnEvolveStatusChanged?.Invoke(canUpgrade);

        if (canUpgrade)
        {
            OnInfoMessage?.Invoke("다음 시대로 진화할 수 있습니다!");
        }
    }
    #endregion
    private IEnumerator PassiveGoldGeneration()
    {
        // 5초 대기 시간을 미리 할당하여 가비지 컬렉션을 줄일 수 있습니다.
        var fiveSecondWait = new WaitForSeconds(5f);

        while (true) // 게임이 진행되는 동안 계속 반복
        {
            yield return fiveSecondWait; // 5초 대기

            int goldToAdd = 0;

            // ageManager가 할당되어 있는지 확인
            if (ageManager != null)
            {
                // --- 이 부분은 다시 작성되었습니다 ---
                // 1. 현재 플레이어(나)의 팀 태그를 확인합니다.
                string localTeamTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");

                // 2. 이 팀 태그에 맞는 현재 시대 상태를 가져옵니다.
                AgeType localPlayerAge = (localTeamTag == "P1") ? p1_currentAge : p2_currentAge;

                // 3. 현재 플레이어의 현재 시대를 기반으로 얻을 골드량을 결정합니다.
                switch (localPlayerAge)
                {
                    case AgeType.Ancient:
                        goldToAdd = 15;
                        break;

                    case AgeType.Medieval: // 중세 시대가 되면
                        goldToAdd = 40;
                        break;

                    case AgeType.Modern: // 현대 시대가 되면
                        goldToAdd = 100;
                        break;

                    default:
                        goldToAdd = 0; // 해당 시대가 아니면 골드 없음
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
        if (isGameOver) return; // 이미 처리되었다면 중복 실행 방지
        isGameOver = true;

        Debug.Log($"게임 오버. 패배 팀: {losingTeamTag}. 마스터 클라이언트가 RPC를 보낼 것입니다.");
        // 모든 클라이언트에게 게임 종료 RPC를 전송
        photonView.RPC("RPC_ShowResultPanels", RpcTarget.All, losingTeamTag);
    }

    [PunRPC]
    private void RPC_ShowResultPanels(string losingTeamTag)
    {
        // 모든 클라이언트에서 게임 시간을 멈춤
        Time.timeScale = 0f;
        OnInfoMessage?.Invoke("게임 오버"); // 큰 글씨로 GameOver 메시지 표시

        // 현재 클라이언트의 네트워크 상태에 따라 자신의 팀 태그를 파악
        string myTeamTag;
        if (isDebugMode)
        {
            myTeamTag = isDebugHost ? "P1" : "P2";
        }
        else
        {
            myTeamTag = PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2";
        }

        // 패배한 팀 태그와 자신의 팀 태그를 비교하여 승/패 이벤트 호출
        if (myTeamTag == losingTeamTag)
        {
            OnGameLost?.Invoke();
            Debug.Log("결과: 당신은 패배했습니다.");
            SendMatchResult(false); // 패배 결과 전송
        }
        else
        {
            OnGameWon?.Invoke();
            Debug.Log("결과: 당신은 승리했습니다!");
            SendMatchResult(true); // 승리 결과 전송
        }
    }
    #region 게임오버 시퀀스

    /// <summary>
    /// 게임 결과를 기록하기 위해 네트워크 서비스와 상호작용
    /// 이 함수는 각 클라이언트(플레이어)에서 자신의 결과를 전송할 때 호출
    /// </summary>
    /// <param name="isWinner">현재 플레이어의 승리 여부.</param>
    private void SendMatchResult(bool isWinner)
    {
        // 네트워크 사용자가 아닌 경우 시스템을 우회
        // 현재 로그인된 Firebase 사용자 정보를 가져옴
        FirebaseUser user = UserAuthService.Auth?.CurrentUser;
        if (user == null)
        {
            Debug.LogError("게임 결과 전송: 로그인된 Firebase 사용자를 찾을 수 없습니다.");
            return;
        }

        string resultLog = isWinner ? "승리" : "패배";
        Debug.Log($"[게임 결과 상호작용] 플레이어: {user.DisplayName} ({user.UserId}), 결과: {resultLog}");

        // TODO: 네트워크 서버는 이 상호작용을 받아 DB에 저장하는 로직
        // 현재는 데이터를 업데이트하는 로직이 없음

    }
    #endregion
}