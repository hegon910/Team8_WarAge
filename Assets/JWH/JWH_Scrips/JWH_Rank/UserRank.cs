using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;

public class UserRank : MonoBehaviour
{
    public static UserRank Instance { get; private set; }
    private DatabaseReference dbRef;
    private string uid => FirebaseAuth.DefaultInstance.CurrentUser.UserId;
    public event Action OnRankUpdated;


    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public void InitializeNewUser()//���� �ʱ�ȭ ȸ�����Խ� �ο�
    {
        if (string.IsNullOrEmpty(uid)) return;

        var userRef = dbRef.Child("users").Child(uid);
        userRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.Result.Exists) return; // �̹� ������ ����

            var data = new
            {
                win = 0,
                lose = 0,
                rank = "newbie"
            };
            userRef.SetRawJsonValueAsync(JsonUtility.ToJson(data));
        });
    }


    public void UpdateMatchResult(bool isWinner)//����� ������Ʈ
    {
        var userRef = dbRef.Child("users").Child(uid);
        userRef.GetValueAsync().ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted || !t.Result.Exists) return;

            int wins = int.Parse(t.Result.Child("win").Value.ToString());
            int losses = int.Parse(t.Result.Child("lose").Value.ToString());

            if (isWinner) wins++;
            else losses++;

            var updates = new Dictionary<string, object>
            {
                { "win",   wins },
                { "lose",  losses },
                { "rank",  CalculateRank(wins, losses) }
            };
            userRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(_ =>
            {
                OnRankUpdated?.Invoke(); // UI ���� �̺�Ʈ ȣ��
            });
        });
    }

    
    private string CalculateRank(int wins, int losses)//��ũ���
    {
        int total = wins + losses;
        if (total < 3) return "newbie";
        float winRate = (float)wins / total;
        if (winRate < 0.5f) return "newbie";
        else if (winRate < 0.8f) return "nomal";
        else return "god";
    }
}