using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

public class TestNetworkManager : MonoBehaviourPunCallbacks
{
    private void Awake()
    {
        
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Awake: PhotonNetwork.IsConnected = " + PhotonNetwork.IsConnected);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster: ������ ���� �����. PhotonNetwork.IsConnected = " + PhotonNetwork.IsConnected);
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions { MaxPlayers = 2 }, null);
    }

    public override void OnJoinedRoom()
    {
        
    }

}
