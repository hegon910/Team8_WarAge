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
        Unit unitData = unitPrefab.GetComponent<UnitController>().unitdata;

        // ���� ������ ���� ������ �ƴ����� ���� ������ �и�
        if (isProducing)
        {
            // --- �̹� �ٸ� ������ ���� ���� ���: ��⿭(Queue)�� �߰� ---
            if (productionQueue.Count < 5) // ť�� 5ĭ
            {

                if (InGameManager.SpendGold(unitData.goldCost))
                {
                    productionQueue.Enqueue((unitPrefab, ownerTag));
                    OnQueueChanged?.Invoke(productionQueue.Count);
                    // UI �ؽ�Ʈ�� "��⿭�� �߰���"���� ��Ȯ�� ǥ���մϴ�.
                    InGameUIManager.Instance.UnitInfoText.text = $"{unitPrefab.name} added to queue.";
                }
                else
                {
                    // ��� ���� �� UI�� �ǵ���� �ݴϴ�.

                    InGameUIManager.Instance.UnitInfoText.text = $"Faild to Spawn {unitPrefab.name}...";
                    InGameUIManager.Instance.inGameInfoText.text = "Not enough gold!";
                }
            }
            else
            {
                InGameUIManager.Instance.inGameInfoText.text = "Production Queue is Full!"; // ��⿭�� ���� á�� �� �˸�
            }
        }
        else
        {
            // --- ���� ������ ������� ���: �ٷ� ���� ���� ---
            // ��� ������ ���� �õ��ϰ� �����ϸ� ������ ����
            if (InGameManager.SpendGold(unitData.goldCost))
            {
                StartCoroutine(ProcessSingleUnit(unitPrefab, ownerTag));
            }
            else
            {
                // ��� ���� �� UI�� �ǵ����.
                InGameUIManager.Instance.inGameInfoText.text = "Not enough gold!";
            }
        }

    }
    /// <summary>
    /// ���� '�� ��'�� �����ϰ�, �Ϸ�Ǹ� ��⿭���� ���� ������ ������ �ٽ� �� �ڷ�ƾ�� ����
    /// </summary>
    private IEnumerator ProcessSingleUnit(GameObject prefabToProduce, string ownerTag)
    {


        Vector3 initialMoveDirection = (ownerTag == "P1") ? Vector3.right : Vector3.left;
        // --- ���� ���� ó�� ---
        isProducing = true;
        OnProductionStatusChanged?.Invoke(true); // �����̴� Ȱ��ȭ
        OnProductionProgress?.Invoke(0f);      // �����̴� 0%���� ����

        Unit unitData = prefabToProduce.GetComponent<UnitController>().unitdata;

        // --- ���� �ð� ���� ��� ---
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
        OnProductionProgress?.Invoke(1f); // 100% ä���

        Transform spawnPoint = (ownerTag == "P1") ? p1_spawnPoint : p2_spawnPoint;
        if (spawnPoint != null)
        {
            // ����� ���� ���� ��Ʈ��ũ ȯ���� �б�
            if (InGameManager.isDebugMode)
            {
                // --- ����� ���: �Ϲ� Instantiate ��� ---
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
                // UnitController�� public ������ ���� �����Ͽ� ������ ����
                UnitController controller = newUnit.GetComponent<UnitController>();
                if (controller != null)
                {
                    controller.moveDirection = initialMoveDirection;
                }

            }
            else
            {
                // --- ���� ��Ʈ��ũ ȯ��: PhotonNetwork.Instantiate ��� ---
                object[] data = new object[] { ownerTag, initialMoveDirection };

                // 1. Photon���� ������ �����ϰ�, ������ ���ӿ�����Ʈ�� ������ �����մϴ�.
                GameObject newUnit = PhotonNetwork.Instantiate(prefabToProduce.name, spawnPoint.position, spawnPoint.rotation, 0, data);

                // 2. ������ ������ PhotonView�� �����ɴϴ�.
                PhotonView newUnitPV = newUnit.GetComponent<PhotonView>();
                if (newUnitPV != null)
                {
                    // 3. ��� Ŭ���̾�Ʈ���� �±׿� ���̾ �����϶�� RPC�� ȣ���մϴ�.
                    //    (UnitSpawnManager �ڽ��� PhotonView�� �̿��� RPC�� ����)
                    GetComponent<PhotonView>().RPC("RPC_SetUnitTag", RpcTarget.AllBuffered, newUnitPV.ViewID, ownerTag);
                }
            }
        }

        // --- �ļ� ó��: ��⿭�� ���� ������ �ִ��� Ȯ�� ---
        if (productionQueue.Count > 0)
        {
            var nextUnit = productionQueue.Dequeue();
            OnQueueChanged?.Invoke(productionQueue.Count);
            StartCoroutine(ProcessSingleUnit(nextUnit.prefab, nextUnit.ownerTag));
        }
        else
        {
            isProducing = false;
            OnProductionStatusChanged?.Invoke(false); // �����̴� ��Ȱ��ȭ
            OnProductionProgress?.Invoke(0f);         // �����̴� 0%�� ����
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
