using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.Progress;

//public class PhotonManager : MonoBehaviourPunCallbacks
//{
//    public static PhotonManager Instance;

//    private string testloginScene = "JWH_LoginScene";
//    private string testlobbyScene = "JWH_LobbyScene";
//    private string testgameScene = "JWH_GameScene";
//    private string testroomSceene = "JWH_RoomScene";

//    void Awake()
//    {
//        PhotonNetwork.AutomaticallySyncScene = true;
//        if (Instance == null)
//        {
//            Instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
       
//    }


//    public void ConnectToServer(string nickname)//�г��ӹ޾Ƽ�����
//    {
//        Debug.Log("�г��� �Էµ�: " + nickname);
//        if (PhotonNetwork.IsConnected)
//        {
//            Debug.Log("�̹� �����");
//            return;
//        }
//        PhotonNetwork.NickName = nickname;
//        PhotonNetwork.ConnectUsingSettings();
//    }

//    public override void OnConnectedToMaster()
//    {
//        PhotonNetwork.JoinLobby();
//    }

//    public override void OnJoinedLobby()
//    {
//        Debug.Log("�κ�");
//        SceneManager.LoadScene(testlobbyScene);
//    }

   
//    public void CreateOrJoinRoom()
//    {
//        string roomName = "TestRoom";
//        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
//        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
//    }

//    public override void OnJoinedRoom()
//    {
//        Debug.Log("�� ����");
//        SceneManager.LoadScene("JWH_RoomScene");
//    }

//    public override void OnPlayerEnteredRoom(Player newPlayer)
//    {
//        //if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
//        //{
//        //    PhotonNetwork.LoadLevel(testgameScene);
//        //}
//        //�ڵ������ؼ� ������
//    }


//    public override void OnLeftRoom()
//    {
//        SceneManager.LoadScene(testlobbyScene);
//    }
//}