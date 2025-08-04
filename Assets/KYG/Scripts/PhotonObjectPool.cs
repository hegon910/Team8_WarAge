using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Photon과 연동되는 Object Pool Manager
/// Instantiate/Destroy를 최소화하여 성능 최적화
/// </summary>
public class PhotonObjectPool : MonoBehaviourPun
{
    public static PhotonObjectPool Instance;

    [System.Serializable]
    public class PoolItem
    {
        public string key;                 // Prefab 이름 (Resource Key)
        public GameObject prefab;          // Prefab 참조
        public int initialSize = 5;        // 초기 생성 개수
    }

    [Header("풀링 프리팹 등록")]
    [SerializeField] private List<PoolItem> poolPrefabs = new();

    private Dictionary<string, Queue<GameObject>> poolDictionary = new();

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        InitializePools();
    }

    /// <summary>
    /// 등록된 Prefab별로 초기 풀 생성
    /// </summary>
    private void InitializePools()
    {
        foreach (var item in poolPrefabs)
        {
            Queue<GameObject> objectQueue = new Queue<GameObject>();

            for (int i = 0; i < item.initialSize; i++)
            {
                GameObject obj = CreateNewObject(item.key, item.prefab);
                obj.SetActive(false);
                objectQueue.Enqueue(obj);
            }

            poolDictionary[item.key] = objectQueue;
        }
    }

    /// <summary>
    /// 풀에서 Prefab 인스턴스를 꺼내는 함수
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;

        // 풀에 등록되지 않은 프리팹이면 새로 등록
        if (!poolDictionary.ContainsKey(key))
        {
            Queue<GameObject> newQueue = new Queue<GameObject>();
            poolDictionary.Add(key, newQueue);
        }

        GameObject obj;

        // 풀에 남은 객체가 있으면 꺼내기
        if (poolDictionary[key].Count > 0)
        {
            obj = poolDictionary[key].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            // 부족하면 새로 생성
            obj = CreateNewObject(key, prefab);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }

        return obj;
    }

    /// <summary>
    /// 사용 완료된 오브젝트를 풀에 반환
    /// </summary>
    public void Release(GameObject obj)
    {
        obj.SetActive(false);
        string key = obj.name.Replace("(Clone)", "").Trim();

        if (!poolDictionary.ContainsKey(key))
            poolDictionary.Add(key, new Queue<GameObject>());

        poolDictionary[key].Enqueue(obj);
    }

    /// <summary>
    /// 특정 시간 후 자동 반환
    /// </summary>
    public IEnumerator ReleaseAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Release(obj);
    }

    /// <summary>
    /// 새로운 네트워크 객체 생성 (PhotonView 연동)
    /// </summary>
    private GameObject CreateNewObject(string key, GameObject prefab)
    {
        // PhotonView가 있으면 PhotonNetwork.Instantiate 대신 로컬 Instantiate
        // 네트워크 관리되는 오브젝트는 RPC로 생성되기 때문에 여기서는 로컬만 사용
        GameObject obj = Instantiate(prefab, transform);
        obj.name = key;
        return obj;
    }
}