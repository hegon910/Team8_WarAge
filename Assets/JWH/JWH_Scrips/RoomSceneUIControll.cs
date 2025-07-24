using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class RoomSceneUIController : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI leftPlayerNameText;
    [SerializeField] private TextMeshProUGUI rightPlayerNameText;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyText;
    [SerializeField] private Button startButton;

    private bool isLocalPlayerReady = false;

    void Start()
    {
        if (leftPlayerNameText == null || rightPlayerNameText == null ||
            readyButton == null || readyText == null || startButton == null)
        {
            return;
        }

        readyButton.onClick.RemoveAllListeners();
        readyButton.onClick.AddListener(OnReadyButtonClicked);

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartButtonClicked);

        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        UpdatePlayerUI();

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Ready", out object readyValue))
        {
            isLocalPlayerReady = (bool)readyValue;
            UpdateReadyUI(isLocalPlayerReady);
        }
        else
        {
            isLocalPlayerReady = false;
            UpdateReadyUI(isLocalPlayerReady);
            PhotonManager.Instance.SetLocalPlayerReady(isLocalPlayerReady);
        }
    }

    private void OnReadyButtonClicked()
    {
        isLocalPlayerReady = !isLocalPlayerReady;
        PhotonManager.Instance.SetLocalPlayerReady(isLocalPlayerReady);
        UpdateReadyUI(isLocalPlayerReady);
    }

    private void OnStartButtonClicked()
    {
        PhotonManager.Instance.StartGame();
    }

    void UpdatePlayerUI()
    {
        Player[] players = PhotonManager.Instance.GetCurrentRoomPlayers();

        if (leftPlayerNameText != null)
        {
            leftPlayerNameText.text = players.Length >= 1 ? players[0].NickName : "Waiting...";
        }

        if (rightPlayerNameText != null)
        {
            rightPlayerNameText.text = players.Length >= 2 ? players[1].NickName : "Waiting...";
        }

        if (startButton != null && PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = PhotonManager.Instance.AreAllPlayersReady();
        }
    }

    void UpdateReadyUI(bool ready)
    {
        if (readyText != null)
        {
            readyText.text = ready ? "Ready" : "Not Ready";
            readyText.color = ready ? Color.green : Color.red;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerUI();
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
            UpdatePlayerUI();
        }
    }
}

