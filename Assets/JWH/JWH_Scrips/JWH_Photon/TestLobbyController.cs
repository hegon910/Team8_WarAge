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
            Debug.LogError("TestLobbyController: UI ��Ұ� �������ϴ�.");
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
        Debug.Log("�� ���� �Ϸ�");
        PhotonManager.Instance.SetUID(); // �� uid ����
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
        Debug.Log("2p ����");
        GameObject obj = GameObject.Find("P2PlayerInfo");
        if (obj != null)
        {
            obj.SetActive(true);
            Debug.Log("P2PlayerInfo ������Ʈ�� Ȱ��ȭ");
        }
        else
        {
            Debug.LogWarning("P2PlayerInfo ������Ʈ null");
        }
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("uid", out object uid))//�뿡�� uid ȣ�� �ѹ���
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "uid", uid } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props); // �ٽ� ����
            Debug.Log($"[uid ������] {PhotonNetwork.LocalPlayer.NickName} �� {uid}");
        }
        UpdateAllLobbyUI(); // �÷��̾� ����
        FindObjectOfType<TestStateUI>()?.UpdateOpponentStateUI();//�������ǥ��
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} ���� �� ��� ���� Ŭ����");

        if (PhotonNetwork.IsMasterClient)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                player.SetCustomProperties(new Hashtable { { "Ready", false } });
            }
        }

        UpdateAllLobbyUI();
        FindObjectOfType<TestStateUI>()?.UpdateOpponentStateUI(); // ��� Ŭ����
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // ���� uid ���� �� ���� UI ����
        if (changedProps.ContainsKey("uid"))
        {
            Debug.Log($"[UID ���ŵ�] {targetPlayer.NickName} �� ���� UI ���� �õ�");
            if (targetPlayer != PhotonNetwork.LocalPlayer)
            {
                FindObjectOfType<TestStateUI>()?.UpdateOpponentStateUI();
            }
        }

        // Ready ���� UI ����
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
        UpdateAllLobbyUI(); // ������ Ŭ���̾�Ʈ ���� �� UI ��ü ������Ʈ
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateAllLobbyUI(); // ���� ���� �� UI �ʱ�ȭ
    }

    private void LoadLocalPlayerReadyState()
    {
        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Ready", out object readyValue))
        {
            isLocalPlayerReady = (bool)readyValue;
        }
        else
        {
            isLocalPlayerReady = false; // �⺻���� Not Ready
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
        if (PhotonNetwork.InRoom) // �濡 ���� ���� ���� ����
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