using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KYG
{
    /// <summary>
    /// 궁극기 데이터를 이름으로 검색할 수 있도록 관리하는 DB
    /// </summary>
    public class SkillDatabase : MonoBehaviour
    {
        public static SkillDatabase Instance;

        [Tooltip("게임 내에서 사용하는 모든 궁극기 스킬 데이터 등록")]
        [SerializeField] private List<UltimateSkillData> allSkills = new();

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// 스킬 이름으로 ScriptableObject 가져오기
        /// </summary>
        public UltimateSkillData GetSkillByName(string name)
        {
            return allSkills.Find(skill => skill.skillName == name);
        }
    }
}
