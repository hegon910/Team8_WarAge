using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;


namespace PHK
{
    //인게임의 전반적인 UI를 관리하는 매니저 스크립트
    //시대 발전, 자원표시, 유닛 UI관리등을 담당
    public class UnitPanelManager : MonoBehaviour
    {
        //시대 (Age)를 명확하게 구분하기 위해 열거형 (enum)으로 정의
        public enum Age
        {
            Ancient,
            Middle,
            Modern,
            Future//혹시나 미래 시대가 추가될 경우를 대비하여 Future 추가

        }

        // 현재 활성화된 패널의 종류를 추적하기 위한 열거형
        private enum ActivePanelType
        {
            Selection,
            Units,
            Turrets,
        }

        // 현재 게임의 시대를 저장하는 변수, 시작은 '고대'
        [Header("현재상태")]
        public Age currentAge = Age.Ancient;
        private ActivePanelType activePanel = ActivePanelType.Selection;

        [Header("공용패널")]
        public GameObject selectPanel; //"Units", "Turrets" 버튼이 있는 초기 선택 패널


        //유니티 에디터에서 각 시대에 맞는 유닛 패널을 연결
        [Header("유닛 패널")]
        public GameObject ancientUnitPanel;
        public GameObject middleUnitPanel;
        public GameObject modernUnitPanel;
        public GameObject futureUnitPanel; //만일 쓴다면

        [Header("시대별 터렛 패널")]
        public GameObject ancientTurretPanel;
        public GameObject middleAgeTurretPanel;
        public GameObject modernTurretPanel;
        public GameObject futureTurretPanel;

        //시대 발전 버튼 
        [Header("시대 발전 버튼")]
        public Button evolveButton;
        //마지막 시대일 경우 발전 버튼을 비활성화하고 해당 유닛으로 대체
        public Button lastAgeUnitButton;

        void Start()
        {
            //게임 시작 시, 유닛 생성은 고대 ui 패널로 시작
            UpdateUnitPanelVisibility();
            activePanel = ActivePanelType.Selection;
        }
        void Update()
        {

        }

        //현재 시대에 맞는 패널 함수
        private void UpdateUnitPanelVisibility()
        {
            //모든 패널 비활성화
            selectPanel.SetActive(false);
            ancientUnitPanel.SetActive(false);
            middleUnitPanel.SetActive(false);
            modernUnitPanel.SetActive(false);
            futureUnitPanel.SetActive(false);
            //터렛 패널 비활성화
            if (ancientTurretPanel != null) ancientTurretPanel.SetActive(false);
            if (middleAgeTurretPanel != null) middleAgeTurretPanel.SetActive(false);
            if (modernTurretPanel != null) modernTurretPanel.SetActive(false);
            if (futureTurretPanel != null) futureTurretPanel.SetActive(false);

            //현재 상태에 해당하는 패널 활성화
            switch (activePanel)
            {
                case ActivePanelType.Selection:
                    selectPanel.SetActive(true);
                    break;

                case ActivePanelType.Units:
                    // 현재 시대에 맞는 유닛 패널을 켭니다.
                    switch (currentAge)
                    {
                        case Age.Ancient: ancientUnitPanel.SetActive(true); break;
                        case Age.Middle: middleUnitPanel.SetActive(true); break;
                        case Age.Modern: modernUnitPanel.SetActive(true); break;
                        case Age.Future: futureUnitPanel.SetActive(true); break;
                    }
                    break;

                case ActivePanelType.Turrets:
                    // 현재 시대에 맞는 터렛 패널을 켭니다.
                    switch (currentAge)
                    {
                        case Age.Ancient: if (ancientTurretPanel != null) ancientTurretPanel.SetActive(true); break;
                        case Age.Middle: if (middleAgeTurretPanel != null) middleAgeTurretPanel.SetActive(true); break;
                        case Age.Modern: if (modernTurretPanel != null) modernTurretPanel.SetActive(true); break;
                        case Age.Future: if (futureTurretPanel != null)
                            {
                                futureTurretPanel.SetActive(true);
                                evolveButton.interactable = false; //미래 시대는 발전 버튼 비활성화
                                evolveButton.gameObject.SetActive(false); //버튼 자체를 비활성화
                            } break;
                    }
                    break;
            }

            Debug.Log("현재 시대: " + currentAge.ToString() + " 패널 활성화됨");
        }

        //Unit 버튼 누르면 호출
        public void ShowUnitPnale()
        {
            //유닛 패널 활성화
            activePanel = ActivePanelType.Units;
            UpdateUnitPanelVisibility();
        }
        //Turret 버튼 누르면 호출
        public void ShowTurretPanel()
        {
            //터렛 패널 활성화
            activePanel = ActivePanelType.Turrets;
            UpdateUnitPanelVisibility();
        }
        //유닛/터렛 패널의 돌아가기 버튼 누르면 selection 패널로 돌아감
        public void ShowSelectionPanel()
        {
            //초기 선택 패널 활성화
            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();
        }

        // 시대를 다음 단계로 넘기는 대신, 특정 시대로 직접 설정하는 함수
        public void SetAge(Age newAge)
        {
            // 마지막 시대보다 높은 값으로 설정되지 않도록 방지
            if (newAge <= Age.Future) // Age enum의 마지막 값
            {
                currentAge = newAge;
                UpdateUnitPanelVisibility(); // 해당 시점에 맞는 패널을 보여줌
                Debug.Log($"시대가 {newAge}로 직접 설정되었습니다.");
            }
        }

        // 시대 발전 버튼에 연결할 공용 함수, 다음 시대로 발전시키는 역할
        public void EvolveToNextAge()
        {
            Debug.Log($"EvolveToNextAge() 실행됨. 실제 UnitPanelManager ID: <color=lime>{GetInstanceID()}</color>");
            //마지막 시대가 아닐 경우에만 시대를 발전
            if (currentAge < Age.Future)
            {
                currentAge++;
                UpdateUnitPanelVisibility();
            }
            else
            {
                //마지막 시대일 경우 더이상 발전 할 수 없음.
                //발전 버튼 비활성화,
                evolveButton.interactable = false;
                evolveButton.gameObject.SetActive(false);
                //대신 마지막 시대 유닛 버튼 활성화
                lastAgeUnitButton.gameObject.SetActive(true);

            }
        }

    }

}