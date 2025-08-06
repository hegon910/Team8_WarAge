
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour
{
    [SerializeField] GameObject loginPannel;
    [SerializeField] GameObject editPanel;

    [SerializeField] TMP_Text emailText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text userIdText;

    [SerializeField] Button logoutButton;
    [SerializeField] Button editProfileButton;
    [SerializeField] Button deleteUserButton;

    private void Awake()
    {
        logoutButton.onClick.AddListener(Logout);
        //editProfileButton.onClick.AddListener(EditProfile);
        //deleteUserButton.onClick.AddListener(DeleteUser);
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
        nameText.text = user.DisplayName;
        userIdText.text = user.UserId;
    }

    private void Logout()
    {
        UserAuthService.Instance.SignOut();
        loginPannel.SetActive(true);
        gameObject.SetActive(false);
    }

    //private void EditProfile()
    //{
    //    editPanel.SetActive(true);
    //    gameObject.SetActive(false);
    //}

    //private void DeleteUser()
    //{
    //    UserAuthService.Instance.DeleteAccount(success =>
    //    {
    //        if (success)
    //        {
    //            loginPannel.SetActive(true);
    //            gameObject.SetActive(false);
    //        }
    //    });
    //}
}
