using ExitGames.Client.Photon;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance;
    [SerializeField] public GameObject soundManagerPrefab;

    [Header("Input")]
    [SerializeField] private string gameSceneName = "PHG_NetWorkInGameTest";
    [SerializeField] private TMP_InputField roomNameInputField; // 로비패널에 있는 인풋 방이름

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
        if (SoundManager.Instance == null && soundManagerPrefab != null)
        {
            Instantiate(soundManagerPrefab);
        }
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
        PhotonNetwork.JoinLobby();
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 연결");
        PhotonNetwork.JoinLobby();
        UIManager.Instance.Connect();
    }
    //추가    PHG : 방에 참가했을 때 호출되는 콜백


    // [수정] 방을 나간 후 자동으로 호출되는 콜백 함수
    public override void OnLeftRoom()
    {
        base.OnLeftRoom(); // PUN의 기본 로직을 실행합니다.
        // [핵심] UIManager를 호출하는 대신, LobbyScene을 로드합니다.
        // "LobbyScene"은 CJH 님이 작업하신 로비 씬의 실제 파일 이름이어야 합니다.
        SceneManager.LoadScene("LobbyScene"); // 로비 씬을 로드합니다.
        PhotonNetwork.JoinLobby();
        //   UIManager.Instance.ShowLobbyPanel();

        Debug.Log("방을 나갔으며, LobbyScene을 로드합니다.");
    }
    public override void OnJoinedRoom()
    {
        Debug.Log($"방에 참가했습니다: {PhotonNetwork.CurrentRoom.Name}");

        UIManager.Instance.ShowRoomPanel();

        string firebaseUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
        { "uid", firebaseUid },
               { "Ready", false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // 유저가 처음일 경우 초기화
        FirebaseDatabase.DefaultInstance.GetReference("users").Child(firebaseUid)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.Result.Exists)
                {
                    Debug.Log("[전적 없음] 방 입장 시 전적 초기화");
                    UserRank.Instance?.InitializeNewUser();
                }

                // uid 설정 후 강제 전적 UI 갱신 호출
                FindObjectOfType<TestStateUI>()?.UpdateMyStateUI();
            });
    }

    public override void OnJoinedLobby()
    {
        //if (UIManager.Instance != null)
        //{
        //    UIManager.Instance.ShowLobbyPanel(); // 로비 패널을 보여줍니다.
        //    Debug.Log("로비에 입장했습니다.");
        //}
        //else
        //{
        //    Debug.LogWarning("UIManager가 초기화되지 않았습니다. 로비 패널을 표시할 수 없습니다.");
        //}
        StartCoroutine(ShowLobbyPanelWhenReady());
    }
    private System.Collections.IEnumerator ShowLobbyPanelWhenReady()
    {
        // UIManager.Instance가 null이 아닐 때까지 (준비될 때까지) 매 프레임 기다립니다.
        while (UIManager.Instance == null)
        {
            yield return null;
        }

        // UIManager가 준비되었으므로, 이제 안전하게 로비 패널을 표시합니다.
        UIManager.Instance.ShowLobbyPanel();
        Debug.Log("로비에 입장했으며, UIManager가 준비되어 패널을 표시합니다.");
    }


    public void CreateOrJoinLobby()
    {

        Debug.Log("CreateOrJoinLobby");

        if (!PhotonNetwork.InLobby)
        {
            Debug.LogWarning("로비에 입장하지 않았습니다");
            return;
        }

        string roomName = roomNameInputField.text;

        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("방 이름이 비어 있습니다");
            return;
        }
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
        Debug.Log($"CreateRoom 호출됨: {roomName}");
    }


    public void SetLocalPlayerReady(bool ready)
    {

        Hashtable props = new Hashtable { { "Ready", ready } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public bool AreAllPlayersReady()
    {
        SoundManager.Instance.PlayEvolveSound();
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
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
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
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["Ready"] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props); // 로비로 돌아갈 때 플레이어의 준비 상태를 초기화합니다.
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

    public void SetUID()
    {
        string firebaseUid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(firebaseUid))
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "uid", firebaseUid }
        };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }



}
