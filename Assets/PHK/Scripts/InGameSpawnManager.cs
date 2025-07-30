using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// TestNetworkManager 대신 이 스크립트를 사용합니다.
public class InGameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("생성 위치")]
    public Transform p1_spawnPoint;
    public Transform p2_spawnPoint;

    void Start()
    {
        // 마스터 클라이언트만 생성 로직을 실행하도록 하여 중복 생성을 방지합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnAllBases();
        }
    }

    private void SpawnAllBases()
    {
        Player[] players = PhotonNetwork.PlayerList;

        // --- P1 (마스터 클라이언트) 기지 생성 ---
        if (players.Length >= 1)
        {
            // P1의 스폰 포인트와 데이터를 사용합니다.
            float offsetDistance = 0.4f;
            Vector3 spawnPosition = p1_spawnPoint.position - (p1_spawnPoint.right * offsetDistance);
            PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP1" });
            Debug.Log("P1(Master Client)의 기지를 생성했습니다.");
        }

        // --- P2 (두 번째 플레이어) 기지 생성 ---
        if (players.Length >= 2)
        {
            // P2의 스폰 포인트와 데이터를 사용합니다.
            float offsetDistance = 0.4f;
            Vector3 spawnPosition = p2_spawnPoint.position - (p2_spawnPoint.right * offsetDistance);
            GameObject p2BaseObject = PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP2" });
            Debug.Log("P2의 기지를 생성합니다.");

            // 생성된 P2 기지의 소유권을 두 번째 플레이어에게 넘겨줍니다.
            if (p2BaseObject != null)
            {
                p2BaseObject.GetComponent<PhotonView>().TransferOwnership(players[1]);
                Debug.Log($"P2 기지의 소유권을 {players[1].NickName}에게 이전했습니다.");
            }
        }
    }
}