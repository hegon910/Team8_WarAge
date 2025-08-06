using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawnManager : MonoBehaviour
{
    public static UnitSpawnManager Instance { get; private set; }
    public InGameManager InGameManager => InGameManager.Instance;

    [Header("���� ���� ����")]
    [SerializeField] Transform p1_spawnPoint;
    [SerializeField] Transform p2_spawnPoint;

    //��⿭
    private Queue<(GameObject prefab, string ownerTag)> productionQueue = new Queue<(GameObject prefab, string ownerTag)>();
    //���� ���� ������ ���� ������
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
    #region ���� ���� ����
    ///<summary>
    ///�������� ���� ť�� �߰��ϴ� �Լ�
    /// </summary>
    /// 
    public void RequestUnitProduction(GameObject unitPrefab, string ownerTag)
    {
        // ��û�ڰ� AI("P2")�̰� ����� ����� ���, ������ AI ���� ������ ��� ����
        if (ownerTag == "P2" && InGameManager.isDebugMode)
        {
            SpawnAIUnit(unitPrefab);
            return; // AI ���� ������ ���������Ƿ� �Ʒ� �÷��̾� ������ �ǳʶݴϴ�.
        }

        bool isPlayerRequest = (ownerTag == "P1");

        if (isProducing)
        {
            if (productionQueue.Count < 5)
            {
                productionQueue.Enqueue((unitPrefab, ownerTag));
                if (isPlayerRequest)
                {
                    OnQueueChanged?.Invoke(productionQueue.Count);
                    InGameUIManager.Instance.UnitInfoText.text = $"{unitPrefab.name} added to queue.";
                }
            }
            else
            {
                if (isPlayerRequest)
                {
                    InGameUIManager.Instance.inGameInfoText.text = "Production Queue is Full!";
                }
            }
        }
        else
        {
            StartCoroutine(ProcessSingleUnit(unitPrefab, ownerTag, isPlayerRequest));
        }
    }
    private void SpawnAIUnit(GameObject prefabToProduce)
    {
        Transform spawnPoint = p2_spawnPoint;
        if (spawnPoint == null) return;

        Quaternion initialRotation = Quaternion.Euler(0, 180, 0); // P2 ������ �׻� ����� ����
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
    /// ���� '�� ��'�� �����ϰ�, �Ϸ�Ǹ� ��⿭���� ���� ������ ������ �ٽ� �� �ڷ�ƾ�� ����
    /// </summary>
    private IEnumerator ProcessSingleUnit(GameObject prefabToProduce, string ownerTag, bool isPlayerRequest)
    {

        Vector3 initialMoveDirection = (ownerTag == "P1") ? Vector3.right : Vector3.left;
        isProducing = true;

        if (isPlayerRequest)
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
                if (isPlayerRequest)
                {
                    OnProductionProgress?.Invoke(Mathf.Clamp01(timer / unitData.SpawnTime));
                    int percent = (int)((timer / unitData.SpawnTime) * 100f);
                    InGameUIManager.Instance.UnitInfoText.text = $" Spawning : {prefabToProduce.name}. . . .{percent}%";
                }
                yield return null;
            }
        }
        if (isPlayerRequest) OnProductionProgress?.Invoke(1f);

        Transform spawnPoint = (ownerTag == "P1") ? p1_spawnPoint : p2_spawnPoint;
        if (spawnPoint != null)
        {
            if (InGameManager.isDebugMode)
            {
                // ����� ��忡�� P2 ������ ������ ���� �����մϴ�.
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
                GameObject newUnit = PhotonNetwork.Instantiate(prefabToProduce.name, spawnPoint.position, spawnPoint.rotation, 0, data);

                // ������ ������ PhotonView�� �����ɴϴ�.
                PhotonView newUnitPV = newUnit.GetComponent<PhotonView>();
                if (newUnitPV != null)
                {
                    // ��� Ŭ���̾�Ʈ���� �±׿� ���̾ �����϶�� RPC�� ȣ���մϴ�.
                    //    (UnitSpawnManager �ڽ��� PhotonView�� �̿��� RPC�� ����)
                    GetComponent<PhotonView>().RPC("RPC_SetUnitTag", RpcTarget.AllBuffered, newUnitPV.ViewID, ownerTag);
                }
            }
        }

        // --- �ļ� ó��: ��⿭�� ���� ������ �ִ��� Ȯ�� ---
        if (productionQueue.Count > 0)
        {
            var nextUnit = productionQueue.Dequeue();
            bool isNextPlayerRequest = (nextUnit.ownerTag == "P1");
            if (isNextPlayerRequest) OnQueueChanged?.Invoke(productionQueue.Count);
            StartCoroutine(ProcessSingleUnit(nextUnit.prefab, nextUnit.ownerTag, isNextPlayerRequest));
        }
        else
        {
            isProducing = false;
            if (isPlayerRequest)
            {
                OnProductionStatusChanged?.Invoke(false);
                OnProductionProgress?.Invoke(0f);
                InGameUIManager.Instance.UnitInfoText.text = $"{prefabToProduce.name} has Spawned!!";
            }
        }

        InGameUIManager.Instance.UnitInfoText.text = $"{prefabToProduce.name} has Spawned!!";
    }
    ///<summary>
    ///��� Ŭ���̾�Ʈ���� Ư�� ������ �±׸� ������
    /// </summary>
    [PunRPC]
    private void RPC_SetUnitTag(int viewID, string tag)
    {
        PhotonView targetPV = PhotonView.Find(viewID);
        if (targetPV != null)
        {
            targetPV.gameObject.tag = tag;
            // *** START: ��û�� ���� ���̾� ���� �߰� ***
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
