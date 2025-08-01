using PHK;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// �ΰ����� �������� UI�� �����ϴ� ��ũ��Ʈ.
// �ٸ� �Ŵ�����κ��� �̺�Ʈ�� �޾� UI�� �����ϰ�, UI ��ư �Է��� �޾� �ٸ� �Ŵ������� ��û.
public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    [Header("UI ���")]
    public TextMeshProUGUI inGameInfoText;
    public TextMeshProUGUI UnitInfoText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI expText;
    public Slider baseHpSlider;
    public Slider GuestBaseHpSlider;
    public Button evolveButton; // �ô� ���� ��ư ����
    public GameObject winnerPanel;
    public GameObject loserPanel;

    [Header("���� ���� ť")]
    public Slider productionSlider;
    public Toggle[] queueSlots = new Toggle[5];
    
    

    // --- �ͷ� ���� ���� ---
    // PlayerActionState�� ���� UI�� ��ȣ�ۿ��ϴ� ��带 ��Ÿ���Ƿ� UI �Ŵ����� ����.
    public enum PlayerActionState
    {
        None,
        PlacingTurret,
        SellingTurret
    }
    public PlayerActionState currentState { get; private set; } = PlayerActionState.None;
    public GameObject turretPrefabToPlace { get; private set; } // �б� �������� �ܺ� ����

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
        // ���� ���� �� ���� �ؽ�Ʈ �����
        if (inGameInfoText != null) inGameInfoText.gameObject.SetActive(false);
        if (UnitInfoText != null) UnitInfoText.text = "";

        // --- �̺�Ʈ ���� ---
        // �̱��� �ν��Ͻ��� null�� �� �����Ƿ� �����ϰ� Ȯ��
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
            //�Խ�Ʈ ���̽� HP ó��
            InGameManager.Instance.OnOpponentBaseHealthChanged += UpdateGuestBaseUI;
            InGameManager.Instance.OnEvolveStatusChanged += UpdateEvolveButton;
            InGameManager.Instance.OnAgeEvolved += HandleAgeEvolvedUI;
            InGameManager.Instance.OnGameWon += ShowWinnerPanel;
            InGameManager.Instance.OnGameLost += ShowLoserPanel;
        }

        // UI �ʱ�ȭ
        if (productionSlider != null) productionSlider.gameObject.SetActive(false);
        if (queueSlots != null)
        {
            foreach (var slot in queueSlots)
            {
                if (slot != null) slot.SetIsOnWithoutNotify(false);
            }
        }
        //�����Ҷ� evolve��ư ��Ȱ��ȭ
        if (evolveButton != null)
            evolveButton.interactable = false; // �ʱ⿡�� ��Ȱ��ȭ
        if (winnerPanel != null) winnerPanel.SetActive(false);
        if (loserPanel != null) loserPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // ������Ʈ �ı� �� �̺�Ʈ ������ �ݵ�� �����ؾ� �޸� ������ ����
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
        // �ͷ� �Ǽ� �Ǵ� �Ǹ� ����� ���� �Է��� Ȯ���մϴ�.
        if (currentState != PlayerActionState.None)
        {
            // ��Ŭ�� �Ǵ� ESC Ű�� ������ �ൿ�� ����մϴ�.
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlayerAction();
            }
        }
    }
    private void HandleAgeEvolvedUI(KYG.AgeData newAgeData)
    {
        // ��: �ô밡 ����Ǿ����� �˸��� �ؽ�Ʈ ������Ʈ
        Debug.Log($"UI UPDATE: New Age - {newAgeData.ageType}");
        // ���⿡ ���ο� �ô� ����(newAgeData)�� �������� UI�� �����ϴ� �ڵ带 �ۼ�
    }

    public void RegisterPlayerBase(KYG.BaseController playerBase)
    {
        if (playerBase != null)
        {
            // �� HP �����̴��� '�� ����'�� ü�� ���� �̺�Ʈ�� ����
        //    playerBase.OnHpChanged += UpdateBaseHpUI;
            // UI �ʱ�ȭ�� ���� ���� ü������ �ѹ� ������Ʈ
            UpdateBaseHpUI(playerBase.CurrentHP, playerBase.MaxHP);
        }
    }
    public void RegisterOpponentBase(KYG.BaseController opponentBase)
    {
        if (opponentBase != null)
        {
            // ���� HP �����̴��� '��� ����'�� ü�� ���� �̺�Ʈ�� ����
         //   opponentBase.OnHpChanged += UpdateGuestBaseUI;
            // UI �ʱ�ȭ�� ���� ���� ü������ �ѹ� ������Ʈ
            UpdateGuestBaseUI(opponentBase.CurrentHP, opponentBase.MaxHP);
        }
    }

    #region UI ������Ʈ �Լ� (�̺�Ʈ ����)
    public void UpdateResourceUI(int newGold, int newExp)
    {
        if (goldText != null) goldText.text = $"{newGold}";
        if (expText != null) expText.text = $"{newExp}";
    }

    public void UpdateBaseHpUI(int currentHp, int maxHp)
    {
        Debug.Log($"--- PLAYER UI UPDATED --- ü��: {currentHp}/{maxHp}");
        if (baseHpSlider != null && maxHp > 0)
        {
            baseHpSlider.value = (float)currentHp / maxHp;
        }
    }

    public void UpdateGuestBaseUI(int currentHp, int maxHp)
    {
        Debug.Log($"--- OPPONENT UI UPDATED --- ü��: {currentHp}/{maxHp}");
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
            // �ʿ��ϴٸ� �� �� �ڿ� �ڵ����� ������� ���� �߰� ����
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

    #region ���� ���� UI
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

    #region �ͷ� ���� UI �� ���� ����
    // �ͷ� �Ǽ� ��ư���� ȣ��
    public void EnterTurretPlaceMode(GameObject turretPrefab)
    {
        currentState = PlayerActionState.PlacingTurret;
        turretPrefabToPlace = turretPrefab;
        ShowInfoText("Click on a turret slot to build. (Right-click to cancel)");
    }

    // �ͷ� �Ǹ� ��ư���� ȣ��
    public void EnterTurretSellMode()
    {
        currentState = PlayerActionState.SellingTurret;
        turretPrefabToPlace = null;
        ShowInfoText("Select a turret to sell. (Right-click to cancel)");
    }

    // �ͷ� �Ǽ�/�Ǹ� ���� �Ǵ� ��� �� ȣ��
    public void CancelPlayerAction()
    {
        currentState = PlayerActionState.None;
        turretPrefabToPlace = null;
        HideInfoText();
    }

    // �ͷ� ���� �߰� ��ư���� ȣ��
    public void OnClick_AddTurretSlotButton()
    {
        /*int slotCost = 100; // ����� InGameManager�� �ٸ� ������ ��Ʈ���� �����ϴ� ���� �� ����
        if (InGameManager.Instance.SpendGold(slotCost))
        {
            ShowInfoText("Turret Slot Added!");
            // TODO: BaseController�� ���� �߰��� ��û�ϴ� ����
            // ��: InGameManager.Instance.GetPlayerBase().AddNewSlot();
        }
        else
        {
            ShowInfoText("Not Enough Gold to Add Turret Slot!");
        }*/
        var baseCtrl = InGameManager.Instance.GetLocalPlayerBase();
        if (baseCtrl != null)
            baseCtrl.UnlockNextTurretSlot(100); // 슬롯 해금 비용 100
    }
    
    
    private void ShowWinnerPanel()
    {
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(true);
        }
    }
    private void ShowLoserPanel()
    {
        if (loserPanel != null)
        {
            loserPanel.SetActive(true);
        }
    }

    public void ReturnToLobby()
    {
        // ������ ���� ������ �� �����Ƿ� �ð��� �ٽ� �帣�� �մϴ�.
        Time.timeScale = 1f;

        // ���� ���� ��� �гε��� ��Ȱ��ȭ�մϴ�.
        if (winnerPanel != null) winnerPanel.SetActive(false);
        if (loserPanel != null) loserPanel.SetActive(false);

        Debug.Log("�κ�� ���ư��� ���� PhotonManager ȣ��.");

        // PhotonManager���� ���� ������ �κ� ���� �ε��϶�� ��û�մϴ�.
        if (PhotonManager.Instance != null)
        {
            // �Լ� �̸��� LeaveRoomAndLoadLobby �� �� ��Ȯ�ϰ� �ٲ㵵 �����ϴ�.
            PhotonManager.Instance.LeaveRoomAndRejoinLobby();
        }
        else
        {
            Debug.LogError("PhotonManager�� ã�� �� �����ϴ�! �������� LobbyScene�� �ε��մϴ�.");
            // PhotonManager�� ���� ��� ��Ȳ�� ����� ���� ���� �ε��մϴ�.
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene"); // "LobbyScene"�� ���� �� �̸����� �����ؾ� �մϴ�.
        }
    }

    #endregion
}
