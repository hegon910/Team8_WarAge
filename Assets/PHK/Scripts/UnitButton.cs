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
        public Unit unitData; // ���� ��ũ���ͺ� ������Ʈ ������

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
            
        }
    }
}
