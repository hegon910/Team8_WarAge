using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TestNetworkManager : MonoBehaviourPunCallbacks
{
    [Header("생성 위치")]
    public Transform p1_spawnPoint;
    public Transform p2_spawnPoint;

    private void Awake()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("Room1", new RoomOptions { MaxPlayers = 2 }, null);
    }

    // '나 자신'이 룸에 들어왔을 때 각자 한 번씩 호출됩니다.
    public override void OnJoinedRoom()
    {
        // 마스터 클라이언트(P1)는 자신의 기지만 생성합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            float offsetDistance = 0.4f;
            Vector3 spawnPosition = p1_spawnPoint.position - (p1_spawnPoint.right * offsetDistance);

            PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP1" });
        }
    }

    // '다른' 플레이어가 룸에 들어왔을 때 '마스터 클라이언트에게만' 호출됩니다.
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 마스터 클라이언트만 P2의 기지를 생성하고 소유권을 넘겨줍니다.
        if (PhotonNetwork.IsMasterClient)
        {
            float offsetDistance = 0.4f;
            Vector3 spawnPosition = p2_spawnPoint.position - (p2_spawnPoint.right * offsetDistance);

            GameObject p2BaseObject = PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP2" });

            if (p2BaseObject != null)
            {
                p2BaseObject.GetComponent<PhotonView>().TransferOwnership(newPlayer);
            }
        }
    }
}
