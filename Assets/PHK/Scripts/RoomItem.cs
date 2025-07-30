using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class RoomItem : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    private RoomInfo roomInfo;

    // 방 정보를 설정하고 UI를 업데이트하는 함수
    public void SetRoomInfo(RoomInfo info)
    {
        roomInfo = info;
        roomNameText.text = info.Name;
        playerCountText.text = $"{info.PlayerCount} / {info.MaxPlayers}";
        // 방이 꽉 찼거나 비공개가 아니면 버튼 활성화
        joinButton.interactable = (info.PlayerCount < info.MaxPlayers);
    }

    // 참가 버튼 클릭 시 호출될 함수
    public void OnJoinButtonClicked()
    {
        PhotonNetwork.JoinRoom(roomInfo.Name);
    }
}