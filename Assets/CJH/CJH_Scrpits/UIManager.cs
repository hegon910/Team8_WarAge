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

    public void OnClickedCancel()
    {
        optionPanel.SetActive(false);
    }

    public void OnClickedStart()
    {
        
    }
}