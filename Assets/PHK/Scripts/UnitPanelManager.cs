using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using KYG;
using Unity.VisualScripting;

namespace PHK
{
    // �ΰ����� ����/�ͷ� �г� UI�� �����ϴ� �Ŵ��� ��ũ��Ʈ
    // �ô� ������ ���� ����/�ͷ� �г��� ��ü�ϰ� ��ư�� �����ϴ� ������ ���
    public class UnitPanelManager : MonoBehaviour
    {
        // �ô� (Age)�� ��Ȯ�ϰ� �����ϱ� ���� ������ (enum)���� ����
        public AgeType age;

        [Header("�ô� ������ (���� ����)")]
        [SerializeField] private AgeData[] ageDataArray;
        private Dictionary<AgeType, AgeData> ageDataDict;

        // ���� Ȱ��ȭ�� �г��� ������ �����ϱ� ���� ������
        private enum ActivePanelType
        {
            Selection,
            Units,
            Turrets,
        }

        // ���� ������ �ô븦 �����ϴ� ����, ������ '���'
        [Header("���� ����")]
        public AgeType currentAge = AgeType.Ancient; // ���� �ô븦 �����ϴ� ����, ������ '���'
        private ActivePanelType activePanel = ActivePanelType.Selection;

        [Header("���� �г�")]
        public GameObject selectPanel; //"Units", "Turrets" ��ư�� �ִ� �ʱ� ���� �г�

        // ����Ƽ �����Ϳ��� �� �ô뿡 �´� ���� �г��� ����
        [Header("���� �г�")]
        public GameObject ancientUnitPanel;
        public GameObject middleUnitPanel;
        public GameObject modernUnitPanel;
        public GameObject futureUnitPanel; // ���� ���ٸ�

        [Header("�ô뺰 �ͷ� �г�")]
        public GameObject ancientTurretPanel;
        public GameObject middleAgeTurretPanel;
        public GameObject modernTurretPanel;
        public GameObject futureTurretPanel;

        // �ô� ���� ��ư 
        [Header("�ô� ���� ��ư")]
        public Button evolveButton;
        // ������ �ô��� ��� ���� ��ư�� ��Ȱ��ȭ�ϰ� �ش� �������� ��ü
        public Button lastAgeUnitButton;

        void Start()
        {
            // 1. �� ��ũ��Ʈ�� ���� ���� ageDataArray�� ��ųʸ��� ����
            ageDataDict = new Dictionary<AgeType, AgeData>();
            foreach (var data in ageDataArray)
            {
                if (data != null)
                {
                    ageDataDict[data.ageType] = data;
                }
            }

            // 2. ���� �ô�(currentAge, �⺻�� Ancient)�� �ش��ϴ� AgeData�� ��ųʸ����� ���� �˻�
            if (ageDataDict.TryGetValue(currentAge, out AgeData initialAgeData))
            {
                // 3. ã�� �����͸� ������ UI ������Ʈ �� ��ư ���� �Լ��� '���' ȣ��
                //    ScriptableObject -> UnitButton�� ���� �������� ����
                Debug.Log($"[UnitPanelManager.Start] ���� �ô�({currentAge})�� ��ư ������ �����մϴ�.");
                UpdateAge(initialAgeData);
            }
            else
            {
                Debug.LogError($"[UnitPanelManager.Start] ���� �ô�({currentAge})�� �ش��ϴ� AgeData�� ã�� �� �����ϴ�! Age Data Array�� Ȯ�����ּ���.");
            }

            // 4. ���� �� ����/�ͷ� ���� �г��� Ȱ��ȭ
            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();
        }

        void Update()
        {

        }

        public void UpdateAge(AgeData newAgeData)
        {
            Debug.Log($"[2. UnitPanelManager] UpdateAge ȣ���. ���� ��: {newAgeData.spawnableUnits.Count}");
            // 1. ���� �ô� ���� ������Ʈ (����ȯ ���� ���� ����)
            this.currentAge = newAgeData.ageType;

            // 2. UI ���ü� ������Ʈ
            UpdateUnitPanelVisibility();

            // 3. ���� �ô뿡 �´� ���� �г� ã��
            GameObject currentUnitPanel = null;
            switch (this.currentAge)
            {
                case AgeType.Ancient: currentUnitPanel = ancientUnitPanel; break;
                case AgeType.Medieval: currentUnitPanel = middleUnitPanel; break;
                case AgeType.Modern: currentUnitPanel = modernUnitPanel; break;
                    // case AgeType.Future: currentUnitPanel = futureUnitPanel; break; // Future �ô� �߰� �� �ּ� ����
            }

            // 4. ã�� �г��� ���� ��ư�� ����
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
                // ���� �߰��� �ͷ� ���� �Լ��� ȣ���մϴ�.
                ConfigureTurretPanel(currentTurretPanel, newAgeData.availableTurrets);
            }
            // --- ������� �߰� ---

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
            // `TurretButton`�� ã���� ����
            TurretButton[] buttons = panel.GetComponentsInChildren<TurretButton>(true);
            Debug.Log($"[3. ConfigureTurretPanel] �г�({panel.name}) ���� ����. ���޵� �ͷ� ��: {turrets.Count}, ã�� ��ư ��: {buttons.Length}");

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i < turrets.Count)
                {
                    // TurretData�� �Ѱ��ֵ��� ����
                    buttons[i].Init(turrets[i]);
                }
                else
                {
                    buttons[i].Init(null);
                }
            }
        }
        // ���� Ȱ��ȭ�� �г� Ÿ��(activePanel)�� �ô�(currentAge)�� �´� �г��� �����ִ� �Լ�
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
                    selectPanel.SetActive(true);
                    break;

                case ActivePanelType.Units:
                    switch (currentAge)
                    {
                        case AgeType.Ancient: ancientUnitPanel.SetActive(true); break;
                        case AgeType.Medieval: middleUnitPanel.SetActive(true); break;
                        case AgeType.Modern: modernUnitPanel.SetActive(true); break;
                    }
                    break;

                case ActivePanelType.Turrets:
                    switch (currentAge)
                    {
                        case AgeType.Ancient: if (ancientTurretPanel != null) ancientTurretPanel.SetActive(true); break;
                        case AgeType.Medieval: if (middleAgeTurretPanel != null) middleAgeTurretPanel.SetActive(true); break;
                        case AgeType.Modern: if (modernTurretPanel != null) modernTurretPanel.SetActive(true); break;
                    }
                    break;
            }
        }

        // 'Units' ��ư�� ������ ȣ��
        public void ShowUnitPnale()
        {
            // ���� �г� Ȱ��ȭ
            activePanel = ActivePanelType.Units;
            UpdateUnitPanelVisibility();
        }

        // 'Turrets' ��ư�� ������ ȣ��
        public void ShowTurretPanel()
        {
            // �ͷ� �г� Ȱ��ȭ
            activePanel = ActivePanelType.Turrets;
            UpdateUnitPanelVisibility();
        }

        // ����/�ͷ� �г��� '���ư���' ��ư�� ������ selection �гη� ���ư�
        public void ShowSelectionPanel()
        {
            // �ʱ� ���� �г� Ȱ��ȭ
            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();
        }

    }

}
