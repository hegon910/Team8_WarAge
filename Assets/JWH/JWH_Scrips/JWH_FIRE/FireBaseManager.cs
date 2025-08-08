
using System;
using System.Collections;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class UserAuthService : MonoBehaviour
{
    public static UserAuthService Instance { get; private set; }

    public static FirebaseApp App { get; private set; }
    public static FirebaseAuth Auth { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("Firebase 초기화 성공");
                App = FirebaseApp.DefaultInstance;
                Auth = FirebaseAuth.DefaultInstance;
            }
            else
            {
                Debug.LogError("Firebase 초기화 실패: " + task.Result);
            }
        });
    }

    public void SignUp(string email, string password, Action<bool> callback = null)
    {
        Auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("회원가입 실패: " + task.Exception);
                    callback?.Invoke(false);
                }
                else
                {
                    Debug.Log("회원가입 성공");
                    UserRank.Instance?.InitializeNewUser();//전적초기화
                    callback?.Invoke(true);
                }
            });
    }

    public void Login(string email, string password, Action<bool, FirebaseUser> callback = null)
    {
        FirebaseUser user = Auth.CurrentUser;
        Auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("로그인 실패: " + task.Exception);
                    callback?.Invoke(false, null);
                }
                else
                {
                    Debug.Log("로그인 성공");

                    if (user.IsEmailVerified)
                        UIManager.Instance.OnClickedLogin();
                    else
                        UIManager.Instance.OnClickedLoginFirst();

                    callback?.Invoke(true, task.Result.User);
                }
            });
    }

    public void SendVerificationEmail(Action<bool> callback = null)
    {
        if (Auth.CurrentUser == null)
        {
            Debug.LogError("이메일 인증 실패: 로그인된 유저가 없습니다.");
            callback?.Invoke(false);
            return;
        }

        Auth.CurrentUser.SendEmailVerificationAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("이메일 인증 실패: " + task.Exception);
                    callback?.Invoke(false);
                }
                else
                {
                    Debug.Log("이메일 인증 메일 전송 성공");
                    callback?.Invoke(true);
                }
            });
    }

    public IEnumerator WaitForEmailVerification(Action<bool> onVerified)
    {
        WaitForSeconds delay = new WaitForSeconds(2f);
        FirebaseUser user = Auth.CurrentUser;

        while (true)
        {
            yield return delay;
            yield return user.ReloadAsync();

            if (user.IsEmailVerified)
            {
                Debug.Log("이메일 인증 완료");
                onVerified?.Invoke(true);
                yield break;
            }
            else
            {
                Debug.Log("이메일 인증 대기중...");
            }
        }
    }

    public void SetNickname(string nickname, Action<bool> callback = null)
    {
        var profile = new UserProfile { DisplayName = nickname };
        Auth.CurrentUser.UpdateUserProfileAsync(profile)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("닉네임 설정 실패: " + task.Exception);
                    callback?.Invoke(false);
                }
                else
                {
                    Debug.Log("닉네임 설정 성공");
                    callback?.Invoke(true);
                    UIManager.Instance.OnClickedNicknameConfirm(nickname);
                }
            });
    }


    //public void ResetPassword(string email, Action<bool> callback = null)
    //{
    //    Auth.SendPasswordResetEmailAsync(email)
    //        .ContinueWithOnMainThread(task =>
    //        {
    //            if (task.IsCanceled || task.IsFaulted)
    //            {
    //                Debug.LogError("패스워드 재설정 실패: " + task.Exception);
    //                callback?.Invoke(false);
    //            }
    //            else
    //            {
    //                Debug.Log("패스워드 재설정 메일 전송 성공");
    //                callback?.Invoke(true);
    //            }
    //        });
    //}

    //public void DeleteAccount(Action<bool> callback = null)
    //{
    //    Auth.CurrentUser.DeleteAsync()
    //        .ContinueWithOnMainThread(task =>
    //        {
    //            if (task.IsCanceled || task.IsFaulted)
    //            {
    //                Debug.LogError("계정 삭제 실패: " + task.Exception);
    //                callback?.Invoke(false);
    //            }
    //            else
    //            {
    //                Debug.Log("계정 삭제 성공");
    //                callback?.Invoke(true);
    //            }
    //        });
    //}

    public void SignOut()
    {
        Auth.SignOut();
    }
}
