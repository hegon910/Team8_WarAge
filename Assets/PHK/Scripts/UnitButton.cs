using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PHK
{
    //유닛 버튼에 붙어서 마우스 호버 이벤트 처리
    public class UnitButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("유닛데이터")]
        public GameObject unitPrefab;

        [Header("소유자 태그")]
        //public string OwnerTag;
        private bool isHost = true;
        
        
        private Unit unitData; // 유닛 스크립터블 오브젝트 데이터


        private void Awake()
        {
            //프리팹이 할당 되어 있으면 UnitContorller에서 데이터를 가져옴
            if(unitPrefab != null)
            {
                unitData = unitPrefab.GetComponent<UnitController>().unitdata;
            }
        }

        //마우스가 버튼 위에 올라갔을 때 호출되는 이벤트
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (unitData != null)
            {
                //인게임 UI 매니저를 통해 유닛 정보 표시
                InGameUIManager.Instance.ShowUnitGoldCost(unitData);
            }
        }
        //마우스 커서가 버튼 위에서 벗어 났을 때 호출
        public void OnPointerExit(PointerEventData eventData)
        {
            //인게임 UI 매니저를 통해 정보 텍스트 숨김
            InGameUIManager.Instance.HideInfoText();
        }

        //SpawnManager.cs 스크립트 받았을 때 클릭 시 유닛 생성요청을 SpawnManager로 보내는 onclick 이벤트 함수
        public void SpawnUnit()
        {
            // 이 버튼이 호스트(P1)의 소유일 때만 로직을 실행합니다.
            if (isHost)
            {
                if (unitPrefab != null)
                {
                    // 자동으로 "P1" 태그를 붙여 생성을 요청합니다.
                    InGameManager.Instance.RequestUnitProduction(unitPrefab, "P1");
                }
            }
            else
            {
                // 클라이언트(참가자)일 경우, 여기에서 호스트에게 유닛 생성을 요청하는
                // 네트워크 명령(RPC)을 보내야 합니다. 지금은 아무것도 하지 않습니다.
                Debug.Log("클라이언트에서는 유닛 생성 RPC를 호출해야 합니다.");
            }
        }
    }
}
