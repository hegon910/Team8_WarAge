using Photon.Pun;
using UnityEngine;


public class InGameCameraManager : MonoBehaviourPunCallbacks
{
    [Header("ī�޶� ������ (Inspector���� �Ҵ�)")]
    public GameObject playerCameraPrefab;

    public override void OnEnable()
    {
        base.OnEnable();//ī�޶� ������ ����
        DisableCameras();
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }


    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            Debug.Log("ī�޶� �ν��Ͻ�ȭ");
            InitializePlayerCamera();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("�뿡 ���� ī�޶� �ν��Ͻ�ȭ");
        InitializePlayerCamera();
    }

    private void InitializePlayerCamera()//ī�޶� �ν��Ͻ�ȭ�� ����
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("InitializePlayerCamera");
            return;
        }

        if (playerCameraPrefab == null)
        {
            Debug.LogError("ī�޶� ����");
            return;
        }


        Vector3 cameraSpawnPosition = GetCameraSpawnMasterClient();//������ġ
        GameObject playerCameraInstance = PhotonNetwork.Instantiate(playerCameraPrefab.name, cameraSpawnPosition, Quaternion.identity);//Ŭ���̾�Ʈ�� ī�޶� �ν��Ͻ�ȭ �� ������
        EnableOnlyMyCamera();//��ī�޶��Ѱ� �ٸ� ī�ް� ����
    }


    private void DisableCameras()//�����ִ� ī�޶� ����
    {
        Camera[] sceneCameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in sceneCameras)
        {
            if (cam.gameObject != this.gameObject && cam.GetComponent<PhotonView>() == null)
            {
                cam.enabled = false;
                AudioListener audioListener = cam.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = false;
                }
                Debug.Log($"�⺻ �� ī�޶� ��Ȱ��ȭ: {cam.gameObject.name}");
            }
        }
    }

    private void EnableOnlyMyCamera()//��ī�޶��Ѱ� �ٸ� ī�ް� ����
    {
        Camera[] allNetworkCameras = FindObjectsOfType<Camera>();//�� ī�޶� ã��
        foreach (Camera cam in allNetworkCameras)
        {
            PhotonView pv = cam.GetComponent<PhotonView>();
            if (pv != null)
            {
                if (pv.IsMine) //���� Ŭ���̾�Ʈ�� ī�޶� ������
                {
                    cam.enabled = true; // ī�޶�Ȱ��ȭ
                    AudioListener audioListener = cam.GetComponent<AudioListener>();
                    if (audioListener != null)
                    {
                        audioListener.enabled = true;
                    }
                    Debug.Log($"�ڽ��� ī�޶� Ȱ��ȭ(View ID: {pv.ViewID})");
                }
                else // �ٸ� Ŭ���̾�Ʈ�� ī�޶�
                {
                    cam.enabled = false; // ī�޶��Ȱ��ȭ
                    AudioListener audioListener = cam.GetComponent<AudioListener>();
                    if (audioListener != null)
                    {
                        audioListener.enabled = false;
                    }
                    Debug.Log($"�ٸ� �÷��̾��� ī�޶� ��Ȱ��ȭ (Name: {cam.gameObject.name}, View ID: {pv.ViewID})");
                }
            }
        }
    }

    
    private Vector3 GetCameraSpawnMasterClient()
    {
        if (PhotonNetwork.IsMasterClient) //������Ŭ���̾�Ʈ�� ���
        {
            return new Vector3(-10f, 5f, -10f); // ����
        }
        else //������
        {
            return new Vector3(10f, 5f, -10f); // ������
        }
    }
}