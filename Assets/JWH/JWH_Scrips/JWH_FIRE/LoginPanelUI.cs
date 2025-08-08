
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : MonoBehaviour
{
    [SerializeField] GameObject signUpPanel;
    [SerializeField] GameObject lobbyPanel;
    [SerializeField] GameObject emailPanel;
    [SerializeField] GameObject nicknamePanel;

    [SerializeField] TMP_InputField idInput;
    [SerializeField] TMP_InputField passInput;

    [SerializeField] Button signUpButton;
    [SerializeField] Button loginButton;
    [SerializeField] Button resetPassButton;

    private void Awake()
    {
        signUpButton.onClick.AddListener(SignUp);
        loginButton.onClick.AddListener(Login);
        //resetPassButton.onClick.AddListener(ResetPass);
    }

    private void SignUp()
    {
        UIManager.Instance.OnClickedSignup();
    }

    private void Login()
    {
        //Login(idInput.text);
        string cleanedId = idInput.text.Replace(" ", ""); // 스페이스 제거
        Login(cleanedId);
    }

    private void Login(string nickname)
    {
        UserAuthService.Instance.Login(nickname, passInput.text, (success, user) =>
        {
            if (!success) return;

            if (user.IsEmailVerified)
            {
                if (string.IsNullOrEmpty(user.DisplayName))
                {
                    nicknamePanel.SetActive(true);
                }
                else
                {
                    UIManager.Instance.OnClickedNicknameConfirm(nickname);
                }
            }
            else
            {
                emailPanel.SetActive(true);
            }
        });
    }
}
