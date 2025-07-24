using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResolutionManager : MonoBehaviour
{
    // 드롭다운으로 구성하여 표시합니다.
    public Dropdown resolutionDropdown;
    // 해상도를 리스트로 할당하여 보관합니다.
    private List<Resolution> resolutions = new List<Resolution>();
    // 해상도와 일치하는 인데스를 기록하기 위해 변수 선언.
    private int optimalResolutionIndex = 0;

    private void Start()
    {
        //해상도를 수동으로 리스트에 등록.
        resolutions.Add(new Resolution { width = 1280, height = 720 });
        resolutions.Add(new Resolution { width = 1920, height = 1080 });
        resolutions.Add(new Resolution { width = 2560, height = 1440 });
        resolutions.Add(new Resolution { width = 3480, height = 2160 });

        // 기존에 있던 옵션들 모두 클리어.
        resolutionDropdown.ClearOptions();

        
        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Count; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            // 가장 적합한 해상도에 별표를 표기합니다.
            // 만약 현재 모니터랑 동일한 해상도라면 * 표기
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                optimalResolutionIndex = i;
                option += " *";
            }
            options.Add(option);
        }
        // 옵션 리스트를 드롭다운에 넣고, 기본값으로는 현재 해상도에 맞는 항목을 선택해서 표시.
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = optimalResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // 게임이 가장 적합한 해상도로 시작되도록 설정합니다.
        SetResolution(optimalResolutionIndex);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        //전체 모드
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}