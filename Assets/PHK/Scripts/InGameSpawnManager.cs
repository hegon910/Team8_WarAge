using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class InGameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("프리팹")]
    [Tooltip("인스턴스화할 기지 프리팹")]
    public GameObject basePrefab;

    [Header("생성 위치")]
    public Transform p1_spawnPoint;
    public Transform p2_spawnPoint;

    void Start()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            Debug.Log("<color=yellow>디버그 모드: 네트워크 연결 없이 로컬 기지를 생성합니다.</color>");
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
        Debug.Log("모든 플레이어가 참여했습니다. 기지 생성을 시작합니다.");
        SpawnAllBasesOverNetwork();
    }

    /// <summary>
    /// 디버그 모드용 로컬 기지 생성 메서드
    /// </summary>
    private void SpawnBasesLocally()
    {
        if (basePrefab == null)
        {
            Debug.LogError("Base Prefab이 InGameSpawnManager에 할당되지 않았습니다!");
            return;
        }

        if (InGameManager.Instance == null)
        {
            Debug.LogError("InGameManager를 찾을 수 없어 디버그 기지를 등록할 수 없습니다.");
            return;
        }

        float offsetDistance = 0.4f;

        // P1 기지 생성 및 등록
        Vector3 p1SpawnPosition = p1_spawnPoint.position - (p1_spawnPoint.right * offsetDistance);
        GameObject p1BaseObject = Instantiate(basePrefab, p1SpawnPosition, Quaternion.identity);

        // [수정] P1 기지의 태그와 레이어를 명확하게 설정
        p1BaseObject.tag = "BaseP1";
        p1BaseObject.layer = LayerMask.NameToLayer("P1Base");

        KYG.BaseController p1Controller = p1BaseObject.GetComponentInChildren<KYG.BaseController>();
        if (p1Controller != null)
        {
            InGameManager.Instance.RegisterBase(p1Controller, "BaseP1");
            p1Controller.InitializeTeam("P1");
        }
        Debug.Log("디버그 모드: P1 기지 생성 완료 (Tag: BaseP1, Layer: P1Base)");

        // P2 기지 생성 및 등록
        Vector3 p2SpawnPosition = p2_spawnPoint.position - (p2_spawnPoint.right * offsetDistance);
        GameObject p2BaseObject = Instantiate(basePrefab, p2SpawnPosition, Quaternion.identity);

        // [수정] P2 기지의 태그와 레이어를 명확하게 설정
        p2BaseObject.tag = "BaseP2";
        p2BaseObject.layer = LayerMask.NameToLayer("P2Base");

        // [수정] 기존 스케일을 유지하면서 x축만 반전시켜 P1과 크기가 동일하게 유지되도록 수정
        Vector3 originalScale = p2BaseObject.transform.localScale;
        p2BaseObject.transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);

        KYG.BaseController p2Controller = p2BaseObject.GetComponentInChildren<KYG.BaseController>();
        if (p2Controller != null)
        {
            InGameManager.Instance.RegisterBase(p2Controller, "BaseP2");
            p2Controller.InitializeTeam("P2");
        }
        Debug.Log("디버그 모드: P2 기지 생성 완료 (Tag: BaseP2, Layer: P2Base)");
    }
    private void SpawnAllBasesOverNetwork()
    {
        Player[] players = PhotonNetwork.PlayerList;
        float offsetDistance = 0.4f;

        if (players.Length >= 1)
        {
            Vector3 spawnPosition = p1_spawnPoint.position - (p1_spawnPoint.right * offsetDistance);
            PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP1" });
            Debug.Log("P1(Master Client)의 기지를 생성했습니다.");
        }

        if (players.Length >= 2)
        {
            Vector3 spawnPosition = p2_spawnPoint.position - (p2_spawnPoint.right * offsetDistance);
            GameObject p2BaseObject = PhotonNetwork.Instantiate("BasePrefab", spawnPosition, Quaternion.identity, 0, new object[] { "BaseP2" });
            Debug.Log("P2의 기지를 생성합니다.");

            if (p2BaseObject != null)
            {
                // P2 기지 뒤집기 (네트워크 환경에서도 localScale을 사용)
                PhotonView pv = p2BaseObject.GetComponent<PhotonView>();
              //  if (pv != null)
              //  {
              //      // RPC를 통해 모든 클라이언트에서 P2 기지를 뒤집도록 합니다.
              //      pv.RPC("FlipObjectX", RpcTarget.AllBuffered);
              //  }

                pv.TransferOwnership(players[1]);
                Debug.Log($"P2 기지의 소유권을 {players[1].NickName}에게 이전했습니다.");
            }
        }
    }

    // 네트워크 동기화를 위한 RPC 메서드 추가
    [PunRPC]
    public void FlipObjectX()
    {
        // 자신의 localScale.x 값을 -1로 설정하여 뒤집습니다.
        transform.localScale = new Vector3(-1f, 1f, 1f);
    }
}