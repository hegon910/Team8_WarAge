using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;


    public class PhotonManager : MonoBehaviourPunCallbacks
    {
        public static PhotonManager Instance;

        [SerializeField] private string loginSceneName = "JWH_LoginScene";
        [SerializeField] private string lobbySceneName = "JWH_LobbyScene";
        [SerializeField] private string roomSceneName = "JWH_RoomScene";
        [SerializeField] private string gameSceneName = "JWH_GameScene";

        void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void ConnectToServer(string nickname)
        {
            if (PhotonNetwork.IsConnected)
            {
                OnConnectedToMaster();
                return;
            }
            PhotonNetwork.NickName = nickname;
            PhotonNetwork.ConnectUsingSettings();
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        public void CreateOrJoinRoom()// 방 만든 사람이 마스터
        {
            string roomName = "TestRoom";
            RoomOptions options = new RoomOptions { MaxPlayers = 2 };
            PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            SceneManager.LoadScene(roomSceneName);
        }

        public void SetLocalPlayerReady(bool ready)
        {
            Hashtable props = new Hashtable { { "Ready", ready } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public bool AreAllPlayersReady()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (!p.CustomProperties.TryGetValue("Ready", out object value) || !(bool)value)
                {
                    return false;
                }
            }
            return true;
        }

        public void StartGame()
        {
            if (PhotonNetwork.IsMasterClient && AreAllPlayersReady())
            {
                PhotonNetwork.LoadLevel(gameSceneName);
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
        }

        public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
        {
        }

        public override void OnLeftRoom()
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        public Player[] GetCurrentRoomPlayers()
        {
            return PhotonNetwork.PlayerList;
        }
    }
