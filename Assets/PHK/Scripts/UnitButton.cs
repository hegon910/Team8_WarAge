using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PHK
{
    //���� ��ư�� �پ ���콺 ȣ�� �̺�Ʈ ó��
    public class UnitButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("���ֵ�����")]
        public GameObject unitPrefab;

        [Header("������ �±�")]
        //public string OwnerTag;
        private bool isHost = true;
        
        
        private Unit unitData; // ���� ��ũ���ͺ� ������Ʈ ������


        private void Awake()
        {
            //�������� �Ҵ� �Ǿ� ������ UnitContorller���� �����͸� ������
            if(unitPrefab != null)
            {
                unitData = unitPrefab.GetComponent<UnitController>().unitdata;
            }
        }

        //���콺�� ��ư ���� �ö��� �� ȣ��Ǵ� �̺�Ʈ
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (unitData != null)
            {
                //�ΰ��� UI �Ŵ����� ���� ���� ���� ǥ��
                InGameUIManager.Instance.ShowUnitGoldCost(unitData);
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
            // �� ��ư�� ȣ��Ʈ(P1)�� ������ ���� ������ �����մϴ�.
            if (isHost)
            {
                if (unitPrefab != null)
                {
                    // �ڵ����� "P1" �±׸� �ٿ� ������ ��û�մϴ�.
                    InGameManager.Instance.RequestUnitProduction(unitPrefab, "P1");
                }
            }
            else
            {
                // Ŭ���̾�Ʈ(������)�� ���, ���⿡�� ȣ��Ʈ���� ���� ������ ��û�ϴ�
                // ��Ʈ��ũ ���(RPC)�� ������ �մϴ�. ������ �ƹ��͵� ���� �ʽ��ϴ�.
                Debug.Log("Ŭ���̾�Ʈ������ ���� ���� RPC�� ȣ���ؾ� �մϴ�.");
            }
        }
    }
}
