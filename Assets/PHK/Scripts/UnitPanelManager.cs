using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using KYG;
using Unity.VisualScripting;

namespace PHK
{
    // 인게임의 유닛/터렛 패널 UI를 관리하는 매니저 스크립트
    // 시대 발전에 따라 유닛/터렛 패널을 교체하고 버튼을 설정하는 역할을 담당
    public class UnitPanelManager : MonoBehaviour
    {
        // 시대 (Age)를 명확하게 구분하기 위해 열거형 (enum)으로 정의
        public AgeType age;

        [Header("시대 데이터 (직접 연결)")]
        [SerializeField] private AgeData[] ageDataArray;
        private Dictionary<AgeType, AgeData> ageDataDict;

        // 현재 활성화된 패널의 종류를 추적하기 위한 열거형
        private enum ActivePanelType
        {
            Selection,
            Units,
            Turrets,
        }

        // 현재 게임의 시대를 저장하는 변수, 시작은 '고대'
        [Header("현재 상태")]
        public AgeType currentAge = AgeType.Ancient; // 현재 시대를 저장하는 변수, 시작은 '고대'
        private ActivePanelType activePanel = ActivePanelType.Selection;

        [Header("공용 패널")]
        public GameObject selectPanel; //"Units", "Turrets" 버튼이 있는 초기 선택 패널

        // 유니티 에디터에서 각 시대에 맞는 유닛 패널을 연결
        [Header("유닛 패널")]
        public GameObject ancientUnitPanel;
        public GameObject middleUnitPanel;
        public GameObject modernUnitPanel;
        public GameObject futureUnitPanel; // 만일 쓴다면

        [Header("시대별 터렛 패널")]
        public GameObject ancientTurretPanel;
        public GameObject middleAgeTurretPanel;
        public GameObject modernTurretPanel;
        public GameObject futureTurretPanel;

        // 시대 발전 버튼 
        [Header("시대 발전 버튼")]
        public Button evolveButton;
        // 마지막 시대일 경우 발전 버튼을 비활성화하고 해당 유닛으로 대체
        public Button lastAgeUnitButton;

        void Start()
        {
            // 1. 이 스크립트가 직접 가진 ageDataArray로 딕셔너리를 만듦
            ageDataDict = new Dictionary<AgeType, AgeData>();
            foreach (var data in ageDataArray)
            {
                if (data != null)
                {
                    ageDataDict[data.ageType] = data;
                }
            }

            // 2. 시작 시대(currentAge, 기본값 Ancient)에 해당하는 AgeData를 딕셔너리에서 직접 검색
            if (ageDataDict.TryGetValue(currentAge, out AgeData initialAgeData))
            {
                // 3. 찾은 데이터를 가지고 UI 업데이트 및 버튼 설정 함수를 '즉시' 호출
                //    ScriptableObject -> UnitButton의 가장 직접적인 연결
                Debug.Log($"[UnitPanelManager.Start] 시작 시대({currentAge})의 버튼 설정을 시작합니다.");
                UpdateAge(initialAgeData);
            }
            else
            {
                Debug.LogError($"[UnitPanelManager.Start] 시작 시대({currentAge})에 해당하는 AgeData를 찾을 수 없습니다! Age Data Array를 확인해주세요.");
            }

            // 4. 시작 시 유닛/터렛 선택 패널을 활성화
            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();
        }

        void Update()
        {

        }

        public void UpdateAge(AgeData newAgeData)
        {
            Debug.Log($"[2. UnitPanelManager] UpdateAge 호출됨. 유닛 수: {newAgeData.spawnableUnits.Count}");
            // 1. 현재 시대 변수 업데이트 (형변환 없이 직접 대입)
            this.currentAge = newAgeData.ageType;

            // 2. UI 가시성 업데이트
            UpdateUnitPanelVisibility();

            // 3. 현재 시대에 맞는 유닛 패널 찾기
            GameObject currentUnitPanel = null;
            switch (this.currentAge)
            {
                case AgeType.Ancient: currentUnitPanel = ancientUnitPanel; break;
                case AgeType.Medieval: currentUnitPanel = middleUnitPanel; break;
                case AgeType.Modern: currentUnitPanel = modernUnitPanel; break;
                    // case AgeType.Future: currentUnitPanel = futureUnitPanel; break; // Future 시대 추가 시 주석 해제
            }

            // 4. 찾은 패널의 유닛 버튼들 설정
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
                // 새로 추가한 터렛 설정 함수를 호출합니다.
                ConfigureTurretPanel(currentTurretPanel, newAgeData.availableTurrets);
            }
            // --- 여기까지 추가 ---

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
            // `TurretButton`을 찾도록 변경
            TurretButton[] buttons = panel.GetComponentsInChildren<TurretButton>(true);
            Debug.Log($"[3. ConfigureTurretPanel] 패널({panel.name}) 설정 시작. 전달된 터렛 수: {turrets.Count}, 찾은 버튼 수: {buttons.Length}");

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i < turrets.Count)
                {
                    // TurretData를 넘겨주도록 변경
                    buttons[i].Init(turrets[i]);
                }
                else
                {
                    buttons[i].Init(null);
                }
            }
        }
        // 현재 활성화된 패널 타입(activePanel)과 시대(currentAge)에 맞는 패널을 보여주는 함수
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

        // 'Units' 버튼을 누르면 호출
        public void ShowUnitPnale()
        {
            // 유닛 패널 활성화
            activePanel = ActivePanelType.Units;
            UpdateUnitPanelVisibility();
        }

        // 'Turrets' 버튼을 누르면 호출
        public void ShowTurretPanel()
        {
            // 터렛 패널 활성화
            activePanel = ActivePanelType.Turrets;
            UpdateUnitPanelVisibility();
        }

        // 유닛/터렛 패널의 '돌아가기' 버튼을 누르면 selection 패널로 돌아감
        public void ShowSelectionPanel()
        {
            // 초기 선택 패널 활성화
            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();
        }

    }

}
