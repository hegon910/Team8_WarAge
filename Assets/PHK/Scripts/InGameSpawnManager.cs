using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections; // 코루틴 사용을 위해 추가

public class InGameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("프리팹")]
    [Tooltip("인스턴스화할 기지 프리팹")]
    public GameObject basePrefab;

    [Header("생성 위치")]
    public Transform p1_spawnPoint;
    public Transform p2_spawnPoint;

    // --- 수정된 부분 ---
    void Start()
    {
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            Debug.Log("<color=yellow>디버그 모드: 네트워크 연결 없이 로컬 기지를 생성합니다.</color>");
            SpawnBasesLocally();
        }
        // 네트워크 모드에서는 마스터 클라이언트가 코루틴을 시작
        else if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitForPlayersAndSpawn());
        }
    }

    // --- 새로 추가된 코루틴 ---
    /// <summary>
    /// 방에 2명이 될 때까지 기다렸다가 모든 기지를 생성하는 코루틴.
    /// </summary>
    private IEnumerator WaitForPlayersAndSpawn()
    {
        // 현재 방의 플레이어 수가 2명이 될 때까지 매 프레임 체크하며 기다림
        yield return new WaitUntil(() => PhotonNetwork.CurrentRoom.PlayerCount == 2);

        // 조건이 만족되면 기지 생성 함수를 호출
        Debug.Log("모든 플레이어가 참여했습니다. 기지 생성을 시작합니다.");
        SpawnAllBasesOverNetwork();
    }

    /// <summary>
    /// 디버그 모드용 로컬 기지 생성 메서드 (기존과 동일)
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
        p1BaseObject.tag = "BaseP1";
        KYG.BaseController p1Controller = p1BaseObject.GetComponent<KYG.BaseController>();
        if (p1Controller != null)
        {
            InGameManager.Instance.RegisterBase(p1Controller, "BaseP1");
            p1Controller.InitializeTeam("P1");
        }
        Debug.Log("디버그 모드: P1 기지를 생성하고 등록했습니다.");


        // P2 기지 생성 및 등록
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
        Debug.Log("디버그 모드: P2 기지를 생성하고 등록했습니다.");
    }

    /// <summary>
    /// 포톤 네트워크를 통한 기지 생성 메서드 (기존과 동일)
    /// </summary>
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
                p2BaseObject.GetComponent<PhotonView>().TransferOwnership(players[1]);
                Debug.Log($"P2 기지의 소유권을 {players[1].NickName}에게 이전했습니다.");
            }
        }
    }
}