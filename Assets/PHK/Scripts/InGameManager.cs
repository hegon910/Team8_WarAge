using KYG;
using Photon.Pun;
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

    // 현재 게임 상태 변수
    private int currentGold;
    private int currentEXP;
    private PhotonView photonView;
    private string teamTag;

    // --- 이벤트 ---
    public event Action<KYG.AgeData> OnAgeEvolved;
    public event Action<int, int> OnResourceChanged;
    public event Action<int, int> OnPlayerBaseHealthChanged;    // P1(내 기지)용 이벤트
    public event Action<int, int> OnOpponentBaseHealthChanged;  // P2(상대 기지)용 이벤트
    public event Action<string> OnInfoMessage; // UI에 일반 메시지를 전달하기 위한 이벤트
    public event Action<bool> OnEvolveStatusChanged; // 시대 발전 가능 상태 변경 이벤트
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


        // 초기 게임 상태를 이벤트로 UI에 반영
        OnResourceChanged?.Invoke(currentGold, currentEXP);
        OnInfoMessage?.Invoke("Game Started!");

        if (ageManager != null)
        {
            ageManager.OnAgeChangedByTeam += HandleAgeChanged;
        }
        else
        {
            Debug.LogError("AgeManager가 할당되지 않았습니다! InGameManager의 Inspector에서 할당해주세요.");
        }
      //  if (p1_Base != null)
      //  {
      //      // p1_Base의 이벤트는 P1용 핸들러에 연결
      //      p1_Base.OnHpChanged += HandleP1BaseHpChanged;
      //  }
      //  if (p2_Base != null)
      //  {
      //      // p2_Base의 이벤트는 P2용 핸들러에 연결
      //      p2_Base.OnHpChanged += HandleP2BaseHpChanged;
      //  }
        // 게임 시작 시 시대 발전 버튼은 비활성화 상태로 시작
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
                AddGold(50); // 테스트용 골드 추가
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                AddExp(500); // 테스트용 경험치 추가
            }
        
    }

    #region 자원 및 체력 관리 함수
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
            Debug.Log($"{amount} 경험치 획득. 현재 경험치 : {currentEXP}");
        }
    }
    private void HandleP1BaseHpChanged(int currentHp, int maxHp)
    {
        // P1 기지 체력이 바뀌면 P1용 이벤트를 발생시킴
        OnPlayerBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0) GameOver();
    }
    private void HandleP2BaseHpChanged(int currentHp, int maxHp)
    {
        // P2 기지 체력이 바뀌면 P2용 이벤트를 발생시킴
        OnOpponentBaseHealthChanged?.Invoke(currentHp, maxHp);
        if (currentHp <= 0) GameOver();
    }

    #endregion

    #region 시대 발전 관련
    public void AttemptEvolve()
    {
        if (isDebugMode)
        {
            ageManager.TryUpgradeAge(teamTag, currentEXP);
        }
        else
        {
            photonView.RPC("RPC_RequestEvolve", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log("마스터 클라이언트에게 시대 발전을 요청합니다.");
        }
    }

    [PunRPC]
    private void RPC_RequestEvolve(int requestingPlayerActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Debug.Log($"{requestingPlayerActorNumber}번 플레이어의 시대 발전 요청을 수신 및 검증 (현재는 자동 통과)");
        photonView.RPC("RPC_ConfirmEvolve", RpcTarget.All, requestingPlayerActorNumber);
    }

    [PunRPC]
    private void RPC_ConfirmEvolve(int targetPlayerActorNumber)
    {
        Debug.Log($"{targetPlayerActorNumber}번 플레이어의 시대 발전 확정 RPC 수신");
        string teamTag = (targetPlayerActorNumber == 1) ? "P1" : "P2";
        if (targetPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            ageManager.TryUpgradeAge(teamTag, currentEXP);
        }
        else
        {
            Debug.Log($"다른 플레이어({targetPlayerActorNumber}, 팀: {teamTag})의 시대 발전을 확인했습니다.");
        }
    }

    private void HandleAgeChanged(string teamtag, KYG.AgeData newAgeData)
    {
        Debug.Log($"[InGameManager] AgeData 수신. 유닛 수: {newAgeData.spawnableUnits.Count}");

        // UnitPanelManager는 UI 요소이므로, 직접 제어하기보다 이벤트로 처리하는 것이 이상적이나,
        // 현재 구조상 AgeData를 직접 전달해야 하므로 이 부분은 유지합니다.
        unitPanelManager.UpdateAge(newAgeData);

        OnAgeEvolved?.Invoke(newAgeData);
        CheckForAgeUp();
    }

    private void CheckForAgeUp()
    {
        bool canUpgrade = ageManager.CanUpgrade(currentEXP);
        OnEvolveStatusChanged?.Invoke(canUpgrade); // 시대 발전 가능 여부를 이벤트로 알림

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
                // 현재 시대에 따라 지급할 골드량을 결정합니다.
                // (AgeType 이름은 실제 프로젝트의 enum 이름에 맞게 조정해야 할 수 있습니다)
                switch (ageManager.CurrentAge)
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

                    // 필요하다면 다른 시대에 대한 case 추가
                    // ...

                    default:
                        goldToAdd = 0; // 해당하는 시대가 없으면 지급 안함
                        break;
                }
            }

            if (goldToAdd > 0)
            {
                AddGold(goldToAdd);
                // OnInfoMessage?.Invoke($"+{goldToAdd} Gold"); // 골드 획득을 알리는 메시지 (선택 사항)
            }
        }
    }
    private void GameOver()
    {
        Debug.Log("게임 오버 처리 추가 작업 필요");
        OnInfoMessage?.Invoke("GAME OVER");
        Time.timeScale = 0f;
    }
}
