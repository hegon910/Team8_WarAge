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
        }
        
        /// <summary>
        /// 터렛 설치
        /// </summary>
        public void InstallTurret(TurretData data)
        {
            if (!IsEmpty) return;

            if (data == null || data.turretPrefab == null)
            {
                Debug.LogError("TurretData가 비어 있거나 turretPrefab이 없습니다!");
                return;
            }

            GameObject turretObj;

            // 디버그 모드 혹은 Photon 미연결 상태 → Instantiate
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

            // 터렛 초기화 시 TeamTag 전달
            currentTurret.Init(data, this, TeamTag);
        }

        public void SellTurret() // 터렛 판매
        {
            if (IsEmpty) return;
            
            // PhotonNetwork로 터렛 제거 (모든 클라이언트에 반영)
            PhotonNetwork.Destroy(currentTurret.gameObject);
            currentTurret = null;
            // TODO UI 버튼 연동
        }
        /// <summary>
        /// 슬롯 클릭 시 설치 모드 확인 후 설치
        /// </summary>
        private void OnMouseDown()
        {
            if (InGameUIManager.Instance.currentState == InGameUIManager.PlayerActionState.PlacingTurret)
            {
                TurretData data = InGameUIManager.Instance.turretDataToPlace;
                if (data != null && InGameManager.Instance.SpendGold(data.cost))
                {
                    InstallTurret(data);
                }
            }
            else if (InGameUIManager.Instance.currentState == InGameUIManager.PlayerActionState.SellingTurret)
            {
                SellTurret();
            }
        }

    
}
}
