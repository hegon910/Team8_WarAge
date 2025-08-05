using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Firebase.Database;
using Firebase.Extensions;

public class TestStateUI : MonoBehaviour
{
    [Header("Left Slot UI")]
    [SerializeField] private TextMeshProUGUI leftRankText;
    [SerializeField] private TextMeshProUGUI leftTotalText;
    [SerializeField] private TextMeshProUGUI leftWinsText;
    [SerializeField] private TextMeshProUGUI leftLossesText;

    [Header("Right Slot UI")]
    [SerializeField] private TextMeshProUGUI rightRankText;
    [SerializeField] private TextMeshProUGUI rightTotalText;
    [SerializeField] private TextMeshProUGUI rightWinsText;
    [SerializeField] private TextMeshProUGUI rightLossesText;

    private DatabaseReference dbRef;

    void Awake()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    
    void OnEnable()
    {
        UpdateStateUI();
    }

    private void UpdateStateUI()
    {
        Player[] players = PhotonNetwork.PlayerList;

        // ¿ÞÂÊ ½½·Ô
        if (players.Length > 0)
        {
            LoadStats(players[0], leftRankText, leftTotalText, leftWinsText, leftLossesText);
        }
        else
        {
            ClearSlot(leftRankText, leftTotalText, leftWinsText, leftLossesText);
        }

        // ¿À¸¥ÂÊ ½½·Ô
        if (players.Length > 1)
        {
            LoadStats(players[1], rightRankText, rightTotalText, rightWinsText, rightLossesText);
        }
        else
        {
            ClearSlot(rightRankText, rightTotalText, rightWinsText, rightLossesText);
        }
    }

    private void LoadStats(Player p,
        TextMeshProUGUI rankT, TextMeshProUGUI totalT,
        TextMeshProUGUI winsT, TextMeshProUGUI lossesT)
    {
        if (!p.CustomProperties.TryGetValue("uid", out object uidObj)) return;
        string uid = uidObj as string;

        dbRef.Child("users").Child(uid)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.Result.Exists) return;
                var snap = task.Result;
                int wins = int.Parse(snap.Child("win").Value.ToString());
                int losses = int.Parse(snap.Child("lose").Value.ToString());
                int total = wins + losses;
                string rank = snap.Child("rank").Value.ToString();

                rankT.text = rank;
                totalT.text = total.ToString();
                winsT.text = wins.ToString();
                lossesT.text = losses.ToString();
            });
    }

    private void ClearSlot(
        TextMeshProUGUI rankT, TextMeshProUGUI totalT,
        TextMeshProUGUI winsT, TextMeshProUGUI lossesT)
    {
       
        rankT.text = "-";
        totalT.text = "-";
        winsT.text = "-";
        lossesT.text = "-";
    }
}