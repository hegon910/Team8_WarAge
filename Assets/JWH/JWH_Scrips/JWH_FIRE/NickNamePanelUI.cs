
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NickNamePanel : MonoBehaviour
{
    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject lobbyPanel;

    [SerializeField] TMP_InputField nicknameInput;

    [SerializeField] Button confirmButton;
    [SerializeField] Button backButton;

    private void Awake()
    {
        confirmButton.onClick.AddListener(Confirm);
        backButton.onClick.AddListener(Back);
    }

    private void Confirm()
    {
        UserAuthService.Instance.SetNickname(nicknameInput.text, success =>
        {
            if (success)
            {
                lobbyPanel.SetActive(true);
                gameObject.SetActive(false);
            }
        });
    }

    private void Back()
    {
        UserAuthService.Instance.SignOut();
        loginPanel.SetActive(true);
        gameObject.SetActive(false);
    }
}
