using KYG;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("궁극기 버튼 UI")] // 추가된 부분
    public Button ultimateSkillButton;
    public Image ultimateSkillCooldownImage;

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
            InGameManager.Instance.OnUltimateSkillUsed += StartUltimateCooldownVisual;
            InGameManager.Instance.OnInfoMessage += ShowInfoText;
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
        if (ultimateSkillButton != null)
        {
            ultimateSkillButton.onClick.AddListener(OnUltimateSkillButtonClicked);
            // 초기 시대(고대)의 궁극기 정보로 UI 업데이트
            AgeData initialAgeData = AgeManager.Instance.GetAgeData(AgeType.Ancient);
            if (initialAgeData != null) HandleAgeEvolvedUI(initialAgeData);
        }
        if (ultimateSkillCooldownImage != null)
        {
            ultimateSkillCooldownImage.fillAmount = 0; // 쿨타임 UI 초기화
        }
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
            InGameManager.Instance.OnUltimateSkillUsed -= StartUltimateCooldownVisual;
        }
    }
    private void Update()
    {
        // 터렛 배치 또는 판매 상태일 때 입력을 확인합니다.
        if (currentState != PlayerActionState.None)
        {
            // 우클릭 또는 ESC 키로 행동을 취소합니다.
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlayerAction();
            }
        
        }
        //esc 누를 때 옵션 패널 열기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ESC 키 입력 감지! OptionManager.Instance는? " + (OptionManager.Instance != null));
            // [조건 1] 옵션 패널이 이미 화면에 나와있다면?
            if (OptionManager.Instance != null && OptionManager.Instance.optionPanel.activeInHierarchy)
            {
                //
                OptionManager.Instance.OnClickedOptionCancel();
            }
            else
            {
                CancelPlayerAction();

                if (OptionManager.Instance != null)
                {
                    OptionManager.Instance.ShowOptionPanel();
                }
            }
        }

    }
    private void HandleAgeEvolvedUI(KYG.AgeData newAgeData)
    {
        Debug.Log($"UI UPDATE: 시대 발전에 따른 UI 업데이트 시도. 새로운 시대: {newAgeData.ageType}");

        // 궁극기 버튼 UI 업데이트
        if (ultimateSkillButton == null)
        {
            Debug.LogError("궁극기 버튼(ultimateSkillButton)이 InGameUIManager에 할당되지 않았습니다!");
            return;
        }

        // 버튼의 Image 컴포넌트를 직접 참조하는 것이 더 안전합니다.
        Image buttonImage = ultimateSkillButton.image;
        if (buttonImage == null)
        {
            Debug.LogError("궁극기 버튼(ultimateSkillButton)에 Image 컴포넌트가 없습니다!");
            return;
        }

        if (newAgeData.ultimateSkill == null)
        {
            Debug.LogError($"{newAgeData.ageType}의 AgeData 에셋에 UltimateSkillData가 할당되지 않았습니다!");
            return;
        }

        if (newAgeData.ultimateSkill.skillIcon == null)
        {
            Debug.LogError($"{newAgeData.ultimateSkill.name} 에셋에 스킬 아이콘(skillIcon) 스프라이트가 할당되지 않았습니다!");
            return;
        }

        // 모든 데이터가 정상일 때만 스프라이트 변경
        Debug.Log($"성공: {newAgeData.ultimateSkill.skillIcon.name} 스프라이트를 버튼에 적용합니다.");
        buttonImage.sprite = newAgeData.ultimateSkill.skillIcon;
    }

    public void RegisterPlayerBase(KYG.BaseController playerBase)
    {
        if (playerBase != null)
        {
            playerBase.OnHpChanged += UpdateBaseHpUI;
            UpdateBaseHpUI(playerBase.CurrentHP, playerBase.MaxHP);
        }
    }
    public void RegisterOpponentBase(KYG.BaseController opponentBase)
    {
        if (opponentBase != null)
        {
            opponentBase.OnHpChanged += UpdateGuestBaseUI;
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
            StartCoroutine(FadeOutInfoText(2.0f));
        }
    }

    private IEnumerator FadeOutInfoText(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInfoText();
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

    #region 궁극기 UI (추가된 부분)
    public void OnUltimateSkillButtonClicked()
    {
        SoundManager.Instance.PlayUltimateSkillSound();
        AgeType currentAge = InGameManager.Instance.GetLocalPlayerCurrentAge();
        AgeData currentAgeData = AgeManager.Instance.GetAgeData(currentAge);

        if (currentAgeData == null || currentAgeData.ultimateSkill == null)
        {
            Debug.LogError("현재 시대의 UltimateSkillData를 찾을 수 없습니다. AgeData 에셋에 궁극기 데이터가 할당되었는지 확인해주세요.");
            return;
        }

        UltimateSkillManager.Instance.TryCastUltimate(currentAgeData.ultimateSkill);
    }



    private void StartUltimateCooldownVisual(float cooldownTime)
    {
        if (ultimateSkillCooldownImage != null)
        {
            StartCoroutine(UltimateCooldownCoroutine(cooldownTime));
        }
    }

    private IEnumerator UltimateCooldownCoroutine(float cooldownTime)
    {
        ultimateSkillButton.interactable = false;

        float timer = 0f;
        ultimateSkillCooldownImage.fillAmount = 1;

        while (timer < cooldownTime)
        {
            timer += Time.deltaTime;
            ultimateSkillCooldownImage.fillAmount = 1 - (timer / cooldownTime);
            yield return null;
        }

        ultimateSkillCooldownImage.fillAmount = 0;
        ultimateSkillButton.interactable = true;
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
    public void EnterTurretPlaceMode(TurretData data)
    {
        SoundManager.Instance.PlayUIClick();
        currentState = PlayerActionState.PlacingTurret;
        turretDataToPlace = data;
        // --- 영문으로 변경 ---
        ShowInfoText("Click a slot to build a turret. (Right-click to cancel)");
    }

    public void EnterTurretSellMode()
    {
        SoundManager.Instance.PlayUIClick();
        currentState = PlayerActionState.SellingTurret;
        turretPrefabToPlace = null;
        // --- 영문으로 변경 ---
        ShowInfoText("Select a turret to sell. (Right-click to cancel)");
    }

    public void CancelPlayerAction()
    {
        currentState = PlayerActionState.None;
        turretPrefabToPlace = null;
        HideInfoText();
    }

    public void OnClick_AddTurretSlotButton()
    {
        SoundManager.Instance.PlayEvolveSound();
        SoundManager.Instance.PlayUIClick();
        int slotCost = 100;
        var baseCtrl = InGameManager.Instance.GetLocalPlayerBase();

        if (baseCtrl == null || InGameManager.Instance == null)
        {
            Debug.LogError("플레이어 기지 또는 게임 매니저를 찾을 수 없습니다!");
            return;
        }

        if (InGameManager.Instance.SpendGold(slotCost))
        {
            baseCtrl.UnlockNextTurretSlot(slotCost);
            // --- 영문으로 변경 ---
            ShowInfoText("Turret slot added!");
        }
        else
        {
            // --- 영문으로 변경 ---
            ShowInfoText("Not enough gold to add a slot!");
        }
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
        if (ultimateSkillButton != null) ultimateSkillButton.gameObject.SetActive(false);

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
            HideAllInGameUI();
            winnerPanel.SetActive(true);
        }
    }
    private void ShowLoserPanel()
    {
        if (loserPanel != null)
        {
            HideAllInGameUI();
            loserPanel.SetActive(true);
        }
    }

    public void ReturnToLobby()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        Time.timeScale = 1f;

        if (winnerPanel != null) winnerPanel.SetActive(false);
        if (loserPanel != null) loserPanel.SetActive(false);

        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            Debug.Log("디버그 모드: 로비 씬을 직접 로드합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            Debug.Log("네트워크 모드: 포톤을 통해 로비로 돌아갑니다.");
            if (PhotonManager.Instance != null)
            {
                PhotonManager.Instance.LeaveRoomAndLoadLobby();
            }
            else
            {
                Debug.LogError("PhotonManager를 찾을 수 없습니다! 안전하게 LobbyScene을 직접 로드합니다.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
            }
        }
    }

    #endregion
}