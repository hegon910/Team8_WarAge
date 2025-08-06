
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditPanel : MonoBehaviour
{
    [SerializeField] GameObject lobbyPanel;

    [SerializeField] TMP_InputField nameInput;
    [SerializeField] TMP_InputField passInput;
    [SerializeField] TMP_InputField passConfirmInput;

    [SerializeField] TMP_Text emailText;
    [SerializeField] TMP_Text userIdText;

    [SerializeField] Button nicknameConfirmButton;
    [SerializeField] Button passConfirmButton;
    [SerializeField] Button backButton;

    private void Awake()
    {
        nicknameConfirmButton.onClick.AddListener(ChangeNickname);
        backButton.onClick.AddListener(Back);
    }

    private void OnEnable()
    {
        var user = UserAuthService.Auth?.CurrentUser;
        if (user == null)
        {
            Debug.LogError("현재 로그인된 유저가 없습니다.");
            return;
        }
        emailText.text = user.Email;
        userIdText.text = user.UserId;
        nameInput.text = user.DisplayName;
    }

    private void ChangeNickname()
    {
        UserAuthService.Instance.SetNickname(nameInput.text, success =>
        {
            if (success)
                Debug.Log("닉네임 변경 성공");
        });
    }

    private void Back()
    {
        lobbyPanel.SetActive(true);
        gameObject.SetActive(false);
    }
}
