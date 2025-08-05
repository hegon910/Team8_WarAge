using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class TestStateUI : MonoBehaviour
{
    [Header("���� UI ����")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private TextMeshProUGUI winsText;
    [SerializeField] private TextMeshProUGUI lossesText;

    private DatabaseReference dbRef;
    private string uid => FirebaseAuth.DefaultInstance.CurrentUser.UserId;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        ShowUserStats();  // �κ� ���� �� ���� �ҷ�����
    }

    public void ShowUserStats()
    {
        dbRef.Child("users").Child(uid)
             .GetValueAsync().ContinueWithOnMainThread(task =>
             {
                 if (task.IsFaulted || !task.Result.Exists) return;

                 var snap = task.Result;
                 int wins = int.Parse(snap.Child("win").Value.ToString());
                 int losses = int.Parse(snap.Child("lose").Value.ToString());
                 int total = wins + losses;
                 string rank = snap.Child("rank").Value.ToString();

                 // UI �ݿ�
                 rankText.text = rank;
                 totalText.text = total.ToString();
                 winsText.text = wins.ToString();
                 lossesText.text = losses.ToString();
             });
    }
}

