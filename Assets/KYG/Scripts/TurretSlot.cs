using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using PHK;

namespace KYG
{

    public class TurretSlot : MonoBehaviourPun
    {
        private TurretController currentTurret;
        public string TeamTag { get; private set; }

        public bool IsEmpty => currentTurret == null;

        public void Init(string teamTag)
        {
            TeamTag = teamTag;
            Debug.Log($"슬롯 {gameObject.name}의 TeamTag가 '{this.TeamTag}' (으)로 설정되었습니다!");
        }

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
                if (turretObj != null) Destroy(turretObj);
                return;
            }

            currentTurret.Init(data, this, ownerTeamTag);
        }

        public void SellTurret()
        {
            if (IsEmpty) return;

            InGameManager.Instance.AddGold(currentTurret.data.sellPrice);

            if (currentTurret.photonView.IsMine || !PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Destroy(currentTurret.gameObject);
            }
            else
            {
                Destroy(currentTurret.gameObject);
            }

            currentTurret = null;
            // --- 영문으로 변경 ---
            InGameUIManager.Instance.ShowInfoText("Turret sold.");
            InGameUIManager.Instance.CancelPlayerAction();
        }

        private void OnMouseDown()
        {
            var uiManager = InGameUIManager.Instance;
            if (uiManager == null || InGameManager.Instance == null) return;

            string localPlayerTeamTag = InGameManager.Instance.GetLocalPlayerBaseTag();
            if (string.IsNullOrEmpty(localPlayerTeamTag))
            {
                Debug.LogError("로컬 플레이어의 팀 정보를 가져올 수 없습니다!");
                return;
            }

            if (uiManager.currentState == InGameUIManager.PlayerActionState.SellingTurret)
            {
                if (IsEmpty)
                {
                    // --- 영문으로 변경 ---
                    uiManager.ShowInfoText("No turret here to sell.");
                    return;
                }

                if (this.TeamTag == localPlayerTeamTag)
                {
                    SellTurret();
                }
                else
                {
                    // --- 영문으로 변경 ---
                    uiManager.ShowInfoText("You can only sell your own turrets.");
                }
                return;
            }

            if (uiManager.currentState == InGameUIManager.PlayerActionState.PlacingTurret)
            {
                if (this.TeamTag != localPlayerTeamTag)
                {
                    // --- 영문으로 변경 ---
                    uiManager.ShowInfoText("You can only build on your own slots.");
                    return;
                }

                if (!IsEmpty)
                {
                    // --- 영문으로 변경 ---
                    uiManager.ShowInfoText("This slot is already occupied.");
                    return;
                }

                TurretData dataToPlace = uiManager.turretDataToPlace;
                if (dataToPlace != null && InGameManager.Instance.SpendGold(dataToPlace.cost))
                {
                    InstallTurret(dataToPlace, localPlayerTeamTag);
                    uiManager.CancelPlayerAction();
                }
            }
        }
    }
}