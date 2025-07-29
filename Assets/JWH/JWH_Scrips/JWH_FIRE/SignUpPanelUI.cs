
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpPanel : MonoBehaviour
{
    [SerializeField] GameObject loginPanel;

    [SerializeField] TMP_InputField idInput;
    [SerializeField] TMP_InputField passInput;
    [SerializeField] TMP_InputField passConfirmInput;

    [SerializeField] Button signUpButton;
    [SerializeField] Button cancelButton;

    private void Awake()
    {
        signUpButton.onClick.AddListener(SignUp);
        cancelButton.onClick.AddListener(Cancel);
    }

    private void SignUp()
    {
        if (passInput.text != passConfirmInput.text)
        {
            Debug.LogError("패스워드가 일치하지 않습니다");
            return;
        }

        UserAuthService.Instance.SignUp(idInput.text, passInput.text, success =>
        {
            if (success)
            {
                loginPanel.SetActive(true);
                gameObject.SetActive(false);
            }
        });
    }

    private void Cancel()
    {
        loginPanel.SetActive(true);
        gameObject.SetActive(false);
    }
}
