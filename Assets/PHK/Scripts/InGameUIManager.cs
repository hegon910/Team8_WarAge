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




    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        //���� ���� �ÿ��� ���� �ؽ�Ʈ ������ �ʵ���
        HideInfoText();
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

}
