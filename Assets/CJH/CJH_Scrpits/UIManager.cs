using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using Photon.Realtime;
using Photon.Pun;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Option Panels")]
    public GameObject mainPanel;
    public GameObject loginPanel;
    public GameObject nicknamePanel;
    public GameObject signUpPanel;
    public GameObject emailPanel;
    public GameObject lobbyPanel;
    public GameObject roomPanel;

    //추가
    [Header("Lobby UI")]
    public GameObject roomItemPrefab; // 1단계에서 만든 RoomItem 프리팹을 연결
    public Transform roomListContent; // 방 목록이 생성될 부모 Transform (ScrollView의 Content)
    private List<RoomItem> roomItemsList = new List<RoomItem>();


    private void Awake()
    {
        // 싱글턴 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 게임 오브젝트를 파괴하지 않고 유지
        }

        else if (Instance != this)
        {
            Destroy(gameObject); // 중복 방지
        }

    }

    ///<summary>
    ///메인 화면 - 옵션 관련 함수
    ///<summary>

    #region 함수

    public void OnClickedStart()
    {
        mainPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    #endregion

    ///<summary>
    ///로비 - 방 관련 함수
    ///<summary>

    #region 함수
    public void OnClickedLoginFirst()
    {
        SoundManager.Instance.PlayEvolveSound();
        emailPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedLogin()
    {

        SoundManager.Instance.PlayEvolveSound();
        lobbyPanel.SetActive(true);
        loginPanel.SetActive(false);
    }
     
    public void OnClickedCancelBackMain()
    {

        SoundManager.Instance.PlayUIClick();
        mainPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedEmailBack()
    {

        SoundManager.Instance.PlayUIClick();
        signUpPanel.SetActive(true);
        emailPanel.SetActive(false);
    }

    public void OnClickedNicknameConfirm(string nickname)
    {
        SoundManager.Instance.PlayEvolveSound();
        PhotonManager.Instance.ConnectToServer(nickname);
    }

    public void OnClickedNicknameBack()
    {

        SoundManager.Instance.PlayUIClick();
        loginPanel.SetActive(true);
        nicknamePanel.SetActive(false);
    }

    public void Connect()
    {
        SoundManager.Instance.PlayUIClick();
        lobbyPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedSignup()
    {

        SoundManager.Instance.PlayEvolveSound();
        signUpPanel.SetActive(true);
        loginPanel.SetActive(false);
    }

    public void OnClickedSignupConfrim()
    {

        SoundManager.Instance.PlayEvolveSound();
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }

    public void OnClickedSignupCancel()
    {

        SoundManager.Instance.PlayEvolveSound();
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }
 
    public void OnClickedCreateRoom()
    {
        SoundManager.Instance.PlayEvolveSound();
        PhotonManager.Instance.CreateOrJoinLobby();
        PhotonManager.Instance.OnJoinedRoom();
        Debug.Log("클릭");
        
    }

    public void CreateRoom()
    {

        SoundManager.Instance.PlayEvolveSound();
        roomPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }
    // 새로 추가된 로비 패널 활성화 메서드 (OnLeftRoom 콜백에서 호출)
    public void ShowLobbyPanel()
    {
        HideAllPanels();
        lobbyPanel.SetActive(true);
    }
    //추가
    public void UpdateRoomList(List<RoomInfo> roomList)
    {
        // 1. 기존에 생성된 모든 방 UI 아이템 삭제
        foreach (RoomItem item in roomItemsList)
        {
            Destroy(item.gameObject);
        }
        roomItemsList.Clear();

        // 2. 서버에서 받아온 방 목록을 기반으로 새로운 UI 아이템 생성
        foreach (RoomInfo info in roomList)
        {
            // 닫혔거나, 비공개거나, 목록에서 제거된 방은 표시하지 않음
            if (info.RemovedFromList || !info.IsVisible || info.IsOpen == false)
            {
                continue;
            }

            GameObject newRoomItemObj = Instantiate(roomItemPrefab, roomListContent);
            RoomItem newRoomItem = newRoomItemObj.GetComponent<RoomItem>();
            newRoomItem.SetRoomInfo(info);

            roomItemsList.Add(newRoomItem);
        }
    }
    public void ShowRoomPanel()
    {
        roomPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        Debug.Log("UI: 방 패널 활성화, 로비 패널 비활성화");

    }
    //---------------
    public void OnClickedLobbyCancel()
    {
        SoundManager.Instance.PlayUIClick();
        loginPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }    

    public void OnClickedStartInGame()
    {
        SoundManager.Instance.PlayEvolveSound();
        //인게임 연결
        //loginPanel.SetActive(true);
        roomPanel.SetActive(false);
    }

    public void OnClickedReady()
    {
    }

    public void OnClickedLeave()
    {
        SoundManager.Instance.PlayUIClick();
        Debug.Log("방 떠남");
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
        PhotonManager.Instance.OnLeftRoom();
        PhotonNetwork.LeaveRoom();
    }
    public void HideAllPanels()
    {
        mainPanel.SetActive(false);
        loginPanel.SetActive(false);
        nicknamePanel.SetActive(false);
        signUpPanel.SetActive(false);
        emailPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
        // 게임 내 UI는 이 스크립트에서 관리되지 않으므로, 이외의 패널은 별도로 처리
    }




    #endregion







}