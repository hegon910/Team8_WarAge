using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// TestNetworkManager ��� �� ��ũ��Ʈ�� ����մϴ�.
public class InGameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("���� ��ġ")]
    public Transform p1_spawnPoint;
    public Transform p2_spawnPoint;

    void Start()
    {
        // ������ Ŭ���̾�Ʈ�� ���� ������ �����ϵ��� �Ͽ� �ߺ� ������ �����մϴ�.
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnAllBases();
        }
    }

    private void SpawnAllBases()
    {
        Player[] players = PhotonNetwork.PlayerList;

        // --- P1 (������ Ŭ���̾�Ʈ) ���� ���� ---
        if (players.Length >= 1)
        {
            // P1�� ���� ����Ʈ�� �����͸� ����մϴ�.
            float offsetDistance = 0.4f;
            Vector3 spawnPosition = p1_spawnPoint.position - (p1_spawnPoint.right * offsetDistance);
            PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP1" });
            Debug.Log("P1(Master Client)�� ������ �����߽��ϴ�.");
        }

        // --- P2 (�� ��° �÷��̾�) ���� ���� ---
        if (players.Length >= 2)
        {
            // P2�� ���� ����Ʈ�� �����͸� ����մϴ�.
            float offsetDistance = 0.4f;
            Vector3 spawnPosition = p2_spawnPoint.position - (p2_spawnPoint.right * offsetDistance);
            GameObject p2BaseObject = PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP2" });
            Debug.Log("P2�� ������ �����մϴ�.");

            // ������ P2 ������ �������� �� ��° �÷��̾�� �Ѱ��ݴϴ�.
            if (p2BaseObject != null)
            {
                p2BaseObject.GetComponent<PhotonView>().TransferOwnership(players[1]);
                Debug.Log($"P2 ������ �������� {players[1].NickName}���� �����߽��ϴ�.");
            }
        }
    }
}