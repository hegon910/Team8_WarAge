using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameField;
    [SerializeField] private Button connectButton;
    

    private void Start()
    {
        connectButton.onClick.AddListener(OnConnectClicked);
    }

    private void OnConnectClicked()
    {
        string nickname = nicknameField.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            
            return;
        }

       
        PhotonManager.Instance.ConnectToServer(nickname);
    }
}
