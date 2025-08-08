using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class TestStateUI : MonoBehaviour
{
    [Header("Left Slot UI (�� ����)")]
    [SerializeField] private TextMeshProUGUI leftRankText;
    [SerializeField] private TextMeshProUGUI leftTotalText;
    [SerializeField] private TextMeshProUGUI leftWinsText;
    [SerializeField] private TextMeshProUGUI leftLossesText;

    [Header("Right Slot UI (��� ����)")]
    [SerializeField] private TextMeshProUGUI rightRankText;
    [SerializeField] private TextMeshProUGUI rightTotalText;
    [SerializeField] private TextMeshProUGUI rightWinsText;
    [SerializeField] private TextMeshProUGUI rightLossesText;

    private DatabaseReference dbRef;
    private HashSet<string> _loadedUids = new HashSet<string>();

    void Awake()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    void OnEnable()
    {
        UpdateMyStateUI(); // �� ������ �켱 ǥ��

        if (UserRank.Instance != null)
            UserRank.Instance.OnRankUpdated += UpdateMyStateUI;
    }

    void OnDisable()
    {
        if (UserRank.Instance != null)
            UserRank.Instance.OnRankUpdated -= UpdateMyStateUI;
    }

    //  �� ������ ����
    public void UpdateMyStateUI()
    {
        StartCoroutine(WaitAndLoadMyStats());
    }

    private IEnumerator WaitAndLoadMyStats()
    {
        float timeout = 2f;
        float elapsed = 0f;

        while (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("uid"))
        {
            yield return null;
            elapsed += Time.deltaTime;

            if (elapsed > timeout)
            {
                Debug.LogWarning("UID�� �������� �ʾ� ������ �ҷ��� �� �����ϴ� (Ÿ�Ӿƿ�)");
                yield break;
            }
        }

        LoadStats(PhotonNetwork.LocalPlayer, leftRankText, leftTotalText, leftWinsText, leftLossesText);
    }

    //  ���� ���� ���� or Ŭ����
    public void UpdateOpponentStateUI()
    {
        Player opponent = null;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player != PhotonNetwork.LocalPlayer)
            {
                opponent = player;
                break;
            }
        }

        if (opponent != null)
            LoadStats(opponent, rightRankText, rightTotalText, rightWinsText, rightLossesText);
        else
            ClearSlot(rightRankText, rightTotalText, rightWinsText, rightLossesText);
    }

    //  ���� �ҷ�����
    private void LoadStats(Player p,
        TextMeshProUGUI rankT, TextMeshProUGUI totalT,
        TextMeshProUGUI winsT, TextMeshProUGUI lossesT)
    {
        if (!p.CustomProperties.TryGetValue("uid", out object uidObj))
        {
            Debug.LogWarning($"[UID ����] {p.NickName} ������ �ҷ��� �� �����ϴ�.");
            SetLoadingText(rankT, totalT, winsT, lossesT);
            return;
        }

        string uid = uidObj as string;
        if (_loadedUids.Contains(uid)) return;
        _loadedUids.Add(uid);

        dbRef.Child("users").Child(uid).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                Debug.LogWarning($"[Firebase] uid {uid} ���� ���� �� �⺻�� ǥ��");
                rankT.text = "newbie";
                totalT.text = "0";
                winsT.text = "0";
                lossesT.text = "0";
                return;
            }

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

    //  ��밡 ���� �� Ŭ����
    private void ClearSlot(
        TextMeshProUGUI rankT, TextMeshProUGUI totalT,
        TextMeshProUGUI winsT, TextMeshProUGUI lossesT)
    {
        rankT.text = "-";
        totalT.text = "-";
        winsT.text = "-";
        lossesT.text = "-";
    }

    //  uid�� ���� ��� ��� �޽���
    private void SetLoadingText(
        TextMeshProUGUI rankT, TextMeshProUGUI totalT,
        TextMeshProUGUI winsT, TextMeshProUGUI lossesT)
    {
        rankT.text = "Loading...";
        totalT.text = "-";
        winsT.text = "-";
        lossesT.text = "-";
    }
}