using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResolutionManager : MonoBehaviour
{
    // ��Ӵٿ����� �����Ͽ� ǥ���մϴ�.
    public TMP_Dropdown resolutionDropdown;
    // �ػ󵵸� ����Ʈ�� �Ҵ��Ͽ� �����մϴ�.
    private List<Resolution> resolutions = new List<Resolution>();
    // �ػ󵵿� ��ġ�ϴ� �ε����� ����ϱ� ���� ���� ����.
    private int optimalResolutionIndex = 0;
    // ��üȭ�� â��� ��� ����.
    public Toggle fullscreenToggle;



    private void Start()
    {
        //�ػ󵵸� �������� ����Ʈ�� ���.
        resolutions.Add(new Resolution { width = 1280, height = 720  });
        resolutions.Add(new Resolution { width = 1920, height = 1080 });
        resolutions.Add(new Resolution { width = 2560, height = 1440 });
        resolutions.Add(new Resolution { width = 3480, height = 2160 });

        // ������ �ִ� �ɼǵ� ��� Ŭ����.
        resolutionDropdown.ClearOptions();

        
        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Count; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            // ���� ������ �ػ󵵿� * �� ǥ���մϴ�.
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                optimalResolutionIndex = i;
                option += " *";
            }
            options.Add(option);
        }
        // �ɼ� ����Ʈ�� ��Ӵٿ �ְ�, �⺻�����δ� ���� �ػ󵵿� �´� �׸��� �����ؼ� ǥ��.
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = optimalResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // ������ ���� ������ �ػ󵵷� ���۵ǵ��� �����մϴ�.
        SetResolution(optimalResolutionIndex);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        // ��üȭ��� â��带 bool ������ �Ǵ�
        bool isFullscreen = fullscreenToggle != null && fullscreenToggle.isOn;
        //��ü ���
        Screen.SetResolution(resolution.width, resolution.height, isFullscreen);
        Debug.Log("�ػ� ����");
    }

    public void ApplyCurrentResolution()
    {
        int selectedIndex = resolutionDropdown.value;
        SetResolution(selectedIndex);
    }
}