using KYG;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PHK
{
    public class UnitPanelManager : MonoBehaviour
    {
        public AgeType age;

        [Header("�ô� ������ (���� ����)")]
        [SerializeField] private AgeData[] ageDataArray;
        private Dictionary<AgeType, AgeData> ageDataDict;

        private enum ActivePanelType { Selection, Units, Turrets }
        [Header("���� ����")]
        public AgeType currentAge = AgeType.Ancient;
        private ActivePanelType activePanel = ActivePanelType.Selection;

        [Header("���� �г�")]
        public GameObject selectPanel;

        [Header("���� �г�")]
        public GameObject ancientUnitPanel;
        public GameObject middleUnitPanel;
        public GameObject modernUnitPanel;
        public GameObject futureUnitPanel;

        [Header("���� �ô� ����")]
        public GameObject finalUnitPrefab;

        [Header("�ô뺰 �ͷ� �г�")]
        public GameObject ancientTurretPanel;
        public GameObject middleAgeTurretPanel;
        public GameObject modernTurretPanel;
        public GameObject futureTurretPanel;

        [Header("�ô� ���� ��ư")]
        public Button evolveButton;
        // --- �߰��� �κ� ---
        // ������ �ô뿡 Ȱ��ȭ�� ���� ���� ��ư (Unity �����Ϳ��� ���� �ʿ�)
        public Button lastAgeUnitButton;

        void Start()
        {
            ageDataDict = new Dictionary<AgeType, AgeData>();
            foreach (var data in ageDataArray)
            {
                if (data != null) ageDataDict[data.ageType] = data;
            }

            if (ageDataDict.TryGetValue(currentAge, out AgeData initialAgeData))
            {
                Debug.Log($"[UnitPanelManager.Start] ���� �ô�({currentAge})�� ��ư ������ �����մϴ�.");
                UpdateAge(initialAgeData);
            }
            else
            {
                Debug.LogError($"[UnitPanelManager.Start] ���� �ô�({currentAge})�� �ش��ϴ� AgeData�� ã�� �� �����ϴ�!");
            }

            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();

            // --- �߰��� �κ� ---
            // ���� �� ���� ���� ��ư�� �׻� ��Ȱ��ȭ
            if (lastAgeUnitButton != null)
            {
                lastAgeUnitButton.gameObject.SetActive(false);
            }
        }

        public void UpdateAge(AgeData newAgeData)
        {
            SoundManager.Instance.PlayEvolveSound();
            Debug.Log($"[2. UnitPanelManager] UpdateAge ȣ���. ���� ��: {newAgeData.spawnableUnits.Count}");
            this.currentAge = newAgeData.ageType;

            UpdateUnitPanelVisibility();

            // --- ����/�߰��� �κ� ---
            // �ô뿡 ���� �ô� ���� ��ư�� ���� ���� ��ư�� Ȱ��ȭ ���¸� ����
            if (this.currentAge == AgeType.Modern) // Modern�� ������ �ô��� ����
            {
                if (evolveButton != null) evolveButton.gameObject.SetActive(false);
                if (lastAgeUnitButton != null)
                {
                    lastAgeUnitButton.gameObject.SetActive(true);

                    // --- �߰��� �κ� ---
                    // ���� ���� ��ư�� �ִ� UnitButton ������Ʈ�� ã�� �ʱ�ȭ
                    UnitButton finalUnitBtnScript = lastAgeUnitButton.GetComponent<UnitButton>();
                    if (finalUnitBtnScript != null && finalUnitPrefab != null)
                    {
                        finalUnitBtnScript.Init(finalUnitPrefab);
                    }
                    else
                    {
                        Debug.LogError("lastAgeUnitButton�� UnitButton ��ũ��Ʈ�� ���ų� finalUnitPrefab�� ������� �ʾҽ��ϴ�!");
                    }
                }
            }
            else
            {
                if (evolveButton != null) evolveButton.gameObject.SetActive(true);
                if (lastAgeUnitButton != null) lastAgeUnitButton.gameObject.SetActive(false);
            }

            GameObject currentUnitPanel = null;
            switch (this.currentAge)
            {
                case AgeType.Ancient: currentUnitPanel = ancientUnitPanel; break;
                case AgeType.Medieval: currentUnitPanel = middleUnitPanel; break;
                case AgeType.Modern: currentUnitPanel = modernUnitPanel; break;
            }

            if (currentUnitPanel != null)
            {
                ConfigureUnitPanel(currentUnitPanel, newAgeData.spawnableUnits);
            }

            GameObject currentTurretPanel = null;
            switch (this.currentAge)
            {
                case AgeType.Ancient: currentTurretPanel = ancientTurretPanel; break;
                case AgeType.Medieval: currentTurretPanel = middleAgeTurretPanel; break;
                case AgeType.Modern: currentTurretPanel = modernTurretPanel; break;
            }

            if (currentTurretPanel != null)
            {
                ConfigureTurretPanel(currentTurretPanel, newAgeData.availableTurrets);
            }

            Debug.Log($"�ô밡 {newAgeData.ageType}(��)�� ����ǰ� ����/�ͷ� ��ư�� �缳���Ǿ����ϴ�.");
        }

        private void ConfigureUnitPanel(GameObject panel, List<GameObject> units)
        {
            UnitButton[] buttons = panel.GetComponentsInChildren<UnitButton>(true);
            Debug.Log($"[3. ConfigureUnitPanel] �г�({panel.name}) ���� ����. ���޵� ���� ��: {units.Count}, ã�� ��ư ��: {buttons.Length}");

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i < units.Count)
                {
                    buttons[i].Init(units[i]);
                }
                else
                {
                    buttons[i].Init(null);
                }
            }
        }

        private void ConfigureTurretPanel(GameObject panel, List<TurretData> turrets)
        {
            TurretButton[] buttons = panel.GetComponentsInChildren<TurretButton>(true);
            Debug.Log($"[3. ConfigureTurretPanel] �г�({panel.name}) ���� ����. ���޵� �ͷ� ��: {turrets.Count}, ã�� ��ư ��: {buttons.Length}");

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i < turrets.Count)
                {
                    buttons[i].Init(turrets[i]);
                }
                else
                {
                    buttons[i].Init(null);
                }
            }
        }

        private void UpdateUnitPanelVisibility()
        {
            // ��� �г� ��Ȱ��ȭ
            selectPanel.SetActive(false);
            ancientUnitPanel.SetActive(false);
            middleUnitPanel.SetActive(false);
            modernUnitPanel.SetActive(false);
            // futureUnitPanel.SetActive(false);

            if (ancientTurretPanel != null) ancientTurretPanel.SetActive(false);
            if (middleAgeTurretPanel != null) middleAgeTurretPanel.SetActive(false);
            if (modernTurretPanel != null) modernTurretPanel.SetActive(false);
            // if (futureTurretPanel != null) futureTurretPanel.SetActive(false);

            // ���� ���¿� �ش��ϴ� �г� Ȱ��ȭ
            switch (activePanel)
            {
                case ActivePanelType.Selection:
                    SoundManager.Instance.PlayUIClick();
                    selectPanel.SetActive(true);
                    break;

                case ActivePanelType.Units:
                    switch (currentAge)
                    {
                        case AgeType.Ancient:
                            ancientUnitPanel.SetActive(true);
                            SoundManager.Instance.PlayUIClick(); break;
                        case AgeType.Medieval:
                            middleUnitPanel.SetActive(true);
                            SoundManager.Instance.PlayUIClick(); break;
                        case AgeType.Modern:
                            modernUnitPanel.SetActive(true);
                            SoundManager.Instance.PlayUIClick(); break;
                    }
                    break;

                case ActivePanelType.Turrets:
                    switch (currentAge)
                    {
                        case AgeType.Ancient:
                            if (ancientTurretPanel != null) ancientTurretPanel.SetActive(true);
                            SoundManager.Instance.PlayUIClick(); break;
                        case AgeType.Medieval:
                            if (middleAgeTurretPanel != null) middleAgeTurretPanel.SetActive(true);
                            SoundManager.Instance.PlayUIClick(); break;
                        case AgeType.Modern:
                            if (modernTurretPanel != null) modernTurretPanel.SetActive(true);
                            SoundManager.Instance.PlayUIClick(); break;
                    }
                    break;
            }
        }

        // 'Units' ��ư�� ������ ȣ��
        public void ShowUnitPnale()
        {
            SoundManager.Instance.PlayUIClick();
            activePanel = ActivePanelType.Units;
            UpdateUnitPanelVisibility();
        }

        // 'Turrets' ��ư�� ������ ȣ��
        public void ShowTurretPanel()
        {
            SoundManager.Instance.PlayUIClick();
            activePanel = ActivePanelType.Turrets;
            UpdateUnitPanelVisibility();
        }

        // ����/�ͷ� �г��� '���ư���' ��ư�� ������ selection �гη� ���ư�
        public void ShowSelectionPanel()
        {
            SoundManager.Instance.PlayUIClick();
            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();
        }
    }
}