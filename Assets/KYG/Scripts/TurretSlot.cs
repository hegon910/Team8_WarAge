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

            // ======================================================================
            // ★★★★★ 사용자 요청: "강제로 P1/P2 태그 받는 로직" 시작 ★★★★★
            // ======================================================================

            // 1. 게임의 유일한 정보 소스인 InGameManager에서 로컬 플레이어의 팀 정보를 직접 가져옵니다.
            //    이것이 가장 확실하고 다른 코드의 영향을 받지 않는 방법입니다.
            string localPlayerTeamTag = InGameManager.Instance.GetLocalPlayerBaseTag();

            // 2. 만약의 경우에 대비해, 팀 정보를 가져오지 못했다면 오류를 출력하고 중단합니다.
            if (string.IsNullOrEmpty(localPlayerTeamTag))
            {
                Debug.LogError("InGameManager에서 플레이어의 팀 정보를 가져오지 못했습니다! InGameManager의 GetLocalPlayerBaseTag() 함수를 확인하세요.");
                return;
            }

            // 3. 이제 'localPlayerTeamTag' ("BaseP1" 또는 "BaseP2")를 기준으로 모든 작업을 수행합니다.
            TurretData dataToPlace = InGameUIManager.Instance.turretDataToPlace;
            if (dataToPlace != null && IsEmpty && InGameManager.Instance.SpendGold(dataToPlace.cost))
            {
                // 4. 터렛 설치 함수에 '강제로 알아낸' 플레이어의 팀 태그를 전달합니다.
                InstallTurret(dataToPlace, localPlayerTeamTag);

                // 5. 작업 완료 후 설치 모드를 해제합니다.
                InGameUIManager.Instance.CancelPlayerAction();
            }
        }



    }
}
