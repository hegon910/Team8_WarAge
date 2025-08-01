using UnityEngine;
using UnityEngine.EventSystems;
using KYG;
// ���ӽ����̽��� PHK�� �����Ͽ� �ٸ� ��ũ��Ʈ�� �����մϴ�.
namespace PHK
{
    /// <summary>
    /// �ͷ� ���� ��ư�� �����Ǿ� ���콺 ȣ�� �� Ŭ�� �̺�Ʈ�� ó���մϴ�.
    /// UnitButton.cs�� ���� ������ �����Դϴ�.
    /// </summary>
    public class TurretButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("�ͷ� ������")]
        [Tooltip("�� ��ư�� Ŭ������ �� ������ �ͷ��� �������Դϴ�.")]
        public GameObject turretPrefab;
        public TurretData turretData;
        // private TurretData turretData; // ���߿� �ͷ� ����(�̸�, ��� ��)�� ���� ������ Ŭ����

        private void Awake()
        {
            // ���߿� �ͷ� �����տ� TurretController ���� ��ũ��Ʈ�� �����
            // �װ����� �̸��̳� ��� ���� �����͸� ������ �� �ֽ��ϴ�.
            // if (turretPrefab != null)
            // {
            //     turretData = turretPrefab.GetComponent<TurretController>().turretData;
            // }
        }

        // ���콺�� ��ư ���� �ö���� ��
        public void OnPointerEnter(PointerEventData eventData)
        {
            // �ͷ� ������ UI�� ǥ���մϴ�. (��� ��)
            // if (turretData != null)
            // {
            //     InGameUIManager.Instance.ShowInfoText($"{turretData.name} (Cost: {turretData.cost})");
            // }
            // �ӽ÷� ������ �̸��� ǥ��
            if (turretPrefab != null)
            {
                InGameUIManager.Instance.inGameInfoText.text=$"{turretPrefab.name}";
            }
        }

        // ���콺�� ��ư���� ����� ��
        public void OnPointerExit(PointerEventData eventData)
        {
            InGameUIManager.Instance.HideInfoText();
        }

        /// <summary>
        /// [OnClick �̺�Ʈ��] �ͷ� ������ InGameManager�� ��û�մϴ�.
        /// </summary>
        public void OnClick_RequestTurret()
        {
            if (turretPrefab == null)
            {
                Debug.LogError("TurretButton�� �ͷ� �������� �Ҵ���� �ʾҽ��ϴ�!");
                return;
            }
            // 설치 모드로 진입
            InGameUIManager.Instance.EnterTurretPlaceMode(turretData);
        }
    }
}