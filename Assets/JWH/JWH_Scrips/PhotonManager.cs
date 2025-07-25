using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using System;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance;

    [SerializeField] private string loginSceneName = "JWH_LoginScene";
    [SerializeField] private string lobbySceneName = "JWH_LobbyScene";
    [SerializeField] private string gameSceneName = "JWH_GameScene";

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ConnectToServer(string nickname)
    {
        if (PhotonNetwork.IsConnected)
        {
            OnConnectedToMaster();
            return;
        }
        PhotonNetwork.NickName = nickname;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        SceneManager.LoadScene(lobbySceneName);
        CreateOrJoinRoom();
    }

    public void CreateOrJoinRoom()// �� ���� ����� ������
    {
        string roomName = "TestRoom";
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public void SetLocalPlayerReady(bool ready)
    {
        Hashtable props = new Hashtable { { "Ready", ready } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public bool AreAllPlayersReady()
    {
        if (!PhotonNetwork.InRoom) return false; // �濡 ������ �غ� ���¸� Ȯ���� �� ����

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue("Ready", out object value) || !(bool)value)
            {
                return false;
            }
        }
        return true;
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient && AreAllPlayersReady())
        {
            PhotonNetwork.LoadLevel(gameSceneName);
        }
        else if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("���۱��� ����");
        }
        else
        {
            Debug.LogWarning("����ȵ�");
        }
    }


    public Player[] GetCurrentRoomPlayers()
    {
        if (PhotonNetwork.InRoom)
        {
            return PhotonNetwork.PlayerList;
        }
        else
        {
            return new Player[0]; // �濡 ������ �� �迭 ��ȯ
        }
    }
}
