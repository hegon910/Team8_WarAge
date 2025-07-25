using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CancelButton : MonoBehaviour
{
    public enum CancelContext
    {
        Graphic,
        Sound,
        SignUp
    }

    public CancelContext context;

    public void OnCancel()
    {
        switch (context)
        {
            case CancelContext.Graphic:
            case CancelContext.Sound:
                UIManager.Instance.HideOptionPanel();
                break;
            case CancelContext.SignUp:
                UIManager.Instance.OnClickedLoginConfirm(); // ← SignUp에서 Login으로 가는 함수
                break;
            default:
                Debug.LogWarning("Cancel context가 정의되지 않았습니다.");
                break;
        }
    }
}