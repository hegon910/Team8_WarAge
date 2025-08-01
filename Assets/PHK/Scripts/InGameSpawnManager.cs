using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections; // �ڷ�ƾ ����� ���� �߰�

public class InGameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("������")]
    [Tooltip("�ν��Ͻ�ȭ�� ���� ������")]
    public GameObject basePrefab;

    [Header("���� ��ġ")]
    public Transform p1_spawnPoint;
    public Transform p2_spawnPoint;

    // --- ������ �κ� ---
    void Start()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            Debug.Log("<color=yellow>����� ���: ��Ʈ��ũ ���� ���� ���� ������ �����մϴ�.</color>");
            SpawnBasesLocally();
        }
        // ��Ʈ��ũ ��忡���� ������ Ŭ���̾�Ʈ�� �ڷ�ƾ�� ����
        else if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitForPlayersAndSpawn());
        }
    }

    // --- ���� �߰��� �ڷ�ƾ ---
    /// <summary>
    /// �濡 2���� �� ������ ��ٷȴٰ� ��� ������ �����ϴ� �ڷ�ƾ.
    /// </summary>
    private IEnumerator WaitForPlayersAndSpawn()
    {
        // ���� ���� �÷��̾� ���� 2���� �� ������ �� ������ üũ�ϸ� ��ٸ�
        yield return new WaitUntil(() => PhotonNetwork.CurrentRoom.PlayerCount == 2);

        // ������ �����Ǹ� ���� ���� �Լ��� ȣ��
        Debug.Log("��� �÷��̾ �����߽��ϴ�. ���� ������ �����մϴ�.");
        SpawnAllBasesOverNetwork();
    }

    /// <summary>
    /// ����� ���� ���� ���� ���� �޼��� (������ ����)
    /// </summary>
    private void SpawnBasesLocally()
    {
        if (basePrefab == null)
        {
            Debug.LogError("Base Prefab�� InGameSpawnManager�� �Ҵ���� �ʾҽ��ϴ�!");
            return;
        }

        if (InGameManager.Instance == null)
        {
            Debug.LogError("InGameManager�� ã�� �� ���� ����� ������ ����� �� �����ϴ�.");
            return;
        }

        float offsetDistance = 0.4f;

        // P1 ���� ���� �� ���
        Vector3 p1SpawnPosition = p1_spawnPoint.position - (p1_spawnPoint.right * offsetDistance);
        GameObject p1BaseObject = Instantiate(basePrefab, p1SpawnPosition, Quaternion.identity);
        p1BaseObject.tag = "BaseP1";
        KYG.BaseController p1Controller = p1BaseObject.GetComponent<KYG.BaseController>();
        if (p1Controller != null)
        {
            InGameManager.Instance.RegisterBase(p1Controller, "BaseP1");
            p1Controller.InitializeTeam("P1");
        }
        Debug.Log("����� ���: P1 ������ �����ϰ� ����߽��ϴ�.");


        // P2 ���� ���� �� ���
        Vector3 p2SpawnPosition = p2_spawnPoint.position - (p2_spawnPoint.right * offsetDistance);
        GameObject p2BaseObject = Instantiate(basePrefab, p2SpawnPosition, Quaternion.identity);
        p2BaseObject.tag = "BaseP2";
        SpriteRenderer p2Renderer = p2BaseObject.GetComponentInChildren<SpriteRenderer>(true);
        if (p2Renderer != null)
        {
            p2Renderer.flipX = true;
        }
        KYG.BaseController p2Controller = p2BaseObject.GetComponent<KYG.BaseController>();
        if (p2Controller != null)
        {
            InGameManager.Instance.RegisterBase(p2Controller, "BaseP2");
            p2Controller.InitializeTeam("P2");
        }
        Debug.Log("����� ���: P2 ������ �����ϰ� ����߽��ϴ�.");
    }

    /// <summary>
    /// ���� ��Ʈ��ũ�� ���� ���� ���� �޼��� (������ ����)
    /// </summary>
    private void SpawnAllBasesOverNetwork()
    {
        Player[] players = PhotonNetwork.PlayerList;
        float offsetDistance = 0.4f;

        if (players.Length >= 1)
        {
            Vector3 spawnPosition = p1_spawnPoint.position - (p1_spawnPoint.right * offsetDistance);
            PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP1" });
            Debug.Log("P1(Master Client)�� ������ �����߽��ϴ�.");
        }

        if (players.Length >= 2)
        {
            Vector3 spawnPosition = p2_spawnPoint.position - (p2_spawnPoint.right * offsetDistance);
            GameObject p2BaseObject = PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP2" });
            Debug.Log("P2�� ������ �����մϴ�.");

            if (p2BaseObject != null)
            {
                p2BaseObject.GetComponent<PhotonView>().TransferOwnership(players[1]);
                Debug.Log($"P2 ������ �������� {players[1].NickName}���� �����߽��ϴ�.");
            }
        }
    }
}