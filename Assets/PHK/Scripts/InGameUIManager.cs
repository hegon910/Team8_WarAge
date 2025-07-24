using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//�ΰ����� �������� UI�� �����ϴ� ��ũ��Ʈ(���, ���� ü��, ���� �ؽ�Ʈ ��)

public class InGameUIManager : MonoBehaviour
{
    //�̱��� ���� : �ٸ� ��ũ��Ʈ���� ������ �� �ֵ��� ��.

    public static InGameUIManager Instance { get; private set; }

    [Header("UI���")]
    public TextMeshProUGUI inGameInfoText; //�ΰ��� ���� �ؽ�Ʈ

    [Header("�ڿ� �� ���� ����(���� ����")]
    public TextMeshProUGUI goldText; //��� �ؽ�Ʈ
    public TextMeshProUGUI expText; //����ġ ǥ�� �ؽ�Ʈ
    public Slider baseHpSlider; //���� ü�� ǥ��

    [Header("���� ���� ť")]
    public Slider productionSlider;
    public Toggle[] queueSlots = new Toggle[5];



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        //���� ���� �ÿ��� ���� �ؽ�Ʈ ������ �ʵ���
        HideInfoText();

        //SpawnManager ���� �� �̺κ� �ּ� ����
        // SpawnManager.Instance.OnQueueChanged += UpdateQueueUI;
        // SpawnManager.Instance.OnProductionProgress += UpdateProductionSlider;
        // SpawnManager.Instance.OnProductionStatusChanged += ToggleProductionSliderVisibility;
        if (productionSlider != null) productionSlider.value = 0;
        if(queueSlots != null)
        {
            foreach(var slot in queueSlots)
            {
                if(slot != null) slot.SetIsOnWithoutNotify(false);
            }
        }
    }
   
    //���� ������ �޾� ���� �ؽ�Ʈ UI�� ��� ��� ǥ��
    public void ShowUnitGoldCost(Unit unitData)
    {
        if (unitData != null && inGameInfoText != null)
        {
            inGameInfoText.text = unitData.unitName + "Cost : " + unitData.goldCost;
            inGameInfoText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Unit data or inGameInfoText is null");
        }
    }
    //���� �ؽ�Ʈ�� ����
    public void HideInfoText()
    {
        if (inGameInfoText != null)
        {
            inGameInfoText.gameObject.SetActive(false);
        }
    }

    //���� ���� ��� �Լ�
    //���� EXP �ݿ�
    public void UpdateResourceUI(int currentGold, int currentExp)
    {
        if (goldText != null) goldText.text = $"{currentGold}";
        if (expText != null) expText.text = $"{currentExp}";
    }

    //���� ü�¹� ������Ʈ 
    public void UpdateBaseHpUI(int currentHp, int maxHp)
    {
        if(baseHpSlider != null)
        {
            //(float)�� ���� ���� �������� ���� �ʵ���
            baseHpSlider.value = (float)currentHp / maxHp;
        }
    }
    private void OnDisable()
    {
        //  SpawnManager.cs�� ������ �� �κ��� �ּ��� ����

        // if (SpawnManager.Instance != null)
        // {
        //     SpawnManager.Instance.OnQueueChanged -= UpdateQueueUI;
        //     SpawnManager.Instance.OnProductionProgress -= UpdateProductionSlider;
        //     SpawnManager.Instance.OnProductionStatusChanged -= ToggleProductionSliderVisibility;
        // }

    }
    //SpawnManager�κ��� ���� ť�����͸� �޾ƿ� UI ����
    private void UpdateQueueUI(Queue<Unit> currentQueue)
    {
        //���� ť�� �ִ� ������ ������ ������
        int queuedCount = currentQueue.Count;
        //5���� ��� ��� ������ ��ȸ
        for (int i = 0; i < queueSlots.Length; i++)
        {
            //���� �ε���(i)�� ť�� �ִ� ���� �� ���� ������ �ش� ������ �����
            if(i < queuedCount)
            {
                //����� on ���·� �����.
                queueSlots[i].SetIsOnWithoutNotify(true);
            }
            else
            {
                //�ش� ������ ��������Ƿ� ����� off
                queueSlots[i].SetIsOnWithoutNotify(false);
            }
        }

    }
    private void UpdateProductionSlider(float progress)
    {
        if(productionSlider != null)
        {
            productionSlider.value = progress;
        }
    }


}
