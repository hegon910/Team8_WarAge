using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

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
            Debug.LogError("RoomSceneUIController: Some UI elements are not assigned in the Inspector.");
            enabled = false; // ��ũ��Ʈ ��Ȱ��ȭ
            return;
        }

        
        readyButton.onClick.AddListener(OnReadyButtonClicked);
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    void Start()
    {
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
        UpdateAllLobbyUI(); // �÷��̾� ����
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateAllLobbyUI(); // �÷��̾� ����
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Ready"))
        {
         
            if (targetPlayer == PhotonNetwork.LocalPlayer)
            {
                isLocalPlayerReady = (bool)changedProps["Ready"];
                UpdateReadyUI(isLocalPlayerReady);
            }
            
            if (PhotonNetwork.IsMasterClient)
            {
                startButton.interactable = PhotonManager.Instance.AreAllPlayersReady();
            }
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
        startButton.gameObject.SetActive(inRoom && isMaster); // �濡 �ְ� �������� ���� ���� ��ư Ȱ��ȭ

        if (inRoom)
        {
            Player[] players = PhotonNetwork.PlayerList;

            leftPlayerNameText.text = players.Length >= 1 ? players[0].NickName : "Waiting...";
            rightPlayerNameText.text = players.Length >= 2 ? players[1].NickName : "Waiting...";

            UpdateReadyUI(isLocalPlayerReady);

            if (isMaster)
            {
                startButton.interactable = PhotonManager.Instance.AreAllPlayersReady();
            }
        }
        else // �濡 ���� �� (�κ� ����)
        {
            leftPlayerNameText.text = PhotonNetwork.IsConnectedAndReady ? PhotonNetwork.NickName : "Disconnected";
            rightPlayerNameText.text = "Waiting for room...";
            readyText.text = "Not Ready";
            readyText.color = Color.red;
        }
    }

    void OnReadyButtonClicked()
    {
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