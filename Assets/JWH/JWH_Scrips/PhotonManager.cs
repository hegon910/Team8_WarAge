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

    public void CreateOrJoinRoom()// 방 만든 사람이 마스터
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
        if (!PhotonNetwork.InRoom) return false; // 방에 없으면 준비 상태를 확인할 수 없음

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
            Debug.LogWarning("시작권한 없음");
        }
        else
        {
            Debug.LogWarning("레디안됨");
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
            return new Player[0]; // 방에 없으면 빈 배열 반환
        }
    }
}
