using KYG;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PHK
{
    public class UnitPanelManager : MonoBehaviour
    {
        public AgeType age;

        [Header("시대 데이터 (직접 연결)")]
        [SerializeField] private AgeData[] ageDataArray;
        private Dictionary<AgeType, AgeData> ageDataDict;

        private enum ActivePanelType { Selection, Units, Turrets }
        [Header("현재 상태")]
        public AgeType currentAge = AgeType.Ancient;
        private ActivePanelType activePanel = ActivePanelType.Selection;

        [Header("공용 패널")]
        public GameObject selectPanel;

        [Header("유닛 패널")]
        public GameObject ancientUnitPanel;
        public GameObject middleUnitPanel;
        public GameObject modernUnitPanel;
        public GameObject futureUnitPanel;

        [Header("최종 시대 전용")]
        public GameObject finalUnitPrefab;

        [Header("시대별 터렛 패널")]
        public GameObject ancientTurretPanel;
        public GameObject middleAgeTurretPanel;
        public GameObject modernTurretPanel;
        public GameObject futureTurretPanel;

        [Header("시대 발전 버튼")]
        public Button evolveButton;
        // --- 추가된 부분 ---
        // 마지막 시대에 활성화될 최종 유닛 버튼 (Unity 에디터에서 연결 필요)
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
                Debug.Log($"[UnitPanelManager.Start] 시작 시대({currentAge})의 버튼 설정을 시작합니다.");
                UpdateAge(initialAgeData);
            }
            else
            {
                Debug.LogError($"[UnitPanelManager.Start] 시작 시대({currentAge})에 해당하는 AgeData를 찾을 수 없습니다!");
            }

            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();

            // --- 추가된 부분 ---
            // 시작 시 최종 유닛 버튼은 항상 비활성화
            if (lastAgeUnitButton != null)
            {
                lastAgeUnitButton.gameObject.SetActive(false);
            }
        }

        public void UpdateAge(AgeData newAgeData)
        {
            SoundManager.Instance.PlayEvolveSound();
            Debug.Log($"[2. UnitPanelManager] UpdateAge 호출됨. 유닛 수: {newAgeData.spawnableUnits.Count}");
            this.currentAge = newAgeData.ageType;

            UpdateUnitPanelVisibility();

            // --- 수정/추가된 부분 ---
            // 시대에 따라 시대 발전 버튼과 최종 유닛 버튼의 활성화 상태를 관리
            if (this.currentAge == AgeType.Modern) // Modern이 마지막 시대라고 가정
            {
                if (evolveButton != null) evolveButton.gameObject.SetActive(false);
                if (lastAgeUnitButton != null)
                {
                    lastAgeUnitButton.gameObject.SetActive(true);

                    // --- 추가된 부분 ---
                    // 최종 유닛 버튼에 있는 UnitButton 컴포넌트를 찾아 초기화
                    UnitButton finalUnitBtnScript = lastAgeUnitButton.GetComponent<UnitButton>();
                    if (finalUnitBtnScript != null && finalUnitPrefab != null)
                    {
                        finalUnitBtnScript.Init(finalUnitPrefab);
                    }
                    else
                    {
                        Debug.LogError("lastAgeUnitButton에 UnitButton 스크립트가 없거나 finalUnitPrefab이 연결되지 않았습니다!");
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

            Debug.Log($"시대가 {newAgeData.ageType}(으)로 변경되고 유닛/터렛 버튼이 재설정되었습니다.");
        }

        private void ConfigureUnitPanel(GameObject panel, List<GameObject> units)
        {
            UnitButton[] buttons = panel.GetComponentsInChildren<UnitButton>(true);
            Debug.Log($"[3. ConfigureUnitPanel] 패널({panel.name}) 설정 시작. 전달된 유닛 수: {units.Count}, 찾은 버튼 수: {buttons.Length}");

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
            Debug.Log($"[3. ConfigureTurretPanel] 패널({panel.name}) 설정 시작. 전달된 터렛 수: {turrets.Count}, 찾은 버튼 수: {buttons.Length}");

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
            // 모든 패널 비활성화
            selectPanel.SetActive(false);
            ancientUnitPanel.SetActive(false);
            middleUnitPanel.SetActive(false);
            modernUnitPanel.SetActive(false);
            // futureUnitPanel.SetActive(false);

            if (ancientTurretPanel != null) ancientTurretPanel.SetActive(false);
            if (middleAgeTurretPanel != null) middleAgeTurretPanel.SetActive(false);
            if (modernTurretPanel != null) modernTurretPanel.SetActive(false);
            // if (futureTurretPanel != null) futureTurretPanel.SetActive(false);

            // 현재 상태에 해당하는 패널 활성화
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

        // 'Units' 버튼을 누르면 호출
        public void ShowUnitPnale()
        {
            SoundManager.Instance.PlayUIClick();
            activePanel = ActivePanelType.Units;
            UpdateUnitPanelVisibility();
        }

        // 'Turrets' 버튼을 누르면 호출
        public void ShowTurretPanel()
        {
            SoundManager.Instance.PlayUIClick();
            activePanel = ActivePanelType.Turrets;
            UpdateUnitPanelVisibility();
        }

        // 유닛/터렛 패널의 '돌아가기' 버튼을 누르면 selection 패널로 돌아감
        public void ShowSelectionPanel()
        {
            SoundManager.Instance.PlayUIClick();
            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();
        }
    }
}