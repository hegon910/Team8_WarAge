// AgeManager.cs

using System;
using System.Linq;
using UnityEngine;

namespace KYG
{
    public class AgeManager : MonoBehaviour
    {
        public static AgeManager Instance { get; private set; }

        [Header("시대 데이터")]
        [SerializeField] private AgeData[] ageDataArray;
        private System.Collections.Generic.Dictionary<AgeType, AgeData> ageDataDict;

        // OnAgeChangedByTeam 이벤트와 CurrentAge 프로퍼티를 제거합니다.

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            ageDataDict = ageDataArray.ToDictionary(d => d.ageType);
        }

        // 이제 CanUpgrade는 현재 플레이어의 시대를 파라미터로 받습니다.
        public bool CanUpgrade(AgeType currentAge, int currentEXP)
        {
            var next = GetNextAgeData(currentAge);
            return next != null && currentEXP >= next.requiredExp;
        }

        // 다음 시대에 필요한 경험치를 반환하는 함수도 현재 시대를 파라미터로 받습니다.
        public int GetRequiredExpForNextAge(AgeType currentAge) =>
            GetNextAgeData(currentAge)?.requiredExp ?? -1;

        // 다음 시대 데이터를 반환하는 함수도 현재 시대를 파라미터로 받습니다.
        public AgeData GetNextAgeData(AgeType currentAge)
        {
            AgeType nextType = currentAge + 1;
            return ageDataDict.ContainsKey(nextType) ? ageDataDict[nextType] : null;
        }

        // 특정 시대의 데이터를 직접 가져올 수 있는 함수를 추가하면 유용합니다.
        public AgeData GetAgeData(AgeType ageType)
        {
            return ageDataDict.ContainsKey(ageType) ? ageDataDict[ageType] : null;
        }

        public UltimateSkillData FindUltimateSkillByName(string name)
        {
            foreach (AgeData ageData in ageDataArray)
            {
                if (ageData.ultimateSkill != null && ageData.ultimateSkill.skillName == name)
                {
                    return ageData.ultimateSkill;
                }
            }
            return null;
        }
    }
}