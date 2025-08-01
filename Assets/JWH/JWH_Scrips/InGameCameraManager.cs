using Photon.Pun;
using UnityEngine;

public class InGameCameraManager : MonoBehaviourPunCallbacks
{
    [Header("카메라 프리팹 (Inspector에서 할당")]
    public GameObject playerCameraPrefab;

    // OnEnable, OnDisable은 변경 사항이 없습니다.
    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    void Start()
    {
        // 디버그 모드인지 먼저 확인합니다.
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            Debug.Log("<color=yellow>디버그 모드: 호스트용 로컬 카메라를 생성합니다.</color>");
            InitializeDebugCamera();
        }
        // 디버그 모드가 아닐 경우, 기존 네트워크 로직을 실행합니다.
        else
        {
            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                Debug.Log("네트워크 모드: 카메라 인스턴스화를 시도합니다.");
                InitializePlayerCamera();
            }
        }
    }

    public override void OnJoinedRoom()
    {
        // 디버그 모드에서는 이 콜백이 실행되지 않도록 방지합니다.
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            return;
        }

        Debug.Log("룸에 조인하여 카메라를 인스턴스화합니다.");
        InitializePlayerCamera();
    }

    /// <summary>
    /// 디버그 모드일 때 호출되는 로컬 카메라 생성 메서드
    /// </summary>
    private void InitializeDebugCamera()
    {
        if (playerCameraPrefab == null)
        {
            Debug.LogError("playerCameraPrefab이 할당되지 않았습니다!");
            return;
        }

        // 호스트(마스터 클라이언트)의 위치에 카메라를 생성합니다.
        Vector3 cameraSpawnPosition = new Vector3(-10f, 0f, -10f);
        Instantiate(playerCameraPrefab, cameraSpawnPosition, Quaternion.identity);

        Debug.Log("디버그용 로컬 카메라 생성이 완료되었습니다.");
    }

    /// <summary>
    /// 네트워크 환경에서 플레이어 카메라를 생성하는 기존 메서드
    /// </summary>
    private void InitializePlayerCamera()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("InitializePlayerCamera 호출 실패: 방에 입장하지 않은 상태입니다.");
            return;
        }

        if (playerCameraPrefab == null)
        {
            Debug.LogError("playerCameraPrefab이 할당되지 않았습니다!");
            return;
        }

        // 이미 내 카메라가 존재하는지 확인하여 중복 생성을 방지합니다.
        foreach (var cam in Camera.allCameras)
        {
            PhotonView pv = cam.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Debug.Log("이미 내 소유의 카메라가 존재하여 새로 생성하지 않습니다.");
                EnableOnlyMyCamera(); // 기존 카메라 활성화 로직만 다시 실행
                return;
            }
        }

        Vector3 cameraSpawnPosition = GetCameraSpawnMasterClient();
        PhotonNetwork.Instantiate(playerCameraPrefab.name, cameraSpawnPosition, Quaternion.identity);
        EnableOnlyMyCamera();
    }

    private void EnableOnlyMyCamera()
    {
        Camera[] allNetworkCameras = FindObjectsOfType<Camera>(true); // 비활성화된 카메라도 포함하여 검색
        bool hasMyAudioListener = false;

        foreach (Camera cam in allNetworkCameras)
        {
            PhotonView pv = cam.GetComponent<PhotonView>();
            if (pv != null) // PhotonView가 있는 카메라만 처리
            {
                bool isMine = pv.IsMine;
                cam.enabled = isMine;
                AudioListener audioListener = cam.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = isMine;
                    if (isMine)
                    {
                        hasMyAudioListener = true;
                    }
                }
            }
        }

        if (!hasMyAudioListener)
        {
            foreach (var cam in allNetworkCameras)
            {
                PhotonView pv = cam.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine && cam.GetComponent<AudioListener>() == null)
                {
                    cam.gameObject.AddComponent<AudioListener>();
                    Debug.LogWarning("내 카메라에 AudioListener가 없어 자동으로 추가했습니다.");
                    break;
                }
            }
        }
    }

    private Vector3 GetCameraSpawnMasterClient()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            return new Vector3(-10f, 0f, -10f);
        }
        else
        {
            return new Vector3(10f, 0f, -10f);
        }
    }
}