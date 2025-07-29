using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
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

        [Header("시대 데이터")]
        
        [SerializeField] private AgeData[] ageDataArray; // 시대별 데이터 디셔너리로 관리

        private Dictionary<AgeType, AgeData> ageDataDict; // Dictionary로 빠른 조회

        public AgeType CurrentAge { get; private set; } // 현재 게임시대 추적 
        //public int CurrentExp {get; private set;} // 현재 경험치 추적

        // 현재 시대에 해당하는 AgeData 반환
        public AgeData CurrentAgeData => ageDataDict[CurrentAge];

        // 시대 변경시 호출될 이벤트 타 시스템에서 반응가능하도록 연결
        public event Action<string,AgeData> OnAgeChangedByTeam;

        private void Awake()
        {
            if (Instance != null) {Destroy(gameObject); return; } // 싱글톤 패턴
            Instance = this;
            
            // Dictionary 초기화
            ageDataDict = ageDataArray.ToDictionary(d => d.ageType);
            // 게임 시작시 초기화
            CurrentAge = startingAge; // 현재 시대 = 시작시 시대
            //CurrentExp = 0; // 현재 경험치 = 0

        }

        /*public void AddExp(int amount) // 경험치 추가
        {
            if (amount <= 0) return;
            CurrentExp += amount;
        }*/

        public bool CanUpgrade(int currentEXP) // 업그레이드 가능 여부 (게임매니져 경험치 기준)
        {
            var next = GetNextAgeData();
            return next != null && currentEXP >= next.requiredExp;

        }

        public bool TryUpgradeAge(string teamTag, int currentEXP) // 시대 업그레이드 시도
        {
            if (!CanUpgrade(currentEXP)) return false;

            var next = GetNextAgeData(); // AgeData에서 다음 시대 데이터 가져오기
            
            CurrentAge = next.ageType; // 시대 변경
            OnAgeChangedByTeam?.Invoke(teamTag,next); // 이벤트 발송
            
            return true;


        }

        public int GetRequiredExpForNextAge() =>
        
            GetNextAgeData()?.requiredExp ?? -1; //다음 시대 없으면 - 1 반환



        public AgeData GetNextAgeData() // 현재 시대의 다음 시대 데이터를 반환
        {
            AgeType nextType = CurrentAge + 1;
            return ageDataDict.ContainsKey(nextType) ? ageDataDict[nextType] : null;
        }


    }
}
