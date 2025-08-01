using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class UserRank : MonoBehaviour
{
    public static UserRank Instance { get; private set; }
    private DatabaseReference dbRef;
    private string uid => FirebaseAuth.DefaultInstance.CurrentUser.UserId;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public void InitializeNewUser()//초기화 (회원가입, 첫 로그인 호출)
    {
        var data = new
        {
            win = 0,
            lose = 0,
            rank = "초보"
        };
        dbRef.Child("users")
             .Child(uid)
             .SetRawJsonValueAsync(JsonUtility.ToJson(data));
    }

    
    public void UpdateMatchResult(bool isWinner)//경기결과 업데이트
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
            userRef.UpdateChildrenAsync(updates);
        });
    }

    
    private string CalculateRank(int wins, int losses)//랭크계산
    {
        int total = wins + losses;
        if (total < 3) return "초보";
        float winRate = (float)wins / total;
        if (winRate < 0.5f) return "초보";
        else if (winRate < 0.8f) return "중수";
        else return "고수";
    }
}