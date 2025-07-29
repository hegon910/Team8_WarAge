using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Option Panels")]
    public GameObject mainPanel;
    public GameObject loadingPanel;
    public GameObject loginPanel;
    public GameObject signUpPanel;
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
        loadingPanel.SetActive(true);
    }

    #endregion

    ///<summary>
    ///로비 - 방 관련 함수
    ///<summary>

    #region 함수
    public void OnClickedLogin()
    {
        //TODO 파이어베이스 로그인 함수 호출

        lobbyPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedCancelBackMain()
    {

        mainPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedSignup()
    {

        signUpPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedSignupConfrim()
    {
        //TODO 파이어베이스 회원가입 함수 호출

        //signUpPanel.SetActive(true);
        //loginPanel.SetActive(false);
    }

    public void OnClickedSignupCancel()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }


    public void OnClickedLoginConfirm()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }


    public void OnClickedCreatRoom()
    {

        roomPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }

    public void OnClickedLobbyCancel()
    {
        lobbyPanel.SetActive(false);
        loginPanel.SetActive(true);
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