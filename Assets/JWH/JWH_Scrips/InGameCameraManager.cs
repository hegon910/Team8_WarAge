using Photon.Pun;
using UnityEngine;


public class InGameCameraManager : MonoBehaviourPunCallbacks
{
    [Header("카메라 프리팹 (Inspector에서 할당")]
    public GameObject playerCameraPrefab;

    public override void OnEnable()
    {
        base.OnEnable();//카메라 강제로 끄기
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
            Debug.Log("카메라 인스턴스화");
            InitializePlayerCamera();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("룸에 조인 카메라 인스턴스화");
        InitializePlayerCamera();
    }

    private void InitializePlayerCamera()//카메라 인스턴스화를 시작
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("InitializePlayerCamera");
            return;
        }

        if (playerCameraPrefab == null)
        {
            Debug.LogError("카메라 없음");
            return;
        }


        Vector3 cameraSpawnPosition = GetCameraSpawnMasterClient();//스폰위치
        GameObject playerCameraInstance = PhotonNetwork.Instantiate(playerCameraPrefab.name, cameraSpawnPosition, Quaternion.identity);//클라이언트의 카메라 인스턴스화 및 소유권
        EnableOnlyMyCamera();//내카메라켜고 다른 카메가 끄기
    }


    private void DisableCameras()//원래있는 카메라 끄기
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
                Debug.Log($"기본 씬 카메라 비활성화: {cam.gameObject.name}");
            }
        }
    }

    private void EnableOnlyMyCamera()//내카메라켜고 다른 카메가 끄기
    {
        Camera[] allNetworkCameras = FindObjectsOfType<Camera>();//내 카메라 찾기
        bool hasMyAudioListener = false;


        foreach (Camera cam in allNetworkCameras)
        {
            PhotonView pv = cam.GetComponent<PhotonView>();
            AudioListener audioListener = cam.GetComponent<AudioListener>();
            if (pv != null && pv.IsMine)//카메라 소유권
            {
                cam.enabled = true;
                if (audioListener != null)
                {
                    audioListener.enabled = true;
                    hasMyAudioListener = true;
                }
            }
            else
            {
                cam.enabled = false;
                if (audioListener != null)
                {
                    audioListener.enabled = false;
                }
            }
        }

        if (!hasMyAudioListener)
        {
            Camera myCam = Camera.main;
            if (myCam != null && myCam.GetComponent<AudioListener>() == null)
            {
                myCam.gameObject.AddComponent<AudioListener>();
                Debug.LogWarning("AudioListener자동추가");
            }
        }
    }



    private Vector3 GetCameraSpawnMasterClient()
    {
        if (PhotonNetwork.IsMasterClient) //마스터클라이언트의 경우
        {
            return new Vector3(-10f, 5f, -10f); // 왼쪽
        }
        else //나머지
        {
            return new Vector3(10f, 5f, -10f); // 오른쪽
        }
    }
}