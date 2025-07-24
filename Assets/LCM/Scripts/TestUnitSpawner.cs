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
            Debug.Log("���� ��ư ������ ���.");
        }
        else
        {
            Debug.LogWarning("���� ��ư�� TestUnitSpawner�� �Ҵ���� �ʾҽ��ϴ�!");
        }
    }

    private void OnSpawnButtonClicked()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("�濡 �������� ���� ���¿����� ������ ������ �� �����ϴ�.");
            return;
        }

        Transform pointToSpawn = null;
        string playerTag = "";
        Vector3 initialMoveDirection = Vector3.zero;

        if (PhotonNetwork.IsMasterClient)
        {
            // ������ Ŭ���̾�Ʈ (P1) ����
            playerTag = "P1";
            initialMoveDirection = Vector3.right; // P1 ������ ���������� �̵�
            pointToSpawn = spawnPointP1;
        }
        else // �Ϲ� Ŭ���̾�Ʈ (P2) ����
        {
            playerTag = "P2";
            initialMoveDirection = Vector3.left; // P2 ������ �������� �̵�
            pointToSpawn = spawnPointP2;
        }

        if (UnitPrefab == null || pointToSpawn == null)
        {
            Debug.LogError("���� ������ �Ǵ� ���� ����Ʈ�� �Ҵ���� �ʾҽ��ϴ�!");
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
            Debug.LogError("������ ���ֿ� UnitController�� �����ϴ�!");
        }

        Debug.Log($"��ư Ŭ������ ���� ���� ��û��! (���� Ŭ���̾�Ʈ: {playerTag})");
    }

}
