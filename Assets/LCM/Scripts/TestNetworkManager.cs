using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TestNetworkManager : MonoBehaviourPunCallbacks
{
    [Header("���� ��ġ")]
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

    // '�� �ڽ�'�� �뿡 ������ �� ���� �� ���� ȣ��˴ϴ�.
    public override void OnJoinedRoom()
    {
        // ������ Ŭ���̾�Ʈ(P1)�� �ڽ��� ������ �����մϴ�.
        if (PhotonNetwork.IsMasterClient)
        {
            float offsetDistance = 0.4f;
            Vector3 spawnPosition = p1_spawnPoint.position - (p1_spawnPoint.right * offsetDistance);

            PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP1" });
        }
    }

    // '�ٸ�' �÷��̾ �뿡 ������ �� '������ Ŭ���̾�Ʈ���Ը�' ȣ��˴ϴ�.
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // ������ Ŭ���̾�Ʈ�� P2�� ������ �����ϰ� �������� �Ѱ��ݴϴ�.
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
