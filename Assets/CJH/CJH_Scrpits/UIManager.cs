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
        // �̱��� ����
        if (Instance == null)
        {
            Instance = this;
        }

        else if (Instance != this)
        {
            Destroy(gameObject); // �ߺ� ����
        }

    }

    ///<summary>
    ///���� ȭ�� - �ɼ� ���� �Լ�
    ///<summary>

    #region �Լ�

    public void OnClickedStart()
    {
        mainPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    #endregion

    ///<summary>
    ///�κ� - �� ���� �Լ�
    ///<summary>

    #region �Լ�
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
        Debug.Log("Ŭ��");
    }

    public void OnClickedLobbyCancel()
    {
        loginPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }    

    public void OnClickedStartInGame()
    {
        //�ΰ��� ����
        //loginPanel.SetActive(true);
        roomPanel.SetActive(false);
    }

    public void OnClickedReady()
    {
        //Todo ���� ���� �Լ� ����
    }

    public void OnClickedLeave()
    {
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
    }
    #endregion







}