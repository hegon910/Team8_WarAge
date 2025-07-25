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

        private Dictionary<AgeType, AgeData> ageDataDict;

        public AgeType CurrentAge { get; private set; } // 현재 게임시대 추적 
        //public int CurrentExp {get; private set;} // 현재 경험치 추적

        // 현재 시대에 해당하는 AgeData 반환
        public AgeData CurrentAgeData => ageDataDict[CurrentAge];

        // 시대 변경시 호출될 이벤트 타 시스템에서 반응가능하도록 연결
        public event Action<AgeData> OnAgeChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Dictionary 초기화
            ageDataDict = ageDataArray.ToDictionary(data => data.ageType);
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
            AgeData next = GetNextAgeData();
            return next != null && currentEXP >= next.requiredExp;

        }

        public void TryUpgradeAge(int currentEXP) // 시대 업그레이드 시도
        {
            if (!CanUpgrade(currentEXP)) return;

            AgeData next = GetNextAgeData(); // AgeData에서 다음 시대 데이터 가져오기
            if (next == null) return;

            CurrentAge = next.ageType; // 시대 변경
            OnAgeChanged?.Invoke(next); // 이벤트 발송


        }


        private AgeData GetNextAgeData() // 현재 시대의 다음 시대 데이터를 반환
        {
            var ordered = ageDataArray.OrderBy(d => d.ageType).ToArray();
            int index = Array.IndexOf(ordered, CurrentAgeData);
            
            if (index < 0 || index + 1 >= ordered.Length) return null;
            return ordered[index + 1];
        }


    }
}
