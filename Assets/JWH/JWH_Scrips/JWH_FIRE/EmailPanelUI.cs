
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EmailPanel : MonoBehaviour
{
    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject nicknamePanel;

    [SerializeField] Button backButton;

    Coroutine emailVerificationRoutine;

    private void Awake()
    {
        backButton.onClick.AddListener(Back);
    }

    private void OnEnable()
    {
        UserAuthService.Instance.SendVerificationEmail(success =>
        {
            if (success)
            {
                emailVerificationRoutine = StartCoroutine(UserAuthService.Instance.WaitForEmailVerification(OnEmailVerified));
            }
        });
    }

    private void Back()
    {
        UserAuthService.Instance.SignOut();
        loginPanel.SetActive(true);
        gameObject.SetActive(false);
    }

    private void OnEmailVerified(bool verified)
    {
        if (verified)
        {
            nicknamePanel.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
