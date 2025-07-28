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
        loadingPanel.SetActive(true);
    }

    #endregion

    ///<summary>
    ///�κ� - �� ���� �Լ�
    ///<summary>

    #region �Լ�
    public void OnClickedLogin()
    {
        //TODO ���̾�̽� �α��� �Լ� ȣ��

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
        //TODO ���̾�̽� ȸ������ �Լ� ȣ��

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