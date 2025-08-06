using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace KYG
{
    
public class TurretSlot : MonoBehaviourPun //  터렛 설치 장소 및 판매,취소 관리
{
        private TurretController currentTurret; // 현재 설치된 터렛
        public string TeamTag { get; private set; }
        
        public bool IsEmpty => currentTurret == null; // 현재 설치된 터렛이 없는지 확인
        
        /// <summary>
        /// BaseController에서 슬롯 활성화 시 호출되어 팀 태그 설정
        /// </summary>
        public void Init(string teamTag)
        {
            TeamTag = teamTag;
            Debug.Log($"슬롯 {gameObject.name}의 TeamTag가 '{this.TeamTag}' (으)로 설정되었습니다!");
        }

        /// <summary>
        /// 터렛 설치
        /// </summary>
        public void InstallTurret(TurretData data, string ownerTeamTag) 
        {
            Debug.Log($"슬롯 {gameObject.name}이(가) 터렛 설치를 시도합니다. 설치 주체 팀: '{ownerTeamTag}'");
            if (!IsEmpty) return;

            if (data == null || data.turretPrefab == null)
            {
                Debug.LogError("TurretData가 비어 있거나 turretPrefab이 없습니다!");
                return;
            }

            GameObject turretObj;

            // ... (Instantiate 또는 PhotonNetwork.Instantiate 로직은 그대로) ...
            if (InGameManager.Instance.isDebugMode || !PhotonNetwork.IsConnected)
            {
                turretObj = Instantiate(data.turretPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                turretObj = PhotonNetwork.Instantiate(data.turretPrefab.name, transform.position, Quaternion.identity);
            }

            currentTurret = turretObj.GetComponent<TurretController>();
            if (currentTurret == null)
            {
                Debug.LogError("TurretController가 터렛 프리팹에 없습니다!");
                return;
            }

            // OnMouseDown에서 전달받은 '플레이어의 팀 태그(ownerTeamTag)'로 터렛을 초기화합니다.
            currentTurret.Init(data, this, ownerTeamTag);
        }


        public void SellTurret() // 터렛 판매
        {
            if (IsEmpty) return;
           
            InGameManager.Instance.AddGold(currentTurret.data.sellPrice);
            // PhotonNetwork로 터렛 제거 (모든 클라이언트에 반영)
            PhotonNetwork.Destroy(currentTurret.gameObject);
            currentTurret = null;
            InGameUIManager.Instance.CancelPlayerAction();
        }
        /// <summary>
        /// 슬롯 클릭 시 설치 모드 확인 후 설치
        /// </summary>
        private void OnMouseDown()
        {
            // 설치 모드가 아니면 즉시 중단
            if (InGameUIManager.Instance.currentState != InGameUIManager.PlayerActionState.PlacingTurret)
            {
                return;
            }

            // 로컬 플레이어의 팀 정보를 가져옴
            string localPlayerTeamTag = InGameManager.Instance.GetLocalPlayerBaseTag();
            if (string.IsNullOrEmpty(localPlayerTeamTag))
            {
                Debug.LogError("InGameManager에서 플레이어의 팀 정보를 가져오지 못했습니다!");
                return;
            }

            // [핵심 추가] 이 슬롯의 팀(this.TeamTag)과 클릭한 플레이어의 팀(localPlayerTeamTag)이 다른 경우, 설치를 막습니다.
            if (this.TeamTag != localPlayerTeamTag)
            {
                InGameUIManager.Instance.ShowInfoText("다른 플레이어의 슬롯에는 건설할 수 없습니다.");
                return;
            }

            // 내 슬롯이 맞을 경우에만 아래의 설치 로직을 진행
            TurretData dataToPlace = InGameUIManager.Instance.turretDataToPlace;
            if (dataToPlace != null && IsEmpty && InGameManager.Instance.SpendGold(dataToPlace.cost))
            {
                // 터렛 설치 함수에 '내 팀 태그'를 전달
                InstallTurret(dataToPlace, localPlayerTeamTag);
                InGameUIManager.Instance.CancelPlayerAction();
            }
        }



    }
}
