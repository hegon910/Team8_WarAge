using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionManager : MonoBehaviour
{
    public static OptionManager Instance { get; private set; }

    [Header("Option Panels")]
    public GameObject optionPanel;
    public GameObject graphicPanel;
    public GameObject soundPanel;




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
    ///���� ȭ�� �ɼ� ����â ���� �Լ�
    ///<summary>

    #region �Լ�
    public void ShowOptionPanel()
    {
        optionPanel.SetActive(true);
        ShowGraphicPanel(); // �⺻������ Graphic
    }


    public void OnClickedOptionConfirm()
    {
        ResolutionManager resolutionManager = FindObjectOfType<ResolutionManager>();
        if (resolutionManager != null)
        {
            resolutionManager.ApplyCurrentResolution();
            Debug.Log("�ػ� ���� Ȯ��");
        }
        SoundManager.Instance.ApplyAudioSettings(); // ���� + ����
        Debug.Log("���� ���� Ȯ��");
        // �ɼ� �г� �ݱ�
        optionPanel.SetActive(false);
    }

    public void OnClickedOptionCancel()
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
    #endregion



}