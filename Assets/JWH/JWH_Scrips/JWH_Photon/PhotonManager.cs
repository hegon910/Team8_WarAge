using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using ExitGames.Client.Photon;
using Firebase.Auth;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance;

    [SerializeField] private string loginSceneName = "JWH_LoginScene";
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

    public void LeaveRoomAndRejoinLobby()
    {
        // 방을 나가는 요청만 합니다.
        // 실제 씬 로드는 OnLeftRoom 콜백에서 처리됩니다.
        PhotonNetwork.LeaveRoom();
    }

    // [수정] 방을 나간 후 자동으로 호출되는 콜백 함수
    public override void OnLeftRoom()
    {
        base.OnLeftRoom(); // PUN의 기본 로직을 실행합니다.

        // [핵심] UIManager를 호출하는 대신, LobbyScene을 로드합니다.
        // "LobbyScene"은 CJH 님이 작업하신 로비 씬의 실제 파일 이름이어야 합니다.
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");

        Debug.Log("방을 나갔으며, LobbyScene을 로드합니다.");
    }

    public void ConnectToServer(string fallbackNickname)//로그인에서 로비로?
    {
        string nicknameToUse = fallbackNickname;
        if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            string displayName = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.DisplayName;
            if (!string.IsNullOrEmpty(displayName))
            {
                nicknameToUse = displayName;
            }
        }

        Debug.Log($"{nicknameToUse}");
        PhotonNetwork.NickName = nicknameToUse;
        PhotonNetwork.ConnectUsingSettings();
    }
    //public void ConnectToServer(string nickname)
    //{
    //    if (PhotonNetwork.IsConnected)
    //    {
    //        OnConnectedToMaster();
    //        return;
    //    }
    //    Debug.Log("��������");
    //    PhotonNetwork.NickName = nickname;
    //    PhotonNetwork.ConnectUsingSettings();
    //}


    public override void OnConnectedToMaster()
    {
        UIManager.Instance.Connect();

        //uid 커스텀프로퍼티로 저장해서 전적을 보여줄거임
        //string firebaseUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        //ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        //{
        //{ "uid", firebaseUid }
        //};
        //PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    //추가    PHG : 방에 참가했을 때 호출되는 콜백
    public override void OnJoinedRoom()
    {
        Debug.Log($"방에 참가했습니다: {PhotonNetwork.CurrentRoom.Name}");
        // 방에 성공적으로 참가했으므로, UIManager를 통해 방 패널을 활성화합니다.
        UIManager.Instance.ShowRoomPanel(); // UIManager에 이 메서드를 추가해야 합니다.
        // 필요하다면 여기서 게임 시작 전 준비 상태 UI 등을 업데이트할 수 있습니다.
        string firebaseUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
        { "uid", firebaseUid }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    //추가 PHG : 주석처리
    //public override void OnJoinedLobby()
    //{
    //    // 방 제목 null 아닐 때
    //    // 채팅 활성화
    //    // 전적 활성화
    //    // 방 목록 활성화
    //}
    //추가 PHG
    public void CreateOrJoinRoom()
    {
        Debug.Log("CreateOrJoinRoom 호출");

        if (!PhotonNetwork.InLobby)
        {
            Debug.LogWarning("로비에 접속하지 않아 방을 만들 수 없습니다.");
            return;
        }

        UIManager.Instance.CreateRoom();
        RoomOptions options = new RoomOptions { MaxPlayers = 2, IsVisible = true, IsOpen = true };
        PhotonNetwork.JoinOrCreateRoom("TestRoom", options, TypedLobby.Default);
        Debug.Log("JoinOrCreateRoom 호출 완료");
    }


    public void CreateOrJoinLobby()
    {
<<<<<<< HEAD:Assets/JWH/JWH_Scrips/JWH_Photon/PhotonManager.cs
        Debug.Log("CreateOrJoinLobby");

        if (!PhotonNetwork.InLobby)//동작안함?
        {
            Debug.LogWarning("로비진입");
            return;
        }

        UIManager.Instance.CreateRoom();
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.JoinOrCreateRoom("TestRoom", options, TypedLobby.Default);
        Debug.Log("JoinOrCreateRoom");

=======
    
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.JoinOrCreateRoom("TestRoom", options, TypedLobby.Default);
        UIManager.Instance.CreateRoom();
        Debug.Log("JoinOrCreateRoom ȣ��");
    
>>>>>>> Develop:Assets/JWH/JWH_Scrips/PhotonManager.cs
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


    public Player[] GetCurrentLobbyPlayers()
    {
        if (PhotonNetwork.InLobby)
        {
            return PhotonNetwork.PlayerList;
        }
        else
        {
            return new Player[0]; // �濡 ������ �� �迭 ��ȯ
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // --- [새로 추가된 디버그 로그 시작] ---
        if (roomList.Count == 0)
        {
            Debug.Log("OnRoomListUpdate: 수신된 방 목록이 비어있습니다. (현재 생성된 방이 없거나, 로비에 방이 없는 상태일 수 있습니다.)");
        }
        else
        {
            Debug.Log($"<color=lime>OnRoomListUpdate: {roomList.Count}개의 방 정보 변경사항 수신.</color> -- 목록 --");
            foreach (RoomInfo info in roomList)
            {
                // info.RemovedFromList는 해당 방이 목록에서 제거되었음을 의미합니다.
                if (info.RemovedFromList)
                {
                    Debug.Log($" - 방 '{info.Name}'이 제거되었습니다.");
                }
                else
                {
                    Debug.Log($" - 방 이름: <color=yellow>{info.Name}</color> | 인원: {info.PlayerCount}/{info.MaxPlayers}");
                }
            }
            Debug.Log("------------------------------------");
        }
        // --- [디버그 로그 끝] ---

        // 기존 UI 업데이트 로직은 그대로 호출합니다.
        UIManager.Instance.UpdateRoomList(roomList);
    }

    public void LeaveRoomAndLoadLobby()
    {
        // ★★★ 가장 중요한 안전장치 ★★★
        // 현재 게임 룸 안에 있을 때만 LeaveRoom을 호출합니다.
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("게임 룸을 나갑니다...");
            PhotonNetwork.LeaveRoom(); // 이 함수는 성공 시 OnLeftRoom 콜백을 자동으로 호출합니다.
        }
        else
        {
            // 이미 룸에 없거나(마스터 서버 등), 연결이 끊긴 상태입니다.
            // 이 경우 그냥 로비 씬을 로드합니다.
            Debug.Log("현재 룸에 접속해있지 않아, 바로 로비 씬을 로드합니다.");
            SceneManager.LoadScene("LobbyScene");
        }
    }



}
