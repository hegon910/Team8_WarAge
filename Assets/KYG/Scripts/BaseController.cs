using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // 네트워크 연동을 위한 using

namespace KYG
{
    
public class BaseController : MonoBehaviourPunCallbacks // PUN 연동 시 PhotonView 사용 가능
{
    /// <summary>
    /// 기지 시스템 컨트롤러
    /// 체력, 피격, 업그레이드, 유닛 생성 , 터렛 설치
    /// 네트워크 담당자와 초기 스텟 존재 유무 논의 필요 초기 스텟만 받아오면 될지도
    /// 네트워크와 동기화 로직 필요
    /// </summary>
    [SerializeField] private int maxHP; // 최대 체력

    private int currentHP; // 임의로 수정 불가능한 현재 체력

    // 프로퍼티로 외부 접근 캡슐화
    public int MaxHP => maxHP; 
    
    public int CurrentHP => currentHP; 
    
    

    [Header("Spawner")] public Transform spawnerPoint; // 유닛 생성 위치

    public event Action<int, int> OnHpChanged; //HP 변동시 이벤트 발생 (최대 체력, 현재 체력) 

    private PhotonView pv; 

    private void Awake()
    {
        pv = GetComponent<PhotonView>(); // PhotonView 함수 초기화
    }

    public void Start() // 게임 시작시 
    {
        InitBase(); // 기지 초기화
    }

    public void InitBase()
    {
        currentHP = maxHP; // 현재 체력 = 최대 체력으로 초기화
        OnHpChanged?.Invoke(currentHP, maxHP); // 이벤트 발생
        //InGameUIManager.Instance?.UpdateBaseHpUI(currentHP, maxHP); // UI연동
    }
    
    /// <summary>
    /// 데미지를 받으면 해당 공력력 만큼 현재체력 감소
    /// 체력 UI에 기지 체력 연동 필요
    /// 데미지를 받아 현재 체력이 0이 되면 게임 매니저에 게임 오버 연동
    /// 체력이 0이 될시 파괴되는 에니메이션은 추가 과제
    /// </summary>
    public void TakeDamageBase()
    {
        
    }

    /// <summary>
    /// UI 버튼이 클릭 되면 해당 버튼에 연결된 유닛이
    /// 지정한 스폰 포인트에 생성
    /// </summary>
    public void SpawnUnit()
    {
        
    }
    
    // TODO 업그래이드
    
    
    // TODO 터렛 설치
    
}
}
