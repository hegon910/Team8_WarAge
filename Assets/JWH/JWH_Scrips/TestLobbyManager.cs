using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestLobbyManager : MonoBehaviour
{
    [SerializeField] private Button joinButton;
   

    private void Start()
    {
        joinButton.onClick.AddListener(() =>
        {
           
            PhotonManager.Instance.CreateOrJoinRoom();
        });
    }
}
