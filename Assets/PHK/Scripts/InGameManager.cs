using KYG;
using Photon.Pun;
using System;
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
    public KYG.BaseController p1_Base;
    public KYG.BaseController p2_Base;

    [Header("게임 기본 설정")]
    [SerializeField] private int startingGold = 175;
    [SerializeField] private int maxBaseHealth = 1000;

    // 현재 게임 상태 변수
    private int currentGold;
    private int currentEXP;
    private int currentBaseHealth;
    private PhotonView photonView;

    // --- 이벤트 ---
    public event Action<KYG.AgeData> OnAgeEvolved;
    public event Action<int, int> OnResourceChanged;
    public event Action<int, int> OnBaseHealthChanged;
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

    private void Start()
    {
        // 게임 상태 초기화
        currentGold = startingGold;
        currentEXP = 0;
        currentBaseHealth = maxBaseHealth;

        // 초기 게임 상태를 이벤트로 UI에 반영
        OnResourceChanged?.Invoke(currentGold, currentEXP);
        OnBaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);
        OnInfoMessage?.Invoke("Game Started!");

        if (ageManager != null)
        {
            ageManager.OnAgeChanged += HandleAgeChanged;
        }
        else
        {
            Debug.LogError("AgeManager가 할당되지 않았습니다! InGameManager의 Inspector에서 할당해주세요.");
        }

        // 게임 시작 시 시대 발전 버튼은 비활성화 상태로 시작
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
                AddGold(50); // 테스트용 골드 추가
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                AddExp(500); // 테스트용 경험치 추가
            }
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

    public void TakeBaseDamage(int damage)
    {
        currentBaseHealth -= damage;
        currentBaseHealth = Mathf.Max(currentBaseHealth, 0);
        OnBaseHealthChanged?.Invoke(currentBaseHealth, maxBaseHealth);

        if (currentBaseHealth <= 0) GameOver();
    }
    #endregion

    #region 시대 발전 관련
    public void AttemptEvolve()
    {
        if (isDebugMode)
        {
            ageManager.TryUpgradeAge(currentEXP);
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
        if (targetPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            ageManager.TryUpgradeAge(currentEXP);
        }
        else
        {
            Debug.Log($"다른 플레이어({targetPlayerActorNumber})의 시대 발전을 확인했습니다.");
        }
    }

    private void HandleAgeChanged(KYG.AgeData newAgeData)
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

    private void GameOver()
    {
        Debug.Log("게임 오버 처리 추가 작업 필요");
        OnInfoMessage?.Invoke("GAME OVER");
        Time.timeScale = 0f;
    }
}
