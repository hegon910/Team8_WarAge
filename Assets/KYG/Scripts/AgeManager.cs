using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace KYG
{
    /// <summary>
    /// 시대 시스템을 전체 관리하는 매니저
    /// 현재 시대 추적
    /// 현재 경험치 확인 후 업그레이드 가능 여부 판단
    /// 시대 변경시 연관된 시스템에 연동
    /// </summary>
    public class AgeManager : MonoBehaviour
    {
        public static AgeManager Instance { get; private set; }

        [Header("초기화 설정")]
        [SerializeField] private AgeType startingAge = AgeType.Ancient; // 시작 시대 : 고대

        [SerializeField] private int[] requiredExpAge; // 각 시대 업그레이드에 필요한 경험치

        [Header("시대 데이터")]
        [SerializeField] private AgeData[] ageDatas; // 시대별 데이터
        
        public AgeType CurrentAge { get; private set; } // 현재 게임시대 추적 
        public int CurrentExp {get; private set;} // 현재 경험치 추적
        
        // 현재 시대에 해당하는 AgeData 반환
        public AgeData CurrentAgeData => ageDatas.FirstOrDefault(a => a.ageType == CurrentAge);
        
        // 시대 변경시 호출될 이벤트 타 시스템에서 반응가능하도록 연결
        public event System.Action<AgeData> OnAgeChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            // 게임 시작시 초기화
            CurrentAge = startingAge; // 현재 시대 = 시작시 시대
            CurrentExp = 0; // 현재 경험치 = 0
            
        }

        
    }
}
