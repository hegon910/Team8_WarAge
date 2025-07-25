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
        
        [SerializeField] private AgeData[] ageDatas; // 시대별 데이터

        public AgeType CurrentAge { get; private set; } // 현재 게임시대 추적 
        //public int CurrentExp {get; private set;} // 현재 경험치 추적

        // 현재 시대에 해당하는 AgeData 반환
        public AgeData CurrentAgeData => ageDatas.FirstOrDefault(a => a.ageType == CurrentAge);

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

            // 게임 시작시 초기화
            CurrentAge = startingAge; // 현재 시대 = 시작시 시대
            //CurrentExp = 0; // 현재 경험치 = 0

        }

        /*public void AddExp(int amount) // 경험치 추가
        {
            if (amount <= 0) return;
            CurrentExp += amount;
        }*/

        public bool CanUpgrade() // 업그레이드 가능 여부 (게임매니져 경험치 기준)
        {
            AgeData nextAgeData = GetNextAgeData();
            return nextAgeData != null && InGameManager.Instance.CurrentEXP >= nextAgeData.requiredExp;

        }

        public void TryUpgradeAge() // 시대 업그레이드 시도
        {
            if (!CanUpgrade()) return;

            AgeData nextAgeData = GetNextAgeData(); // AgeData에서 다음 시대 데이터 가져오기
            if (nextAgeData == null) return;

            CurrentAge = nextAgeData.ageType; // 시대 변경
            OnAgeChanged?.Invoke(nextAgeData); // 이벤트 발송


        }


        private AgeData GetNextAgeData() // 현재 시대의 다음 시대 데이터를 반환
        {
            int currentIndex = Array.IndexOf(ageDatas.OrderBy(d => d.ageType).ToArray(), CurrentAgeData);
            if (currentIndex + 1 >= ageDatas.Length - 1) return null;
            return ageDatas.OrderBy(d => d.ageType).ToArray()[currentIndex + 1];
        }


    }
}
