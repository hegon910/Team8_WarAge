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
        public Unit unitData; // 유닛 스크립터블 오브젝트 데이터

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
            
        }
    }
}
