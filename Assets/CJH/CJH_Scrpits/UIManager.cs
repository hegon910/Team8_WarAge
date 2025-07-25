using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Option Panels")]
    public GameObject optionPanel;
    public GameObject graphicPanel;
    public GameObject soundPanel;
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
            DontDestroyOnLoad(gameObject); // 씬 이동 시 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    ///<summary>
    ///메인 화면 - 옵션 관련 함수
    ///<summary>

    #region 함수
    public void ShowOptionPanel()
    {
        optionPanel.SetActive(true);
        ShowGraphicPanel(); // 기본값으로 Graphic
    }

    public void HideOptionPanel()
    {
        optionPanel.SetActive(false);
    }
    
    public void ShowGraphicPanel()
    {
        graphicPanel.SetActive(true);
        soundPanel.SetActive(false);
    }
    
    public void ShowSoundPanel()
    {
        graphicPanel.SetActive(false);
        soundPanel.SetActive(true);
    }

    public void OnClickedStart()
    {

    }

    public void OnClickedOptionConfirm()
    {
        // 예: 설정 저장, 옵션 닫기
    }

    public void OnClickedOptionCancel()
    {
        soundPanel.SetActive(false);
        graphicPanel.SetActive(false);
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

    public void OnClickedSignup()
    {
        //TODO 파이어베이스 회원가입 함수 호출

        signUpPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedCreatRoom()
    {

        roomPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }

    public void OnClickedLoginConfirm()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
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