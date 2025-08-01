using KYG;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 게임의 핵심 규칙(자원, 체력, 시대)과 상태를 관리하는 싱글턴 매니저 클래스.
/// UI를 직접 제어하지 않고, 상태 변경 시 이벤트를 통해 외부에 알립니다.
/// </summary>
public class InGameManager : MonoBehaviourPunCallbacks
{
    #region 변수
    public static InGameManager Instance { get; private set; }

    [Header("디버그 옵션")]
    public bool isDebugMode = false;
    public bool isDebugHost = true;

    [Header("관리대상")]
    public KYG.AgeManager ageManager;
    public PHK.UnitPanelManager unitPanelManager; // 시대 발전 버튼 상태 변경을 위해 참조 유지
    public BaseController p1_Base { get; private set; }
    public BaseController p2_Base { get; private set; }




    [Header("게임 기본 설정")]
    [SerializeField] private int startingGold = 175;

    private const string PLAYER_EXP_KEY = "PlayerExp"; // Photon Custom Properties 키

    // 현재 게임 상태 변수
    private int currentGold;
    //private int currentEXP; // 로컬 변수 대신 Photon Custom Properties 사용
    private PhotonView photonView;
    private string teamTag;
    private bool isGameOver = false;
    private AgeType p1_currentAge = AgeType.Ancient;
    private AgeType p2_currentAge = AgeType.Ancient;

    // --- 이벤트 ---
    public event Action<KYG.AgeData> OnAgeEvolved;
    public event Action<int, int> OnResourceChanged;
    public event Action<int, int> OnPlayerBaseHealthChanged;    // P1(내 기지)용 이벤트
    public event Action<int, int> OnOpponentBaseHealthChanged;  // P2(상대 기지)용 이벤트
    public event Action<string> OnInfoMessage; // UI에 일반 메시지를 전달하기 위한 이벤트
    public event Action<bool> OnEvolveStatusChanged; // 시대 발전 가능 상태 변경 이벤트
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
            // 이벤트 연결을 여기서 직접 합니다.
            if (p1_Base != null) p1_Base.OnHpChanged += HandleP1BaseHpChanged;
            Debug.Log("P1 Base가 InGameManager에 등록되었습니다.");
        }
        else if (team == "BaseP2")
        {
            p2_Base = baseController;
            // 이벤트 연결을 여기서 직접 합니다.
            if (p2_Base != null) p2_Base.OnHpChanged += HandleP2BaseHpChanged;
            Debug.Log("P2 Base가 InGameManager에 등록되었습니다.");
        }
    }

    private void Start()
    {
        // 게임 상태 초기화
        currentGold = startingGold;
        // 플레이어 커스텀 프로퍼티 초기화 (없으면 0으로 설정)
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PLAYER_EXP_KEY))
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, 0 } });
        }
        else
        {
            // 이미 키가 있다면 명시적으로 0으로 초기화
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


        // 초기 골드 및 경험치 업데이트 (경험치는 CustomProperties에서 가져옴)
        OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp());
        OnInfoMessage?.Invoke("Game Started!");


        // 게임 시작 시 시대 발전 버튼은 비활성화 상태로 시작
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

    // 플레이어 커스텀 프로퍼티 업데이트 콜백 재정의
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 플레이어의 경험치 프로퍼티가 변경되었을 때
        if (changedProps.ContainsKey(PLAYER_EXP_KEY))
        {
            int updatedExp = (int)changedProps[PLAYER_EXP_KEY];

            if (targetPlayer == PhotonNetwork.LocalPlayer)
            {
                // 내 경험치가 변경된 경우 UI 업데이트 및 시대 발전 체크
                OnResourceChanged?.Invoke(currentGold, updatedExp); // 골드와 함께 경험치 UI 업데이트
                CheckForAgeUp();
                Debug.Log($"내 경험치 업데이트: {updatedExp}");
            }
            else
            {
                // 다른 플레이어의 경험치 변경은 현재 InGameManager에서 직접 처리하지 않음 (필요 시 추가)
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
            // 'G' 키로 경험치 추가 시 로컬 플레이어의 ActorNumber 사용
            AddExp(this.teamTag, 500); // 테스트용 경험치 추가
        }
    }

    #region 자원 및 체력 관리 함수

    public int GetLocalPlayerExp() // 로컬 플레이어의 경험치를 CustomProperties에서 가져옴
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
        OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp()); // 경험치 UI도 함께 업데이트
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            OnResourceChanged?.Invoke(currentGold, GetLocalPlayerExp()); // 경험치 UI도 함께 업데이트
            return true;
        }
        return false;
    }
    /// <summary>
    /// 특정 플레이어의 경험치를 추가하고 동기화.
    /// 이 함수는 해당 경험치 획득의 주체가 되는 클라이언트에서 호출되어야 함.
    /// 예: 유닛 처치 시 해당 유닛을 처치한 플레이어 클라이언트에서 호출
    /// </summary>
    /// <param name="targetPlayerActorNumber">경험치를 획득할 플레이어의 ActorNumber.</param>
    /// <param name="amount">추가할 경험치 양.</param>
    public void AddExp(string targetTeamTag, int amount)
    {
        // 네트워크 모드에서만 RPC를 통해 경험치 업데이트 요청
        if (!isDebugMode)
        {
            // 마스터 클라이언트에게 경험치 업데이트 요청
            photonView.RPC("RPC_AddExp", RpcTarget.MasterClient, targetTeamTag, amount);
            Debug.Log($"경험치 추가 요청: 팀 {targetTeamTag}, 양: {amount}");
        }
        else // 디버그 모드에서는 로컬에서 직접 처리
        {
            // 디버그 모드에서는 요청된 팀이 로컬 플레이어의 팀과 같을 때만 경험치를 추가합니다.
            string myTeamTag = isDebugHost ? "P1" : "P2";
            if (myTeamTag == targetTeamTag)
            {
                int newExp = GetLocalPlayerExp() + amount;
                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { PLAYER_EXP_KEY, newExp } });
                Debug.Log($"[DebugMode] 팀 {targetTeamTag}의 경험치 {amount} 추가. 현재 경험치: {newExp}");
            }
        }
    }

    [PunRPC]
    private void RPC_AddExp(string targetTeamTag, int amount, PhotonMessageInfo info)
    {
        // 마스터 클라이언트만 이 RPC를 실행합니다.
        if (!PhotonNetwork.IsMasterClient) return;

        // 팀 태그를 기반으로 플레이어의 ActorNumber를 결정합니다. (P1=1, P2=2)
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

        Debug.Log($"마스터 클라이언트: 팀 {targetTeamTag}에게 {amount} 경험치 추가, 새 경험치: {newExp}");
    }
    private void HandleP1BaseHpChanged(int currentHp, int maxHp)
    {
        // P1 기지 체력이 바뀌면 P1용 이벤트를 발생시킴
        OnPlayerBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0 && PhotonNetwork.IsMasterClient)
        {
            GameOver("P1");
        }
    }
    private void HandleP2BaseHpChanged(int currentHp, int maxHp)
    {
        // P2 기지 체력이 바뀌면 P2용 이벤트를 발생시킴
        OnOpponentBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0 && PhotonNetwork.IsMasterClient)
        {
            GameOver("P2");

        }
    }
    #endregion

    #region 시대 발전 관련
    public void AttemptEvolve()
    {
        // 로컬 플레이어의 팀 태그와 시대를 가져옵니다.
        string localPlayerTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");
        AgeType localPlayerAge = (localPlayerTag == "P1") ? p1_currentAge : p2_currentAge;

        // 발전 가능 여부를 확인합니다.
        if (ageManager.CanUpgrade(localPlayerAge, GetLocalPlayerExp()))
        {
            // 디버그 모드일 경우
            if (isDebugMode)
            {
                Debug.Log($"디버그 모드: {localPlayerTag}의 시대 발전을 직접 실행합니다.");
                // isDebugHost를 기반으로 올바른 플레이어 ActorNumber를 시뮬레이션하여 RPC를 호출합니다.
                int localActorNumber = isDebugHost ? 1 : 2;
                RPC_ConfirmEvolve(localActorNumber);
            }
            // 네트워크 모드일 경우
            else
            {
                photonView.RPC("RPC_RequestEvolve", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
        else
        {
            Debug.Log("시대 발전 실패: 경험치 부족");
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

        // 마스터 클라이언트가 해당 플레이어의 현재 시대와 경험치로 다시 한번 검증
        if (ageManager.CanUpgrade(playerAge, actualExp))
        {
            photonView.RPC("RPC_ConfirmEvolve", RpcTarget.All, requestingPlayerActorNumber);
        }
        else
        {
            Debug.LogWarning($"플레이어 {requestingPlayer.NickName} 시대 발전 실패 (마스터 클라이언트 검증)");
        }
    }



    [PunRPC]
    private void RPC_ConfirmEvolve(int targetPlayerActorNumber)
    {
        Debug.Log($"{targetPlayerActorNumber}번 플레이어의 시대 발전 확정 RPC 수신");
        string teamTag = (targetPlayerActorNumber == 1) ? "P1" : "P2";

        // 1. InGameManager가 해당 팀의 시대 상태를 직접 업데이트
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

        // 2. 해당하는 기지(BaseController)를 찾아 모델 변경 함수를 '직접' 호출
        BaseController targetBase = (teamTag == "P1") ? p1_Base : p2_Base;
        if (targetBase != null)
        {
            targetBase.UpgradeBaseByAge(nextAgeData);
        }
        else
        {
            Debug.LogError($"{teamTag}의 기지를 찾을 수 없어 모델을 변경할 수 없습니다.");
        }

        // 3. UI 업데이트 등 후속 처리 (로컬 플레이어인 경우에만)
        string localPlayerTeamTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");

        // 시대 발전을 한 플레이어가 '나' 자신인지 팀 태그로 확인합니다.
        if (teamTag == localPlayerTeamTag)
        {
            // 경험치 차감 또는 초기화 로직이 필요하다면 여기에 추가
            // 예: SpendExp(ageManager.GetRequiredExpForNextAge(...));

            // UI 업데이트
            unitPanelManager.UpdateAge(nextAgeData);
            OnAgeEvolved?.Invoke(nextAgeData);
            CheckForAgeUp(); // 발전 버튼 상태 다시 체크

            Debug.Log($"로컬 플레이어({localPlayerTeamTag})의 시대가 {nextAgeData.ageType}으로 발전하여 UI를 업데이트합니다.");
        }
    }
    //private void HandleAgeChanged(string teamtag, KYG.AgeData newAgeData)
    //{
    //    Debug.Log($"[InGameManager] AgeData 수신. 유닛 수: {newAgeData.spawnableUnits.Count}");
    //
    //    // UnitPanelManager는 UI 요소이므로, 직접 제어하기보다 이벤트로 처리하는 것이 이상적이나,
    //    // 현재 구조상 AgeData를 직접 전달해야 하므로 이 부분은 유지합니다.
    //    unitPanelManager.UpdateAge(newAgeData);
    //
    //    OnAgeEvolved?.Invoke(newAgeData);
    //    CheckForAgeUp();
    //}

    private void CheckForAgeUp()
    {
        // 로컬 플레이어의 현재 시대를 기준으로 발전 가능 여부 체크
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
        // 5초 대기 시간을 미리 만들어두면 불필요한 메모리 할당을 막을 수 있습니다.
        var fiveSecondWait = new WaitForSeconds(5f);

        while (true) // 게임이 끝날 때까지 무한 반복
        {
            yield return fiveSecondWait; // 5초간 대기

            int goldToAdd = 0;

            // ageManager가 할당되어 있는지 확인
            if (ageManager != null)
            {
                // --- 이 부분이 핵심적인 변경점입니다 ---
                // 1. 로컬 플레이어(나)의 팀 태그를 확인합니다.
                string localTeamTag = isDebugMode ? (isDebugHost ? "P1" : "P2") : (PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2");

                // 2. 팀 태그에 맞는 현재 시대 변수를 가져옵니다.
                AgeType localPlayerAge = (localTeamTag == "P1") ? p1_currentAge : p2_currentAge;

                // 3. 로컬 플레이어의 현재 시대를 기준으로 골드 지급량을 결정합니다.
                switch (localPlayerAge)
                {
                    case AgeType.Ancient:
                        goldToAdd = 15;
                        break;

                    case AgeType.Medieval: // 중세 시대가 있다면
                        goldToAdd = 40;
                        break;

                    case AgeType.Modern: // 현대 시대가 있다면
                        goldToAdd = 100;
                        break;

                    default:
                        goldToAdd = 0; // 해당하는 시대가 없으면 지급 안함
                        break;
                }
            }

            if (goldToAdd > 0)
            {
                AddGold(goldToAdd);
            }
        }
    }
    private void GameOver(string losingTeamTag)
    {
        if (isGameOver) return; // 게임오버가 이미 처리되었다면 중복 실행 방지
        isGameOver = true;

        Debug.Log($"Game Over. Losing team: {losingTeamTag}. MasterClient will send RPC.");
        // 모든 클라이언트에게 게임 결과 RPC를 전송
        photonView.RPC("RPC_ShowResultPanels", RpcTarget.All, losingTeamTag);
    }

    [PunRPC]
    private void RPC_ShowResultPanels(string losingTeamTag)
    {
        // 모든 클라이언트에서 게임 시간을 멈춤
        Time.timeScale = 0f;
        OnInfoMessage?.Invoke("GAME OVER"); // 기존 GameOver 메시지 표시

        // 디버그 모드와 네트워크 모드에 따라 자신의 팀 태그를 결정
        string myTeamTag;
        if (isDebugMode)
        {
            myTeamTag = isDebugHost ? "P1" : "P2";
        }
        else
        {
            myTeamTag = PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2";
        }

        // 패배한 팀과 자신의 팀을 비교하여 승/패 이벤트 호출
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