using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI leftPlayerNameText;
    [SerializeField] private TextMeshProUGUI rightPlayerNameText;

    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyText;

    [SerializeField] private Button startButton;

    private bool isReady = false;

    void Start()
    {
        
        readyButton.onClick.AddListener(() =>
        {
            isReady = !isReady;
            readyText.text = isReady ? "Ready" : "Not Ready";
            readyText.color = isReady ? Color.green : Color.red;

            SetReadyProperty(isReady);
        });

       
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        startButton.onClick.AddListener(() =>
        {
            if (PhotonNetwork.IsMasterClient && AllPlayersReady())
            {
                PhotonNetwork.LoadLevel("JWH_GameScene");
            }
        });

        UpdatePlayerNames();
    }

    void UpdatePlayerNames()
    {
        Player[] players = PhotonNetwork.PlayerList;

        if (players.Length >= 1)
            leftPlayerNameText.text = players[0].NickName;
        if (players.Length >= 2)
            rightPlayerNameText.text = players[1].NickName;
    }

    void SetReadyProperty(bool ready)
    {
        Hashtable props = new Hashtable { { "Ready", ready } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    bool AllPlayersReady()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue("Ready", out object value) || !(bool)value)
                return false;
        }
        return true;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerNames();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerNames();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = AllPlayersReady();
        }
    }
}