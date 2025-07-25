using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestUnitSpawner : MonoBehaviour
{
    public GameObject UnitPrefab;

    public Transform spawnPointP1;  
    public Transform spawnPointP2;

    public Button spawnButton;

    private void Awake()
    {
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(OnSpawnButtonClicked);
            Debug.Log("스폰 버튼 리스너 등록.");
        }
        else
        {
            Debug.LogWarning("스폰 버튼이 TestUnitSpawner에 할당되지 않았습니다!");
        }
    }

    private void OnSpawnButtonClicked()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("방에 참여하지 않은 상태에서는 유닛을 스폰할 수 없습니다.");
            return;
        }

        Transform pointToSpawn = null;
        string playerTag = "";
        Vector3 initialMoveDirection = Vector3.zero;

        if (PhotonNetwork.IsMasterClient)
        {
            playerTag = "P1";
            initialMoveDirection = Vector3.right; 
            pointToSpawn = spawnPointP1;
        }
        else
        {
            playerTag = "P2";
            initialMoveDirection = Vector3.left;
            pointToSpawn = spawnPointP2;
        }

        if (UnitPrefab == null || pointToSpawn == null)
        {
            Debug.LogError("유닛 프리팹 또는 스폰 포인트가 할당되지 않았습니다!");
            return;
        }

        GameObject spawnedUnitGO = PhotonNetwork.Instantiate(UnitPrefab.name, pointToSpawn.position, Quaternion.identity);

        UnitController unitController = spawnedUnitGO.GetComponent<UnitController>();
        if (unitController != null)
        {
            unitController.photonView.RPC("RpcSetPlayerProps", RpcTarget.AllViaServer, playerTag, initialMoveDirection);
        }
        else
        {
            Debug.LogError("스폰된 유닛에 UnitController가 없습니다!");
        }

        Debug.Log($"버튼 클릭으로 유닛 스폰 요청됨! (현재 클라이언트: {playerTag})");
    }

}
