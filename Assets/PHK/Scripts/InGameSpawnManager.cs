using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class InGameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("������")]
    [Tooltip("�ν��Ͻ�ȭ�� ���� ������")]
    public GameObject basePrefab;

    [Header("���� ��ġ")]
    public Transform p1_spawnPoint;
    public Transform p2_spawnPoint;

    void Start()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            Debug.Log("<color=yellow>����� ���: ��Ʈ��ũ ���� ���� ���� ������ �����մϴ�.</color>");
            SpawnBasesLocally();
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitForPlayersAndSpawn());
        }
    }

    private IEnumerator WaitForPlayersAndSpawn()
    {
        yield return new WaitUntil(() => PhotonNetwork.CurrentRoom.PlayerCount == 2);
        Debug.Log("��� �÷��̾ �����߽��ϴ�. ���� ������ �����մϴ�.");
        SpawnAllBasesOverNetwork();
    }

    /// <summary>
    /// ����� ���� ���� ���� ���� �޼���
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

        // [����] P1 ������ �±׿� ���̾ ��Ȯ�ϰ� ����
        p1BaseObject.tag = "BaseP1";
        p1BaseObject.layer = LayerMask.NameToLayer("P1Base");

        KYG.BaseController p1Controller = p1BaseObject.GetComponentInChildren<KYG.BaseController>();
        if (p1Controller != null)
        {
            InGameManager.Instance.RegisterBase(p1Controller, "BaseP1");
            p1Controller.InitializeTeam("P1");
        }
        Debug.Log("����� ���: P1 ���� ���� �Ϸ� (Tag: BaseP1, Layer: P1Base)");

        // P2 ���� ���� �� ���
        Vector3 p2SpawnPosition = p2_spawnPoint.position - (p2_spawnPoint.right * offsetDistance);
        GameObject p2BaseObject = Instantiate(basePrefab, p2SpawnPosition, Quaternion.identity);

        // [����] P2 ������ �±׿� ���̾ ��Ȯ�ϰ� ����
        p2BaseObject.tag = "BaseP2";
        p2BaseObject.layer = LayerMask.NameToLayer("P2Base");

        // [����] ���� �������� �����ϸ鼭 x�ุ �������� P1�� ũ�Ⱑ �����ϰ� �����ǵ��� ����
        Vector3 originalScale = p2BaseObject.transform.localScale;
        p2BaseObject.transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);

        KYG.BaseController p2Controller = p2BaseObject.GetComponentInChildren<KYG.BaseController>();
        if (p2Controller != null)
        {
            InGameManager.Instance.RegisterBase(p2Controller, "BaseP2");
            p2Controller.InitializeTeam("P2");
        }
        Debug.Log("����� ���: P2 ���� ���� �Ϸ� (Tag: BaseP2, Layer: P2Base)");
    }
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
                // P2 ���� ������ (��Ʈ��ũ ȯ�濡���� localScale�� ���)
                PhotonView pv = p2BaseObject.GetComponent<PhotonView>();
              //  if (pv != null)
              //  {
              //      // RPC�� ���� ��� Ŭ���̾�Ʈ���� P2 ������ �������� �մϴ�.
              //      pv.RPC("FlipObjectX", RpcTarget.AllBuffered);
              //  }

                pv.TransferOwnership(players[1]);
                Debug.Log($"P2 ������ �������� {players[1].NickName}���� �����߽��ϴ�.");
            }
        }
    }

    // ��Ʈ��ũ ����ȭ�� ���� RPC �޼��� �߰�
    [PunRPC]
    public void FlipObjectX()
    {
        // �ڽ��� localScale.x ���� -1�� �����Ͽ� �������ϴ�.
        transform.localScale = new Vector3(-1f, 1f, 1f);
    }
}