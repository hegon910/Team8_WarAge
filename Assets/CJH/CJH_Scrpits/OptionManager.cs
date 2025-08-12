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
    ///메인 화면 옵션 설정창 관련 함수
    ///<summary>

    #region 함수
    public void ShowOptionPanel()
    {
        optionPanel.SetActive(true);
        ShowGraphicPanel(); // 기본값으로 Graphic
    }


    public void OnClickedOptionConfirm()
    {
        ResolutionManager resolutionManager = FindObjectOfType<ResolutionManager>();
        if (resolutionManager != null)
        {
            resolutionManager.ApplyCurrentResolution();
            Debug.Log("해상도 설정 확인");
        }
        SoundManager.Instance.ApplyAudioSettings(); // 적용 + 저장
        Debug.Log("사운드 설정 확인");
        // 옵션 패널 닫기
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