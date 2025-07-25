#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using KYG;

namespace KYG.Editor
{
    /// <summary>
    /// 에디터 상단메뉴에
    /// Resources폴더에 AgeData 자동생성 기능
    /// </summary>

    public class AgeDataCreator
    {
        [MenuItem("Tools/Create/AgeData")]
        public static void CreateAgeData()
        {
            string folderPath = "Assets/Resources/AgeData";

            if (!Directory.Exists(folderPath)) // 해당 폴더 없으면 생성
            {
                Directory.CreateDirectory(folderPath);
                
            }

            foreach (AgeType age in System.Enum.GetValues(typeof(AgeType))) // 모든 시대 타입에 해당하는 에셋생성
            {
                string assetPath = $"{folderPath}/{age}_AgeData.asset";
                
                if(File.Exists(assetPath)) continue; // 이미 존재하면 생성 생략
                
                AgeData ageData = ScriptableObject.CreateInstance<AgeData>(); // ScriptableObject 인스턴스 생성
                ageData.ageType = age;
                ageData.maxHP = 500; // 초기 체력 설정
                ageData.requiredExp = 0; // 초기 경험치 설정
                ageData.spawnableUnits = new List<GameObject>(); // 초기화
                
                AssetDatabase.CreateAsset(ageData, assetPath); // 에셋 생성

            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
        }
    }
}
#endif