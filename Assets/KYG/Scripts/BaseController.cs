using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // 네트워크 연동을 위한 using

namespace KYG
{
    /// <summary>
    /// 팀 타입을 위한 열거형 임의 지정 추후 네트워크 담당자와 협의후 수정
    /// </summary>
    /*public enum TeamType
    {
        Player1,Player2
    }*/
    
public class BaseController : MonoBehaviourPunCallbacks // PUN 연동 시 PhotonView 사용 가능
{
    /// <summary>
    /// 기지 시스템 컨트롤러
    /// 체력, 피격, 업그레이드, 유닛 생성 , 터렛 설치
    /// 네트워크 담당자와 초기 스텟 존재 유무 논의 필요 초기 스텟만 받아오면 될지도
    /// 네트워크와 동기화 로직 필요
    /// </summary>
    
    [Header("Base")]
    [SerializeField] private int maxHP; // 최대 체력

    private int currentHP; // 임의로 수정 불가능한 현재 체력

    // 프로퍼티로 외부 접근 캡슐화
    public int MaxHP => maxHP; // 최대 체력 접근용 프로퍼티
    
    public int CurrentHP => currentHP; // 현재 체력 접근용 프로퍼티

    // [SerializeField] private TeamType teamType; // 팀 정보 
    
    [Header("Spawner")]
    public Transform spawnerPoint; // 유닛 생성 위치
    [SerializeField] private GameObject currentBaseModel;
    [SerializeField] private GameObject defaultUnitPrefab; // 생성할 유닛 프리팹

    [Header("Turret Slot")] 
    [SerializeField] private GameObject turretSlotPrefab;

    [SerializeField] private Transform turretSlotParent;

    [SerializeField] private int maxTurretSlots = 4;
    private List<TurretSlot> turretSlots = new List<TurretSlot>();
    
    public event Action<int, int> OnHpChanged; //HP 변동시 이벤트 발생 (최대 체력, 현재 체력) 

    private PhotonView pv; // 네트워크 식별용 포톤 뷰

    private void Awake()
    {
        pv = GetComponent<PhotonView>(); // PhotonView 함수 초기화
    }

    /*
     비동기 초기화 순서에서 늦게 호출될 가능성 있음
     OnEnable() 또는 Awake()에서 초기화하는 방식도 추후 테스트 후 고려
    */
    public void Start() // 게임 시작시 
    {
        InitBase(); // 기지 초기화

        AgeManager.Instance.OnAgeChanged += UpgradeBaseByAge; // 시대 변경 이벤트 받아 자동 업그레이드 처리

    }

    public void InitBase()
    {
        currentHP = maxHP; // 현재 체력 = 최대 체력으로 초기화
        OnHpChanged?.Invoke(currentHP, maxHP); // 이벤트 발생
        InGameUIManager.Instance?.UpdateBaseHpUI(currentHP, maxHP); // UI연동
    }
    
    /// <summary>
    /// 유닛과 마찬가지로 터렛 프리펩을 받아
    /// 터렛을 설치 할수있는 위치에 설치
    /// 터렛 설치 할수있는 장소는 총 4곳
    /// 첫번째 장소는 기지에 설치
    /// 수직으로 설치하며 터렛 설치 UI버튼 눌리면 자동으로 터렛 설치 장소 생성
    /// 터렛 설지 가능 장소 미리 구현하고 UI버튼으로 구매 확인후 SetActive?
    /// 설치 장소에만 터렛 설치 가능
    /// </summary>
    
    /// <summary>
    /// 슬롯 추가: Base 위에 새로운 터렛 설치 공간 생성
    /// </summary>
    public void CreateNewTurretSlot()
    {
        if (turretSlots.Count >= maxTurretSlots)
        {
            Debug.LogWarning("최대 슬롯 수 초과!");
            return;
        }

        if (turretSlotPrefab == null)
        {
            Debug.LogError("Turret Slot Prefab이 지정 되지 않았습니다");
            return;
        }
        
        GameObject newSlotObj = PhotonNetwork.Instantiate(turretSlotPrefab.name, turretSlotParent.position, Quaternion.identity);
        newSlotObj.transform.SetParent(turretSlotParent, true);

        TurretSlot slot = newSlotObj.GetComponent<TurretSlot>();
        turretSlots.Add(slot);

        RepositionTurretSlots();
    }
    
    /// <summary>
    /// 슬롯 위치를 정렬 (일렬 배치)
    /// </summary>
    private void RepositionTurretSlots()
    {
        float spacing = 1.5f;
        for (int i = 0; i < turretSlots.Count; i++)
        {
            Vector3 localPos = new Vector3(i * spacing, 0, 0);
            turretSlots[i].transform.localPosition = localPos;
        }
    }
    
    /// <summary>
    /// 데미지를 받으면 해당 공력력 만큼 현재체력 감소
    /// 체력 UI에 기지 체력 연동 필요
    /// 데미지를 받아 현재 체력이 0이 되면 게임 매니저에 게임 오버 연동
    /// 체력이 0이 될시 파괴되는 에니메이션은 추가 과제
    /// </summary>
    public void TakeDamage(int damage)
    {
        if(damage <= 0) return; // Damege 0일때 무시

        if (pv != null && PhotonNetwork.IsConnected && !pv.IsMine) return; // 내 기지가 아니면 처리 x 
        
        
        currentHP = Mathf.Max(0, currentHP - damage); // 체력 감소 최대 0까지
        
        OnHpChanged?.Invoke(currentHP, maxHP); // UI 갱신
        InGameUIManager.Instance?.UpdateBaseHpUI(currentHP, maxHP); // UI 갱신
        
        if(pv != null && PhotonNetwork.IsConnected)
            pv.RPC(nameof(RPC_UpdateHP), RpcTarget.Others, currentHP); // 네트워크 전체 체력 및 데미지 반영

        if (currentHP <= 0)
        {
            BaseDestroyed();
        }
    }

    /// <summary>
    /// RPC 다른 클라이언트에서 체력 동기화 호출
    /// </summary>
    /// <param name="newHp"></param>
    [PunRPC]
    void RPC_UpdateHP(int newHp)
    {
        currentHP = newHp;
        OnHpChanged?.Invoke(currentHP, maxHP); // UI 갱신
        InGameUIManager.Instance?.UpdateBaseHpUI(currentHP, maxHP);
        if (currentHP <= 0)
        {
            BaseDestroyed();
        }
    }

    /// <summary>
    /// 기지 체력 0일때 호출
    /// 게임 매니져 게임오버랑 연동
    /// </summary>

    private void BaseDestroyed()
    {
        Debug.Log("Base destroyed");
        //GameManager.Instance?.GameOver(teamType);
    }

    /// <summary>
    /// PhotonNetwork를 사용하여 멀티플레이에 대응
    /// UI 버튼이 클릭 되면 해당 버튼에 연결된 유닛이
    /// 지정한 스폰 포인트에 생성
    /// </summary>
    public void SpawnUnit(GameObject prefabToSpawn) // 향후 유닛 확장성 가능성 고려 파라미터로 받을수 있게 수정
    {
        if(pv == null || !pv.IsMine)return; // 포톤뷰 사용하여 내 소유 기지에서만 생성가능하게 제한

        if (prefabToSpawn == null)
        
            
            prefabToSpawn = defaultUnitPrefab;
        
        
        PhotonNetwork.Instantiate(prefabToSpawn.name, spawnerPoint.position, Quaternion.identity);
        // 네트워크 상에서 유닛 생성 Quaternion.identity로 회전없이 정방향으로만 생성
    }

    /// <summary>
    /// 일정 경험치가 충족되면 기지 업그레이드 가능
    /// 기지가 업그레이드 되면 최대 체력 증가 및 체력 회복
    /// 추후 시대 변화 시스템과 연동 하여 시대가 변화 할때 기지도 변화
    /// 기지는 부모 해당 시대의 기지는 자식관계로 놓고 해당 시대가 되면 SetActive?
    /// </summary>
    private void UpgradeBaseByAge(AgeData nextAgeData)
    {
        // TODO 업그래이드 기능 구현
        this.maxHP = nextAgeData.maxHP; // 최대 체력 업그레이드
        this.currentHP = maxHP; // 현재 체력 업그레이드 및 회복
        
        // TODO : newAgeData.baseModelPrefab 적용하여 외형 변경 가능하도록 기능 구현
        if (nextAgeData.baseModelPrefab != null)
        {
            if (currentBaseModel != null) Destroy(currentBaseModel);
            currentBaseModel = Instantiate(nextAgeData.baseModelPrefab, transform);
        }
        
        OnHpChanged?.Invoke(currentHP, maxHP); // UI 갱신
        InGameUIManager.Instance?.UpdateBaseHpUI(currentHP, maxHP); // UI 갱신
        
        
    }

    

    


}
}
