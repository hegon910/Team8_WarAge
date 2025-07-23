using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

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
    }

    
    public void ConnectToServer()
    {
        PhotonNetwork.NickName = "Player";
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
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장");

        
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            if (PhotonNetwork.IsMasterClient)
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