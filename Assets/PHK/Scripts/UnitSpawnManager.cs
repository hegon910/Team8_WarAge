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
        // 요청자가 AI("P2")이고 디버그 모드일 경우, 별도의 AI 생산 로직을 즉시 실행
        if (ownerTag == "P2" && InGameManager.isDebugMode)
        {
            SpawnAIUnit(unitPrefab);
            return; // AI 생산 로직을 실행했으므로 아래 플레이어 로직은 건너뜁니다.
        }

        // [수정] "이 요청이 내 컴퓨터에서 보낸 요청인가?"를 확인합니다.
        string myOwnerTag = InGameManager.isDebugMode ? "P1" : (PhotonNetwork.LocalPlayer.IsMasterClient ? "P1" : "P2");
        bool isMyRequest = (ownerTag == myOwnerTag);

        if (isProducing)
        {
            if (productionQueue.Count < 5)
            {
                productionQueue.Enqueue((unitPrefab, ownerTag));
                // [수정] P1이든 P2든 자신의 요청이면 UI를 업데이트합니다.
                if (isMyRequest)
                {
                    OnQueueChanged?.Invoke(productionQueue.Count);
                    InGameUIManager.Instance.UnitInfoText.text = $"{unitPrefab.name} added to queue.";
                }
            }
            else
            {
                // [수정] P1이든 P2든 자신의 요청이면 UI를 업데이트합니다.
                if (isMyRequest)
                {
                    InGameUIManager.Instance.inGameInfoText.text = "Production Queue is Full!";
                }
            }
        }
        else
        {
            // [수정] isMyRequest 변수를 코루틴에 전달합니다.
            StartCoroutine(ProcessSingleUnit(unitPrefab, ownerTag, isMyRequest));
        }
    }
    private void SpawnAIUnit(GameObject prefabToProduce)
    {
        Transform spawnPoint = p2_spawnPoint;
        if (spawnPoint == null) return;

        Quaternion initialRotation = Quaternion.Euler(0, 180, 0); // P2 유닛은 항상 뒤집어서 생성
        GameObject newUnit = Instantiate(prefabToProduce, spawnPoint.position, initialRotation);

        newUnit.tag = "P2";
        newUnit.layer = LayerMask.NameToLayer("P2Unit");

        UnitController controller = newUnit.GetComponent<UnitController>();
        if (controller != null)
        {
            controller.moveDirection = Vector3.left;
        }
    }
    /// <summary>
    /// 유닛 '한 개'를 생산하고, 완료되면 대기열에서 다음 유닛을 가져와 다시 이 코루틴을 실행
    /// </summary>
    // [수정] isPlayerRequest 매개변수 이름을 isMyRequest로 명확하게 변경
    private IEnumerator ProcessSingleUnit(GameObject prefabToProduce, string ownerTag, bool isMyRequest)
    {

        Vector3 initialMoveDirection = (ownerTag == "P1") ? Vector3.right : Vector3.left;
        isProducing = true;

        // [수정] "이 요청이 내 컴퓨터에서 보낸 요청인가?"를 확인하는 로직 (코루틴 내부에서 재확인)
        string myOwnerTag = InGameManager.isDebugMode ? "P1" : (PhotonNetwork.LocalPlayer.IsMasterClient ? "P1" : "P2");
        bool isMyCurrentRequest = (ownerTag == myOwnerTag);

        if (isMyCurrentRequest)
        {
            OnProductionStatusChanged?.Invoke(true);
            OnProductionProgress?.Invoke(0f);
        }

        Unit unitData = prefabToProduce.GetComponent<UnitController>().unitdata;

        if (unitData.SpawnTime > 0)
        {
            float timer = 0f;
            while (timer < unitData.SpawnTime)
            {
                timer += Time.deltaTime;
                if (isMyCurrentRequest)
                {
                    OnProductionProgress?.Invoke(Mathf.Clamp01(timer / unitData.SpawnTime));
                    int percent = (int)((timer / unitData.SpawnTime) * 100f);
                    InGameUIManager.Instance.UnitInfoText.text = $" Spawning : {prefabToProduce.name}. . . .{percent}%";
                }
                yield return null;
            }
        }
        if (isMyCurrentRequest) OnProductionProgress?.Invoke(1f);

        Transform spawnPoint = (ownerTag == "P1") ? p1_spawnPoint : p2_spawnPoint;
        if (spawnPoint != null)
        {
            if (InGameManager.isDebugMode)
            {
                // 디버그 모드에서 P2 유닛의 방향을 직접 설정합니다.
                Quaternion initialRotation = (ownerTag == "P2") ? Quaternion.Euler(0, 180, 0) : spawnPoint.rotation;
                GameObject newUnit = Instantiate(prefabToProduce, spawnPoint.position, initialRotation);

                newUnit.tag = ownerTag;
                newUnit.layer = (ownerTag == "P1") ? LayerMask.NameToLayer("P1Unit") : LayerMask.NameToLayer("P2Unit");

                UnitController controller = newUnit.GetComponent<UnitController>();
                if (controller != null)
                {
                    controller.moveDirection = initialMoveDirection;
                }
            }
            else
            {
                object[] data = new object[] { ownerTag, initialMoveDirection };
                // PhotonNetwork.Instantiate는 마스터 클라이언트에서만 호출하는 것이 안정적이지만, 기존 로직을 유지합니다.
                GameObject newUnit = PhotonNetwork.Instantiate(prefabToProduce.name, spawnPoint.position, spawnPoint.rotation, 0, data);

                PhotonView newUnitPV = newUnit.GetComponent<PhotonView>();
                if (newUnitPV != null && GetComponent<PhotonView>() != null)
                {
                    GetComponent<PhotonView>().RPC("RPC_SetUnitTag", RpcTarget.AllBuffered, newUnitPV.ViewID, ownerTag);
                }
            }
        }

        // --- 후속 처리: 대기열에 다음 유닛이 있는지 확인 ---
        if (productionQueue.Count > 0)
        {
            var nextUnit = productionQueue.Dequeue();

            // [수정] 다음 유닛이 내 요청인지 확인합니다.
            bool isNextMyRequest = (nextUnit.ownerTag == myOwnerTag);
            if (isNextMyRequest) OnQueueChanged?.Invoke(productionQueue.Count);

            StartCoroutine(ProcessSingleUnit(nextUnit.prefab, nextUnit.ownerTag, isNextMyRequest));
        }
        else
        {
            isProducing = false;
            if (isMyCurrentRequest)
            {
                OnProductionStatusChanged?.Invoke(false);
                OnProductionProgress?.Invoke(0f);
                InGameUIManager.Instance.UnitInfoText.text = $"{prefabToProduce.name} has Spawned!!";
            }
        }

        // 이 라인은 다음 유닛 생산이 시작될 때 덮어쓰여서, 마지막 유닛 정보만 남는 것이 의도된 동작으로 보입니다.
        // InGameUIManager.Instance.UnitInfoText.text = $"{prefabToProduce.name} has Spawned!!";
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