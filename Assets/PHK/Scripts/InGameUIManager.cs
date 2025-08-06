using PHK;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KYG;

// 인게임의 전반적인 UI를 관리하는 스크립트.
// 다른 매니저로부터 이벤트를 받아 UI를 변경하고, UI 버튼 입력을 받아 다른 매니저에 요청.
public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    [Header("UI 요소")]
    public TextMeshProUGUI inGameInfoText;
    public TextMeshProUGUI UnitInfoText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI expText;
    public Slider baseHpSlider;
    public Slider GuestBaseHpSlider;
    public Button evolveButton; // 시대 발전 버튼 참조
    public GameObject winnerPanel;
    public GameObject loserPanel;
    public TurretData turretDataToPlace;

    [Header("유닛 생산 큐")]
    public Slider productionSlider;
    public Toggle[] queueSlots = new Toggle[5];

    [Header("터렛 정보 UI")]
    public GameObject turretInfoPanel; // 판매 버튼이 있는 UI 패널
    private TurretSlot selectedSlot;   // 현재 선택한 슬롯

    // --- 터렛 관련 상태 ---
    // PlayerActionState에 따라 UI와 상호작용하는 상태를 나타내므로 UI 매니저에 위치.
    public enum PlayerActionState
    {
        None,
        PlacingTurret,
        SellingTurret
    }
    public PlayerActionState currentState { get; private set; } = PlayerActionState.None;
    public GameObject turretPrefabToPlace { get; private set; } // 설치할 터렛의 프리팹

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 게임 시작 시 정보 텍스트 숨김
        if (inGameInfoText != null) inGameInfoText.gameObject.SetActive(false);
        if (UnitInfoText != null) UnitInfoText.text = "";

        // --- 이벤트 구독 ---
        // 인게임 인스턴스가 null일 수 있으므로 안전하게 확인
        if (UnitSpawnManager.Instance != null)
        {
            UnitSpawnManager.Instance.OnQueueChanged += UpdateQueueUI;
            UnitSpawnManager.Instance.OnProductionProgress += UpdateProductionSlider;
            UnitSpawnManager.Instance.OnProductionStatusChanged += ToggleProductionSliderVisibility;
        }
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.OnResourceChanged += UpdateResourceUI;
            InGameManager.Instance.OnPlayerBaseHealthChanged += UpdateBaseHpUI;
            // 게스트 베이스 HP 처리
            InGameManager.Instance.OnOpponentBaseHealthChanged += UpdateGuestBaseUI;
            InGameManager.Instance.OnEvolveStatusChanged += UpdateEvolveButton;
            InGameManager.Instance.OnAgeEvolved += HandleAgeEvolvedUI;
            InGameManager.Instance.OnGameWon += ShowWinnerPanel;
            InGameManager.Instance.OnGameLost += ShowLoserPanel;
        }

        // UI 초기화
        if (productionSlider != null) productionSlider.gameObject.SetActive(false);
        if (queueSlots != null)
        {
            foreach (var slot in queueSlots)
            {
                if (slot != null) slot.SetIsOnWithoutNotify(false);
            }
        }
        // 시작할때 evolve버튼 비활성화
        if (evolveButton != null)
            evolveButton.interactable = false; // 초기에는 비활성화
        if (winnerPanel != null) winnerPanel.SetActive(false);
        if (loserPanel != null) loserPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴 시 이벤트 구독을 반드시 해제해야 메모리 누수를 방지
        if (UnitSpawnManager.Instance != null)
        {
            UnitSpawnManager.Instance.OnQueueChanged -= UpdateQueueUI;
            UnitSpawnManager.Instance.OnProductionProgress -= UpdateProductionSlider;
            UnitSpawnManager.Instance.OnProductionStatusChanged -= ToggleProductionSliderVisibility;
        }
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.OnResourceChanged -= UpdateResourceUI;
            InGameManager.Instance.OnPlayerBaseHealthChanged -= UpdateBaseHpUI;
            InGameManager.Instance.OnOpponentBaseHealthChanged -= UpdateGuestBaseUI;
            InGameManager.Instance.OnInfoMessage -= ShowInfoText;
            InGameManager.Instance.OnEvolveStatusChanged -= UpdateEvolveButton;
            InGameManager.Instance.OnAgeEvolved -= HandleAgeEvolvedUI;
            if (InGameManager.Instance.p1_Base != null)
            {
                InGameManager.Instance.p1_Base.OnHpChanged -= UpdateBaseHpUI;
            }
            if (InGameManager.Instance.p2_Base != null)
            {
                InGameManager.Instance.p2_Base.OnHpChanged -= UpdateGuestBaseUI;
            }
            InGameManager.Instance.OnGameWon -= ShowWinnerPanel;
            InGameManager.Instance.OnGameLost -= ShowLoserPanel;
        }
    }
    private void Update()
    {
        // 터렛 배치 또는 판매 상태일 때 입력을 확인합니다.
        if (currentState != PlayerActionState.None)
        {
            // 우클릭 또는 ESC 키로 행동을 취소합니다.
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlayerAction();
            }
        }
    }
    private void HandleAgeEvolvedUI(KYG.AgeData newAgeData)
    {
        // 예: 시대가 발전되었음을 알리는 텍스트 업데이트
        Debug.Log($"UI UPDATE: New Age - {newAgeData.ageType}");
        // 여기에 새로운 시대 정보(newAgeData)를 바탕으로 UI를 변경하는 코드를 작성
    }

    public void RegisterPlayerBase(KYG.BaseController playerBase)
    {
        if (playerBase != null)
        {
            // 내 HP 슬라이더는 '내 베이스'의 체력 변경 이벤트를 구독
            // playerBase.OnHpChanged += UpdateBaseHpUI;
            // UI 초기화를 위해 현재 체력으로 한번 업데이트
            playerBase.OnHpChanged += UpdateBaseHpUI;
            UpdateBaseHpUI(playerBase.CurrentHP, playerBase.MaxHP);
        }
    }
    public void RegisterOpponentBase(KYG.BaseController opponentBase)
    {
        if (opponentBase != null)
        {
            // 상대 HP 슬라이더는 '상대 베이스'의 체력 변경 이벤트를 직접 구독 (주석 해제)
            opponentBase.OnHpChanged += UpdateGuestBaseUI;

            // UI 초기화를 위해 현재 체력으로 한번 업데이트
            UpdateGuestBaseUI(opponentBase.CurrentHP, opponentBase.MaxHP);
        }
    }

    #region UI 업데이트 함수 (이벤트 수신)
    public void UpdateResourceUI(int newGold, int newExp)
    {
        if (goldText != null) goldText.text = $"{newGold}";
        if (expText != null) expText.text = $"{newExp}";
    }

    public void UpdateBaseHpUI(int currentHp, int maxHp)
    {
        Debug.Log($"--- PLAYER UI UPDATED --- 체력: {currentHp}/{maxHp}");
        if (baseHpSlider != null && maxHp > 0)
        {
            baseHpSlider.value = (float)currentHp / maxHp;
        }
    }

    public void UpdateGuestBaseUI(int currentHp, int maxHp)
    {
        Debug.Log($"--- OPPONENT UI UPDATED --- 체력: {currentHp}/{maxHp}");
        if (GuestBaseHpSlider != null && maxHp > 0)
        {
            GuestBaseHpSlider.value = (float)currentHp / maxHp;
        }
    }

    public void ShowInfoText(string message)
    {
        if (inGameInfoText != null)
        {
            inGameInfoText.text = message;
            inGameInfoText.gameObject.SetActive(true);
            // 필요하다면 몇 초 뒤에 자동으로 사라지는 기능 추가 가능
        }
    }

    public void HideInfoText()
    {
        if (inGameInfoText != null)
        {
            inGameInfoText.gameObject.SetActive(false);
        }
    }

    private void UpdateEvolveButton(bool canEvolve)
    {
        if (evolveButton != null)
        {
            evolveButton.interactable = canEvolve;
        }
    }
    #endregion

    #region 유닛 생산 UI
    private void UpdateQueueUI(int queuedCount)
    {
        for (int i = 0; i < queueSlots.Length; i++)
        {
            if (queueSlots[i] != null)
            {
                queueSlots[i].SetIsOnWithoutNotify(i < queuedCount);
            }
        }
    }

    private void UpdateProductionSlider(float progress)
    {
        if (productionSlider != null)
        {
            productionSlider.value = progress;
        }
    }

    private void ToggleProductionSliderVisibility(bool isVisible)
    {
        if (productionSlider != null)
        {
            productionSlider.gameObject.SetActive(isVisible);
        }
    }
    #endregion

    #region 터렛 관련 UI 및 상태 관리
    // 터렛 배치 버튼에서 호출
    public void EnterTurretPlaceMode(TurretData data)
    {
        currentState = PlayerActionState.PlacingTurret;
        turretDataToPlace = data;
        ShowInfoText("Click on a turret slot to build. (Right-click to cancel)");
    }

    // 터렛 판매 버튼에서 호출
    public void EnterTurretSellMode()
    {
        currentState = PlayerActionState.SellingTurret;
        turretPrefabToPlace = null;
        ShowInfoText("Select a turret to sell. (Right-click to cancel)");
    }

    // 터렛 배치/판매 상태 취소 또는 완료 시 호출
    public void CancelPlayerAction()
    {
        currentState = PlayerActionState.None;
        turretPrefabToPlace = null;
        HideInfoText();
    }

    // 터렛 슬롯 추가 버튼에서 호출
    public void OnClick_AddTurretSlotButton()
    {
        /*int slotCost = 100; // 비용은 InGameManager나 다른 곳에서 컨트롤하는 것이 더 좋음
        if (InGameManager.Instance.SpendGold(slotCost))
        {
            ShowInfoText("Turret Slot Added!");
            // TODO: BaseController에 슬롯 추가를 요청하는 기능
            // 예: InGameManager.Instance.GetPlayerBase().AddNewSlot();
        }
        else
        {
            ShowInfoText("Not Enough Gold to Add Turret Slot!");
        }*/
        var baseCtrl = InGameManager.Instance.GetLocalPlayerBase();
        if (baseCtrl != null)
            baseCtrl.UnlockNextTurretSlot(100); // 슬롯 해금 비용 100
    }
    private void HideAllInGameUI()
    {
        if (inGameInfoText != null) inGameInfoText.gameObject.SetActive(false);
        if (UnitInfoText != null) UnitInfoText.gameObject.SetActive(false);
        if (goldText != null) goldText.gameObject.SetActive(false);
        if (expText != null) expText.gameObject.SetActive(false);
        if (baseHpSlider != null) baseHpSlider.gameObject.SetActive(false);
        if (GuestBaseHpSlider != null) GuestBaseHpSlider.gameObject.SetActive(false);
        if (evolveButton != null) evolveButton.gameObject.SetActive(false);

        // 유닛 생산 관련 UI도 숨깁니다.
        if (productionSlider != null) productionSlider.gameObject.SetActive(false);
        if (queueSlots != null)
        {
            foreach (var slot in queueSlots)
            {
                if (slot != null) slot.gameObject.SetActive(false);
            }
        }
    }

    private void ShowWinnerPanel()
    {
        if (winnerPanel != null)
        {
            HideAllInGameUI(); // 다른 인게임 UI 숨김
            winnerPanel.SetActive(true);
        }
    }
    private void ShowLoserPanel()
    {
        if (loserPanel != null)
        {
            HideAllInGameUI(); // 다른 인게임 UI 숨김
            loserPanel.SetActive(true);
        }
    }

    public void ReturnToLobby()
    {
        // 게임오버 상태로 멈췄을 수 있으므로 시간을 다시 흐르게 합니다.
        Time.timeScale = 1f;

        // 승리/패배 모든 패널을 비활성화합니다.
        if (winnerPanel != null) winnerPanel.SetActive(false);
        if (loserPanel != null) loserPanel.SetActive(false);

        // --- 수정된 로직 ---

        // 1. 디버그 모드인지 먼저 확인
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            Debug.Log("디버그 모드: 로비 씬을 직접 로드합니다.");
            // "LobbyScene"은 실제 로비 씬 이름으로 맞춰야 합니다.
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }
        // 2. 네트워크 모드일 경우
        else
        {
            Debug.Log("네트워크 모드: 포톤을 통해 로비로 돌아갑니다.");
            if (PhotonManager.Instance != null)
            {
                // PhotonManager의 함수를 호출 (PhotonManager 내부에서 안전장치를 마련할 것)
                PhotonManager.Instance.LeaveRoomAndLoadLobby();
            }
            else
            {
                // PhotonManager가 없는 비상상황에서는 씬을 직접 로드
                Debug.LogError("PhotonManager를 찾을 수 없습니다! 안전하게 LobbyScene을 직접 로드합니다.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
            }
        }
    }

    #endregion
}
