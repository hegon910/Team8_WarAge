using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseController : MonoBehaviour
{
    /// <summary>
    /// 기지 시스템 컨트롤러
    /// 체력, 피격, 업그레이드, 유닛 생성 , 터렛 설치
    /// 네트워크 담당자와 초기 스텟 존재 유무 논의 필요 초기 스텟만 받아오면 될지도
    /// 네트워크와 동기화 로직 필요
    /// </summary>
    [SerializeField] public int maxHP; // 최대 체력

    [SerializeField] public int currentHP; // 현재 체력

    [Header("Spawner")] public Transform spawnerPoint; // 유닛 생성 위치

    public void Start() // 게임 시작시 
    {
        currentHP = maxHP; // 현재 체력 = 최대 체력으로 초기화
    }
    
    /// <summary>
    /// 데미지를 받으면 해당 공력력 만큼 현재체력 감소
    /// 체력 UI에 기지 체력 연동 필요
    /// 데미지를 받아 현재 체력이 0이 되면 게임 매니저에 게임 오버 연동
    /// 체력이 0이 될시 파괴되는 에니메이션은 추가 과제
    /// </summary>
    public void TakeDamageBass()
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
