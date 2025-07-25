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
        // �̱��� ����
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �� �̵� �� ����
        }
        else
        {
            Destroy(gameObject);
        }
    }

    ///<summary>
    ///���� ȭ�� - �ɼ� ���� �Լ�
    ///<summary>

    #region �Լ�
    public void ShowOptionPanel()
    {
        optionPanel.SetActive(true);
        ShowGraphicPanel(); // �⺻������ Graphic
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
        // ��: ���� ����, �ɼ� �ݱ�
    }

    public void OnClickedOptionCancel()
    {
        soundPanel.SetActive(false);
        graphicPanel.SetActive(false);
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

    public void OnClickedSignup()
    {
        //TODO ���̾�̽� ȸ������ �Լ� ȣ��

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