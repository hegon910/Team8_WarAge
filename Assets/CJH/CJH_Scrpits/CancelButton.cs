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
                UIManager.Instance.OnClickedLoginConfirm(); // �� SignUp���� Login���� ���� �Լ�
                break;
            default:
                Debug.LogWarning("Cancel context�� ���ǵ��� �ʾҽ��ϴ�.");
                break;
        }
    }
}