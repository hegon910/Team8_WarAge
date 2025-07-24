using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

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

    public void OnClickedCancel()
    {
        optionPanel.SetActive(false);
    }

    public void OnClickedStart()
    {
        
    }
}