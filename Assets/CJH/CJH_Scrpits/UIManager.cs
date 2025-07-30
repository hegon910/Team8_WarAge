using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Option Panels")]
    public GameObject mainPanel;
    public GameObject loginPanel;
    public GameObject nicknamePanel;
    public GameObject signUpPanel;
    public GameObject emailPanel;
    public GameObject lobbyPanel;
    public GameObject roomPanel;





    private void Awake()
    {
        // 싱글턴 패턴
        if (Instance == null)
        {
            Instance = this;
        }

        else if (Instance != this)
        {
            Destroy(gameObject); // 중복 방지
        }

    }

    ///<summary>
    ///메인 화면 - 옵션 관련 함수
    ///<summary>

    #region 함수

    public void OnClickedStart()
    {
        mainPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    #endregion

    ///<summary>
    ///로비 - 방 관련 함수
    ///<summary>

    #region 함수
    public void OnClickedLoginFirst()
    {
        emailPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedLogin()
    {
        lobbyPanel.SetActive(true);
        loginPanel.SetActive(false);
    }
     
    public void OnClickedCancelBackMain()
    {

        mainPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedEmailBack()
    {
        signUpPanel.SetActive(true);
        emailPanel.SetActive(false);
    }

    public void OnClickedNicknameConfirm(string nickname)
    {
        PhotonManager.Instance.ConnectToServer(nickname);
    }

    public void OnClickedNicknameBack()
    {
        loginPanel.SetActive(true);
        nicknamePanel.SetActive(false);
    }

    public void Connect()
    {
        lobbyPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedSignup()
    {

        signUpPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedSignupConfrim()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }

    public void OnClickedSignupCancel()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }
 
    public void OnClickedCreateRoom()
    {
        PhotonManager.Instance.CreateOrJoinLobby();
        Debug.Log("클릭");
    }

    public void OnClickedLobbyCancel()
    {
        loginPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }    

    public void OnClickedStartInGame()
    {
        //인게임 연결
        //loginPanel.SetActive(true);
        roomPanel.SetActive(false);
    }

    public void OnClickedReady()
    {
        //Todo 레디 관련 함수 연결
    }

    public void OnClickedLeave()
    {
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
    }
    #endregion







}