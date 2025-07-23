using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI leftPlayerText;
    [SerializeField] private TextMeshProUGUI rightPlayerText;

    void Start()
    {
        Player[] players = PhotonNetwork.PlayerList;

        if (players.Length >= 1)
            leftPlayerText.text = players[0].NickName;

        if (players.Length >= 2)
            rightPlayerText.text = players[1].NickName;
    }
}
