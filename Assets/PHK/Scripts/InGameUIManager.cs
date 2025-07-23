using System.Collections;
using System.Collections.Generic;
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

    [Header("유닛 생산 큐")]
    public Slider productionSlider;
    public Toggle[] queueSlots = new Toggle[5];



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        //게임 시작 시에는 정보 텍스트 보이지 않도록
        HideInfoText();

        //SpawnManager 받을 시 이부분 주석 해제
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
    private void OnDisable()
    {
        //  SpawnManager.cs를 받으면 이 부분의 주석을 해제

        // if (SpawnManager.Instance != null)
        // {
        //     SpawnManager.Instance.OnQueueChanged -= UpdateQueueUI;
        //     SpawnManager.Instance.OnProductionProgress -= UpdateProductionSlider;
        //     SpawnManager.Instance.OnProductionStatusChanged -= ToggleProductionSliderVisibility;
        // }

    }
    //SpawnManager로부터 유닛 큐데이터를 받아와 UI 갱신
    private void UpdateQueueUI(Queue<Unit> currentQueue)
    {
        //현재 큐에 있는 유닛의 개수를 가져옴
        int queuedCount = currentQueue.Count;
        //5개의 모든 토글 슬롯을 순회
        for (int i = 0; i < queueSlots.Length; i++)
        {
            //현재 인덱스(i)가 큐에 있는 유닛 수 보다 작으면 해당 슬롯은 사용중
            if(i < queuedCount)
            {
                //토글을 on 상태로 만든다.
                queueSlots[i].SetIsOnWithoutNotify(true);
            }
            else
            {
                //해당 슬롯은 비어있으므로 토글을 off
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
