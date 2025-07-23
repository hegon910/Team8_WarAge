using TMPro;
using UnityEngine;
using UnityEngine.UI;
//인게임의 전반적인 UI를 관리하는 스크립트(골드, 기지 체력, 정보 텍스트 등)

public class InGameUIManager : MonoBehaviour
{
    //싱글턴 패턴 : 다른 스크립트에서 참조할 수 있도록 함.

    public static InGameUIManager Instance { get; private set; }

    [Header("UI요소")]
    public TextMeshProUGUI inGameInfoText; //인게임 정보 텍스트

    [Header("자원 및 기지 상태(구현 예정")]
    public TextMeshProUGUI goldText; //골드 텍스트
    public TextMeshProUGUI expText; //경험치 표시 텍스트
    public Slider baseHpSlider; //기지 체력 표시




    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        //게임 시작 시에는 정보 텍스트 보이지 않도록
        HideInfoText();
    }

    //유닛 정보를 받아 정보 텍스트 UI에 골드 비용 표시
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
    //정보 텍스트를 숨김
    public void HideInfoText()
    {
        if (inGameInfoText != null)
        {
            inGameInfoText.gameObject.SetActive(false);
        }
    }

    //구현 예정 기능 함수
    //골드와 EXP 반영
    public void UpdateResourceUI(int currentGold, int currentExp)
    {
        if (goldText != null) goldText.text = $"{currentGold}";
        if (expText != null) expText.text = $"{currentExp}";
    }

    //기지 체력바 업데이트 
    public void UpdateBaseHpUI(int currentHp, int maxHp)
    {
        if(baseHpSlider != null)
        {
            //(float)을 붙혀 정수 나눗셈이 되지 않도록
            baseHpSlider.value = (float)currentHp / maxHp;
        }
    }

}
