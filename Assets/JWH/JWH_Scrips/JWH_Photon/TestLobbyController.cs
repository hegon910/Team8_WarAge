using System.Linq;
using ExitGames.Client.Photon;
using Firebase.Database;
using Firebase.Extensions;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//using System.Collections;

public class TestLobbyController : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI leftPlayerNameText;
    [SerializeField] private TextMeshProUGUI rightPlayerNameText;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyText;
    [SerializeField] private Button startButton;

    private bool isLocalPlayerReady = false;

    void Awake()
    {
        if (leftPlayerNameText == null || rightPlayerNameText == null ||
            readyButton == null || readyText == null || startButton == null)
        {
            Debug.LogError("TestLobbyController: UI 요소가 빠졌습니다.");
            enabled = false;
            return;
        }

        readyButton.onClick.AddListener(OnReadyButtonClicked);
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    void Start()
    {
        
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 완료");
        PhotonManager.Instance.SetUID(); // 내 uid 설정
        UIManager.Instance.ShowRoomPanel();
        UpdateAllLobbyUI();
    }


    public override void OnJoinedLobby()
    {
        LoadLocalPlayerReadyState();
        UpdateAllLobbyUI();
    }

    public override void OnLeftLobby()
    {
        UpdateAllLobbyUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("2p 들어옴");
        GameObject obj = GameObject.Find("P2PlayerInfo");
        if (obj != null)
        {
            obj.SetActive(true);
            Debug.Log("P2PlayerInfo 오브젝트를 활성화");
        }
        else
        {
            Debug.LogWarning("P2PlayerInfo 오브젝트 null");
        }
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("uid", out object uid))//룸에서 uid 호출 한번더
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "uid", uid } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props); // 다시 전파
            Debug.Log($"[uid 재전파] {PhotonNetwork.LocalPlayer.NickName} → {uid}");
        }
        UpdateAllLobbyUI(); // 플레이어 입장
        FindObjectOfType<TestStateUI>()?.UpdateOpponentStateUI();//상대전적표시
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} 퇴장 → 상대 전적 클리어");

        if (PhotonNetwork.IsMasterClient)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                player.SetCustomProperties(new Hashtable { { "Ready", false } });
            }
        }

        UpdateAllLobbyUI();
        FindObjectOfType<TestStateUI>()?.UpdateOpponentStateUI(); // 상대 클리어
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 상대방 uid 수신 시 전적 UI 갱신
        if (changedProps.ContainsKey("uid"))
        {
            Debug.Log($"[UID 수신됨] {targetPlayer.NickName} → 전적 UI 갱신 시도");
            if (targetPlayer != PhotonNetwork.LocalPlayer)
            {
                FindObjectOfType<TestStateUI>()?.UpdateOpponentStateUI();
            }
        }

        // Ready 상태 UI 갱신
        if (changedProps.ContainsKey("Ready") && targetPlayer == PhotonNetwork.LocalPlayer)
        {
            isLocalPlayerReady = (bool)changedProps["Ready"];
            UpdateReadyUI(isLocalPlayerReady);
        }

        UpdateAllLobbyUI();

        if (PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = PhotonManager.Instance.AreAllPlayersReady();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdateAllLobbyUI(); // 마스터 클라이언트 변경 시 UI 전체 업데이트
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateAllLobbyUI(); // 연결 해제 시 UI 초기화
    }

    private void LoadLocalPlayerReadyState()
    {
        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Ready", out object readyValue))
        {
            isLocalPlayerReady = (bool)readyValue;
        }
        else
        {
            isLocalPlayerReady = false; // 기본값은 Not Ready
        }
        UpdateReadyUI(isLocalPlayerReady);
    }

    private void UpdateAllLobbyUI()
    {
        bool inRoom = PhotonNetwork.InRoom;
        bool isMaster = PhotonNetwork.IsMasterClient;

        leftPlayerNameText.gameObject.SetActive(inRoom);
        rightPlayerNameText.gameObject.SetActive(inRoom);
        readyButton.gameObject.SetActive(inRoom);
        readyText.gameObject.SetActive(inRoom);
        startButton.gameObject.SetActive(inRoom && isMaster);

        if (inRoom)
        {
            Player[] players = PhotonNetwork.PlayerList;

            leftPlayerNameText.text = players.Length >= 1 ? players[0].NickName : "Waiting...";
            rightPlayerNameText.text = players.Length >= 2 ? players[1].NickName : "Waiting...";

            UpdateReadyUI(isLocalPlayerReady);

            if (isMaster)
                startButton.interactable = PhotonManager.Instance.AreAllPlayersReady();
        }
        else
        {
            leftPlayerNameText.text = PhotonNetwork.IsConnectedAndReady ? PhotonNetwork.NickName : "Disconnected";
            rightPlayerNameText.text = "Waiting for room...";
            readyText.text = "Not Ready";
            readyText.color = Color.red;
        }
    }

    void OnReadyButtonClicked()
    {
        SoundManager.Instance.PlayUIClick();
        if (PhotonNetwork.InRoom) // 방에 있을 때만 레디 가능
        {
            isLocalPlayerReady = !isLocalPlayerReady;
            PhotonManager.Instance.SetLocalPlayerReady(isLocalPlayerReady);
        }
        else
        {
            Debug.LogWarning("RoomSceneUIController: Cannot set ready state, not in a room.");
        }
    }

    void OnStartButtonClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonManager.Instance.StartGame();
        }
        else
        {
            Debug.LogWarning("RoomSceneUIController: Only Master Client can start the game.");
        }
    }

    void UpdateReadyUI(bool ready)
    {
        readyText.text = ready ? "Ready" : "Not Ready";
        readyText.color = ready ? Color.green : Color.red;
    }
}