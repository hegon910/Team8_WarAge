using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawnManager : MonoBehaviour
{
    public static UnitSpawnManager Instance { get; private set; }
    public InGameManager InGameManager => InGameManager.Instance;

    [Header("유닛 생성 관련")]
    [SerializeField] Transform p1_spawnPoint;
    [SerializeField] Transform p2_spawnPoint;

    //대기열
    private Queue<(GameObject prefab, string ownerTag)> productionQueue = new Queue<(GameObject prefab, string ownerTag)>();
    //현재 생산 라인이 가동 중인지
    private bool isProducing = false;

    public event Action<int> OnQueueChanged;
    public event Action<float> OnProductionProgress;
    public event Action<bool> OnProductionStatusChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #region 유닛 생산 로직
    ///<summary>
    ///프리팹을 생산 큐에 추가하는 함수
    /// </summary>
    /// 
    public void RequestUnitProduction(GameObject unitPrefab, string ownerTag)
    {


        InGameUIManager.Instance.UnitInfoText.text = $"Producing {unitPrefab.name}...";
        // 현재 유닛이 생산 중인지 아닌지에 따라 로직을 분리
        if (isProducing)
        {
            // --- 이미 다른 유닛이 생산 중일 경우: 대기열(Queue)에 추가 ---
            if (productionQueue.Count < 5) // 큐는 5칸
            {

                InGameUIManager.Instance.UnitInfoText.text = $"{unitPrefab.name} is Ready to Spawn...";
                Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
                if (InGameManager.SpendGold(unitData.goldCost))
                {
                    productionQueue.Enqueue((unitPrefab, ownerTag));
                    // 대기열 UI 업데이트 (현재 대기중인 유닛 수만 전달)
                    OnQueueChanged?.Invoke(productionQueue.Count);
                }
            }
            else
            {
                InGameUIManager.Instance.inGameInfoText.text = "Production Queue is Full!"; // 대기열이 가득 찼을 때 알림
            }
        }
        else
        {
            // --- 생산 라인이 비어있을 경우: 바로 생산 시작 ---
            Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;
            if (InGameManager.SpendGold(unitData.goldCost))
            {
                // 이 유닛은 큐를 거치지 않고 바로 생산 코루틴으로 전달
                StartCoroutine(ProcessSingleUnit(unitPrefab, ownerTag));
            }
        }
    }
    /// <summary>
    /// 유닛 '한 개'를 생산하고, 완료되면 대기열에서 다음 유닛을 가져와 다시 이 코루틴을 실행
    /// </summary>
    private IEnumerator ProcessSingleUnit(GameObject prefabToProduce, string ownerTag)
    {


        Vector3 initialMoveDirection = (ownerTag == "P1") ? Vector3.right : Vector3.left;
        // --- 생산 시작 처리 ---
        isProducing = true;
        OnProductionStatusChanged?.Invoke(true); // 슬라이더 활성화
        OnProductionProgress?.Invoke(0f);      // 슬라이더 0%에서 시작

        Unit unitData = prefabToProduce.GetComponent<UnitController>().unitdata;

        // --- 생산 시간 동안 대기 ---
        if (unitData.SpawnTime > 0)
        {
            float timer = 0f;
            while (timer < unitData.SpawnTime)
            {
                timer += Time.deltaTime;
                OnProductionProgress?.Invoke(Mathf.Clamp01(timer / unitData.SpawnTime));
                int percent = (int)((timer / unitData.SpawnTime) * 100f);
                InGameUIManager.Instance.UnitInfoText.text = $" Spawning : {prefabToProduce.name}. . . .{percent}%";
                yield return null;
            }
        }
        OnProductionProgress?.Invoke(1f); // 100% 채우기

        Transform spawnPoint = (ownerTag == "P1") ? p1_spawnPoint : p2_spawnPoint;
        if (spawnPoint != null)
        {
            // 디버그 모드와 실제 네트워크 환경을 분기
            if (InGameManager.isDebugMode)
            {
                // --- 디버그 모드: 일반 Instantiate 사용 ---
                GameObject newUnit = Instantiate(prefabToProduce, spawnPoint.position, spawnPoint.rotation);
                newUnit.tag = ownerTag;
                if (ownerTag == "P1")
                {
                    newUnit.layer = LayerMask.NameToLayer("P1Unit");
                }
                else if (ownerTag == "P2")
                {
                    newUnit.layer = LayerMask.NameToLayer("P2Unit");
                }
                // UnitController의 public 변수에 직접 접근하여 방향을 설정
                UnitController controller = newUnit.GetComponent<UnitController>();
                if (controller != null)
                {
                    controller.moveDirection = initialMoveDirection;
                }

            }
            else
            {
                // --- 실제 네트워크 환경: PhotonNetwork.Instantiate 사용 ---
                object[] data = new object[] { ownerTag, initialMoveDirection };
                PhotonNetwork.Instantiate(prefabToProduce.name, spawnPoint.position, spawnPoint.rotation, 0, data);
            }
        }

        // --- 후속 처리: 대기열에 다음 유닛이 있는지 확인 ---
        if (productionQueue.Count > 0)
        {
            var nextUnit = productionQueue.Dequeue();
            OnQueueChanged?.Invoke(productionQueue.Count);
            StartCoroutine(ProcessSingleUnit(nextUnit.prefab, nextUnit.ownerTag));
        }
        else
        {
            isProducing = false;
            OnProductionStatusChanged?.Invoke(false); // 슬라이더 비활성화
            OnProductionProgress?.Invoke(0f);         // 슬라이더 0%로 리셋
        }

        InGameUIManager.Instance.UnitInfoText.text = $"{prefabToProduce.name} has Spawned!!";
    }
    ///<summary>
    ///모든 클라이언트에서 특정 유닛의 태그를 설정함
    /// </summary>
    [PunRPC]
    private void RPC_SetUnitTag(int viewID, string tag)
    {
        PhotonView targetPV = PhotonView.Find(viewID);
        if (targetPV != null)
        {
            targetPV.gameObject.tag = tag;
            // *** START: 요청에 따른 레이어 설정 추가 ***
            if (tag == "P1")
            {
                targetPV.gameObject.layer = LayerMask.NameToLayer("P1Unit");
            }
            else if (tag == "P2")
            {
                targetPV.gameObject.layer = LayerMask.NameToLayer("P2Unit");
            }
        }
        else
        {
            Debug.LogWarning($"PhotonView with ID {viewID} not found for setting tag to {tag}.");
        }
    }
    #endregion

}
