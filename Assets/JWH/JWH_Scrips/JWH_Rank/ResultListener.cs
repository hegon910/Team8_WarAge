using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultListener : MonoBehaviour
{
    void OnEnable()
    {
        InGameManager.Instance.OnGameWon += ListenWin;
        InGameManager.Instance.OnGameLost += ListenLoss;
    }

    void OnDisable()
    {
        InGameManager.Instance.OnGameWon -= ListenWin;
        InGameManager.Instance.OnGameLost -= ListenLoss;
    }

    void ListenWin() => UserRank.Instance.UpdateMatchResult(true);
    void ListenLoss() => UserRank.Instance.UpdateMatchResult(false);
}
