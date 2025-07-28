using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.EventSystems;
using Photon.Pun;

namespace PHK
{
    //유닛 버튼에 붙어서 마우스 호버 이벤트 처리
    public class UnitButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("유닛데이터")]
        public GameObject unitPrefab;
        private Unit unitData; // 유닛 스크립터블 오브젝트 데이터

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(SpawnUnit);
        }
        public void Init(GameObject prefab)
        {
            unitPrefab = prefab;
            if (unitPrefab != null)
            {
                unitData = unitPrefab.GetComponent<UnitController>().unitdata;
                gameObject.SetActive(true); // 유닛 프리팹이 설정되면 버튼 활성화
            }
            else
            {
                gameObject.SetActive(false); // 유닛 프리팹이 없으면 버튼 비활성화
            }
        }

        //마우스가 버튼 위에 올라갔을 때 호출되는 이벤트
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (unitData != null)
            {
                string info = $"{unitData.unitName} Cost : {unitData.goldCost}";
                //인게임 UI 매니저를 통해 유닛 정보 표시
                InGameUIManager.Instance.ShowInfoText(info);
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
            if (unitPrefab == null) return;

            //현재 클라이언트가 마스터 클라이언트인지 확인
            string ownerTag = PhotonNetwork.IsMasterClient ? "P1" : "P2";
            if (InGameManager.Instance.isDebugMode)
            {
                // isDebugHost 값에 따라 P1 또는 P2로 태그를 설정
                ownerTag = InGameManager.Instance.isDebugHost ? "P1" : "P2";
            }
            else // 실제 네트워크 환경일 경우
            {
                ownerTag = PhotonNetwork.IsMasterClient ? "P1" : "P2";
            }
            UnitSpawnManager.Instance.RequestUnitProduction(unitPrefab, ownerTag);
        }
    }
}
