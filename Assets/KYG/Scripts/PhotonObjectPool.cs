using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace KYG
{
    /// <summary>
    /// PhotonNetwork.Instantiate / Destroy를 대체하는 풀링 매니저
    /// - 네트워크 동기화를 유지하며 오브젝트 재사용 최적화
    /// - Projectile, Turret, TurretSlot 등 빈번히 생성/삭제되는 오브젝트에 사용
    /// </summary>
    public class PhotonObjectPool : MonoBehaviourPunCallbacks
    {
        public static PhotonObjectPool Instance { get; private set; }

        // 프리팹 이름을 key로 사용하여 Queue로 오브젝트 관리
        private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();

        private void Awake()
        {
            // 싱글톤 초기화
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 특정 프리팹을 미리 Pool에 등록 및 초기화
        /// </summary>
        public void RegisterPrefab(string prefabName, int initialCount = 0)
        {
            if (poolDict.ContainsKey(prefabName)) return;

            poolDict[prefabName] = new Queue<GameObject>();

            // 초기 개수만큼 미리 생성 후 비활성화
            for (int i = 0; i < initialCount; i++)
            {
                GameObject obj = PhotonNetwork.Instantiate(prefabName, Vector3.zero, Quaternion.identity);
                obj.SetActive(false);
                poolDict[prefabName].Enqueue(obj);
            }
        }

        /// <summary>
        /// 오브젝트 가져오기
        /// - Pool에 존재하면 꺼내서 활성화
        /// - 없으면 PhotonNetwork.Instantiate로 새로 생성
        /// </summary>
        public GameObject Get(string prefabName, Vector3 position, Quaternion rotation, object[] instantiationData = null)
        {
            GameObject obj;

            if (poolDict.ContainsKey(prefabName) && poolDict[prefabName].Count > 0)
            {
                // Queue에서 꺼내서 위치와 회전값 적용
                obj = poolDict[prefabName].Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
            else
            {
                // Pool에 없으면 PhotonNetwork.Instantiate로 새로 생성
                obj = PhotonNetwork.Instantiate(prefabName, position, rotation, 0, instantiationData);
            }

            return obj;
        }

        /// <summary>
        /// 오브젝트를 Pool로 반환 (PhotonNetwork.Destroy 대체)
        /// - 비활성화 후 Queue에 다시 넣음
        /// </summary>
        public void ReturnToPool(GameObject obj)
        {
            string prefabName = obj.name.Replace("(Clone)", "").Trim();
            obj.SetActive(false);

            if (!poolDict.ContainsKey(prefabName))
                poolDict[prefabName] = new Queue<GameObject>();

            poolDict[prefabName].Enqueue(obj);
        }
    }
}
