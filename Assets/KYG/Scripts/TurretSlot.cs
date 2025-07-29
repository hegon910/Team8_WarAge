using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace KYG
{
    
public class TurretSlot : MonoBehaviourPun //  터렛 설치 장소 및 판매,취소 관리
{
        private TurretController currentTurret; // 현재 설치된 터렛

        public bool IsEmpty => currentTurret == null; // 현재 설치된 터렛이 없는지 확인
        
        public void InstallTurret(TurretData data) // 터렛 설치
        {
            if (!IsEmpty) return;
            
            // PhotonNetwork로 터렛 설치 → 모든 클라이언트에 동기화됨
            GameObject turretObj =
                PhotonNetwork.Instantiate(data.turretPrefab.name, transform.position, Quaternion.identity);
                
            // 터렛 컨트롤러 초기화
                currentTurret = turretObj.GetComponent<TurretController>();
                currentTurret.Init(data, this);
                // TODO UI 버튼 연동
        }

        public void SellTurret() // 터렛 판매
        {
            if (IsEmpty) return;
            
            // PhotonNetwork로 터렛 제거 (모든 클라이언트에 반영)
            PhotonNetwork.Destroy(currentTurret.gameObject);
            currentTurret = null;
            // TODO UI 버튼 연동
        }

    
}
}
