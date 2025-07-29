using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.EventSystems;
using Photon.Pun;

namespace PHK
{
    //���� ��ư�� �پ ���콺 ȣ�� �̺�Ʈ ó��
    public class UnitButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("���ֵ�����")]
        public GameObject unitPrefab;
        private Unit unitData; // ���� ��ũ���ͺ� ������Ʈ ������

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(SpawnUnit);
        }
        public void Init(GameObject prefab)
        {
            unitPrefab = prefab;
            if (unitPrefab != null)
            {
                unitData = unitPrefab.GetComponent<UnitController>().unitdata;
                gameObject.SetActive(true); // ���� �������� �����Ǹ� ��ư Ȱ��ȭ
            }
            else
            {
                gameObject.SetActive(false); // ���� �������� ������ ��ư ��Ȱ��ȭ
            }
        }

        //���콺�� ��ư ���� �ö��� �� ȣ��Ǵ� �̺�Ʈ
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (unitData != null)
            {
                string info = $"{unitData.unitName} Cost : {unitData.goldCost}";
                //�ΰ��� UI �Ŵ����� ���� ���� ���� ǥ��
                InGameUIManager.Instance.ShowInfoText(info);
            }
        }
        //���콺 Ŀ���� ��ư ������ ���� ���� �� ȣ��
        public void OnPointerExit(PointerEventData eventData)
        {
            //�ΰ��� UI �Ŵ����� ���� ���� �ؽ�Ʈ ����
            InGameUIManager.Instance.HideInfoText();
        }

        //SpawnManager.cs ��ũ��Ʈ �޾��� �� Ŭ�� �� ���� ������û�� SpawnManager�� ������ onclick �̺�Ʈ �Լ�
        public void SpawnUnit()
        {
            if (unitPrefab == null) return;

            //���� Ŭ���̾�Ʈ�� ������ Ŭ���̾�Ʈ���� Ȯ��
            string ownerTag = PhotonNetwork.IsMasterClient ? "P1" : "P2";
            if (InGameManager.Instance.isDebugMode)
            {
                // isDebugHost ���� ���� P1 �Ǵ� P2�� �±׸� ����
                ownerTag = InGameManager.Instance.isDebugHost ? "P1" : "P2";
            }
            else // ���� ��Ʈ��ũ ȯ���� ���
            {
                ownerTag = PhotonNetwork.IsMasterClient ? "P1" : "P2";
            }
            UnitSpawnManager.Instance.RequestUnitProduction(unitPrefab, ownerTag);
        }
    }
}
