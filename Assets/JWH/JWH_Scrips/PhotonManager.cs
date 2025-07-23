using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.Progress;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance;

    private string testloginScene = "JWH_LoginScene";
    private string testlobbyScene = "JWH_LobbyScene";
    private string testgameScene = "JWH_GameScene";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
        {
            Destroy(gameObject);
        }
    }


    public void ConnectToServer(string nickname)//닉네임받아서시작
    {
        PhotonNetwork.NickName = nickname;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비");
        SceneManager.LoadScene(testlobbyScene);
    }

   
    public void CreateOrJoinRoom()
    {
        string roomName = "TestRoom";
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장");


        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(testgameScene);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(testgameScene);
        }
    }


    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(testlobbyScene);
    }
}