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

    // �� ������ �����ϰ� UI�� ������Ʈ�ϴ� �Լ�
    public void SetRoomInfo(RoomInfo info)
    {
        roomInfo = info;
        roomNameText.text = info.Name;
        playerCountText.text = $"{info.PlayerCount} / {info.MaxPlayers}";
        // ���� �� á�ų� ������� �ƴϸ� ��ư Ȱ��ȭ
        joinButton.interactable = (info.PlayerCount < info.MaxPlayers);
    }

    // ���� ��ư Ŭ�� �� ȣ��� �Լ�
    public void OnJoinButtonClicked()
    {
        PhotonNetwork.JoinRoom(roomInfo.Name);
    }
}