using Photon.Pun;
using UnityEngine;

public class InGameCameraManager : MonoBehaviourPunCallbacks
{
    [Header("ī�޶� ������ (Inspector���� �Ҵ�")]
    public GameObject playerCameraPrefab;

    // OnEnable, OnDisable�� ���� ������ �����ϴ�.
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
        // ����� ������� ���� Ȯ���մϴ�.
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            Debug.Log("<color=yellow>����� ���: ȣ��Ʈ�� ���� ī�޶� �����մϴ�.</color>");
            InitializeDebugCamera();
        }
        // ����� ��尡 �ƴ� ���, ���� ��Ʈ��ũ ������ �����մϴ�.
        else
        {
            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                Debug.Log("��Ʈ��ũ ���: ī�޶� �ν��Ͻ�ȭ�� �õ��մϴ�.");
                InitializePlayerCamera();
            }
        }
    }

    public override void OnJoinedRoom()
    {
        // ����� ��忡���� �� �ݹ��� ������� �ʵ��� �����մϴ�.
        if (InGameManager.Instance != null && InGameManager.Instance.isDebugMode)
        {
            return;
        }

        Debug.Log("�뿡 �����Ͽ� ī�޶� �ν��Ͻ�ȭ�մϴ�.");
        InitializePlayerCamera();
    }

    /// <summary>
    /// ����� ����� �� ȣ��Ǵ� ���� ī�޶� ���� �޼���
    /// </summary>
    private void InitializeDebugCamera()
    {
        if (playerCameraPrefab == null)
        {
            Debug.LogError("playerCameraPrefab�� �Ҵ���� �ʾҽ��ϴ�!");
            return;
        }

        // ȣ��Ʈ(������ Ŭ���̾�Ʈ)�� ��ġ�� ī�޶� �����մϴ�.
        Vector3 cameraSpawnPosition = new Vector3(-10f, 0f, -10f);
        Instantiate(playerCameraPrefab, cameraSpawnPosition, Quaternion.identity);

        Debug.Log("����׿� ���� ī�޶� ������ �Ϸ�Ǿ����ϴ�.");
    }

    /// <summary>
    /// ��Ʈ��ũ ȯ�濡�� �÷��̾� ī�޶� �����ϴ� ���� �޼���
    /// </summary>
    private void InitializePlayerCamera()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("InitializePlayerCamera ȣ�� ����: �濡 �������� ���� �����Դϴ�.");
            return;
        }

        if (playerCameraPrefab == null)
        {
            Debug.LogError("playerCameraPrefab�� �Ҵ���� �ʾҽ��ϴ�!");
            return;
        }

        // �̹� �� ī�޶� �����ϴ��� Ȯ���Ͽ� �ߺ� ������ �����մϴ�.
        foreach (var cam in Camera.allCameras)
        {
            PhotonView pv = cam.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Debug.Log("�̹� �� ������ ī�޶� �����Ͽ� ���� �������� �ʽ��ϴ�.");
                EnableOnlyMyCamera(); // ���� ī�޶� Ȱ��ȭ ������ �ٽ� ����
                return;
            }
        }

        Vector3 cameraSpawnPosition = GetCameraSpawnMasterClient();
        PhotonNetwork.Instantiate(playerCameraPrefab.name, cameraSpawnPosition, Quaternion.identity);
        EnableOnlyMyCamera();
    }

    private void EnableOnlyMyCamera()
    {
        Camera[] allNetworkCameras = FindObjectsOfType<Camera>(true); // ��Ȱ��ȭ�� ī�޶� �����Ͽ� �˻�
        bool hasMyAudioListener = false;

        foreach (Camera cam in allNetworkCameras)
        {
            PhotonView pv = cam.GetComponent<PhotonView>();
            if (pv != null) // PhotonView�� �ִ� ī�޶� ó��
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
                    Debug.LogWarning("�� ī�޶� AudioListener�� ���� �ڵ����� �߰��߽��ϴ�.");
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