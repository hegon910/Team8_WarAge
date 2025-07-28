
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
        resetPassButton.onClick.AddListener(ResetPass);
    }

    private void SignUp()
    {
        signUpPanel.SetActive(true);
        gameObject.SetActive(false);
    }

    private void Login()
    {
        UserAuthService.Instance.Login(idInput.text, passInput.text, (success, user) =>
        {
            if (!success) return;

            if (user.IsEmailVerified)
            {
                if (string.IsNullOrEmpty(user.DisplayName))
                    nicknamePanel.SetActive(true);
                else
                    lobbyPanel.SetActive(true);
            }
            else
            {
                emailPanel.SetActive(true);
            }

            gameObject.SetActive(false);
        });
    }

    private void ResetPass()
    {
        UserAuthService.Instance.ResetPassword(idInput.text, success =>
        {
            if (success)
                Debug.Log("비밀번호 재설정 성공");
        });
    }
}
