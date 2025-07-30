using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using System;
using System.Collections.Generic;

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
        if (PhotonNetwork.InRoom) // 현재 방에 있다면
        {
            Debug.Log("현재 방에서 나가는 중...");
            PhotonNetwork.LeaveRoom(); // 방에서 나가는 Photon 메서드 호출
        }
        else // 이미 방 밖에 있다면 (예: 로비에 이미 있음)
        {
            Debug.Log("이미 방 밖에 있으므로 바로 로비에 재접속 시도.");
            // 로비에 접속되어 있지 않다면 재접속 시도
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }
    }
    // 방을 나갔을 때 호출되는 콜백 (MonoBehaviourPunCallbacks 상속으로 사용 가능)
    public override void OnLeftRoom()
    {
        Debug.Log("방에서 나왔습니다. 로비에 다시 접속합니다.");
        // 방을 나간 후 로비로 다시 진입
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        // UI를 로비 상태로 전환하는 부분은 UIManager에서 처리하도록 함
        UIManager.Instance.ShowLobbyPanel(); // UIManager에 새롭게 추가할 메서드
    }
    public void ConnectToServer(string nickname)
    {
        if (PhotonNetwork.IsConnected)
        {
            OnConnectedToMaster();
            return;
        }
        Debug.Log("��������");
        PhotonNetwork.NickName = nickname;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        UIManager.Instance.Connect();
    }
    //추가    PHG : 방에 참가했을 때 호출되는 콜백
    public override void OnJoinedRoom()
    {
        Debug.Log($"방에 참가했습니다: {PhotonNetwork.CurrentRoom.Name}");
        // 방에 성공적으로 참가했으므로, UIManager를 통해 방 패널을 활성화합니다.
        UIManager.Instance.ShowRoomPanel(); // UIManager에 이 메서드를 추가해야 합니다.
        // 필요하다면 여기서 게임 시작 전 준비 상태 UI 등을 업데이트할 수 있습니다.
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


    public void CreateOrJoinLobby()// �� ���� ����� ������
    {
        Debug.Log("CreateOrJoinLobby ȣ��");

        if (!PhotonNetwork.InLobby)
        {
            Debug.LogWarning("�κ�������");
            return;
        }

        UIManager.Instance.CreateRoom();
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.JoinOrCreateRoom("TestRoom", options, TypedLobby.Default);
        Debug.Log("JoinOrCreateRoom ȣ��");

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
 


}
