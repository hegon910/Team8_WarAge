using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;


namespace PHK
{
    //�ΰ����� �������� UI�� �����ϴ� �Ŵ��� ��ũ��Ʈ
    //�ô� ����, �ڿ�ǥ��, ���� UI�������� ���
    public class UnitPanelManager : MonoBehaviour
    {
        //�ô� (Age)�� ��Ȯ�ϰ� �����ϱ� ���� ������ (enum)���� ����
        public enum Age
        {
            Ancient,
            Middle,
            Modern,
            Future//Ȥ�ó� �̷� �ô밡 �߰��� ��츦 ����Ͽ� Future �߰�

        }

        // ���� Ȱ��ȭ�� �г��� ������ �����ϱ� ���� ������
        private enum ActivePanelType
        {
            Selection,
            Units,
            Turrets,
        }

        // ���� ������ �ô븦 �����ϴ� ����, ������ '���'
        [Header("�������")]
        public Age currentAge = Age.Ancient;
        private ActivePanelType activePanel = ActivePanelType.Selection;

        [Header("�����г�")]
        public GameObject selectPanel; //"Units", "Turrets" ��ư�� �ִ� �ʱ� ���� �г�


        //����Ƽ �����Ϳ��� �� �ô뿡 �´� ���� �г��� ����
        [Header("���� �г�")]
        public GameObject ancientUnitPanel;
        public GameObject middleUnitPanel;
        public GameObject modernUnitPanel;
        public GameObject futureUnitPanel; //���� ���ٸ�

        [Header("�ô뺰 �ͷ� �г�")]
        public GameObject ancientTurretPanel;
        public GameObject middleAgeTurretPanel;
        public GameObject modernTurretPanel;
        public GameObject futureTurretPanel;

        //�ô� ���� ��ư 
        [Header("�ô� ���� ��ư")]
        public Button evolveButton;
        //������ �ô��� ��� ���� ��ư�� ��Ȱ��ȭ�ϰ� �ش� �������� ��ü
        public Button lastAgeUnitButton;

        void Start()
        {
            //���� ���� ��, ���� ������ ��� ui �гη� ����
            UpdateUnitPanelVisibility();
            activePanel = ActivePanelType.Selection;
        }
        void Update()
        {

        }

        //���� �ô뿡 �´� �г� �Լ�
        private void UpdateUnitPanelVisibility()
        {
            //��� �г� ��Ȱ��ȭ
            selectPanel.SetActive(false);
            ancientUnitPanel.SetActive(false);
            middleUnitPanel.SetActive(false);
            modernUnitPanel.SetActive(false);
            futureUnitPanel.SetActive(false);
            //�ͷ� �г� ��Ȱ��ȭ
            if (ancientTurretPanel != null) ancientTurretPanel.SetActive(false);
            if (middleAgeTurretPanel != null) middleAgeTurretPanel.SetActive(false);
            if (modernTurretPanel != null) modernTurretPanel.SetActive(false);
            if (futureTurretPanel != null) futureTurretPanel.SetActive(false);

            //���� ���¿� �ش��ϴ� �г� Ȱ��ȭ
            switch (activePanel)
            {
                case ActivePanelType.Selection:
                    selectPanel.SetActive(true);
                    break;

                case ActivePanelType.Units:
                    // ���� �ô뿡 �´� ���� �г��� �մϴ�.
                    switch (currentAge)
                    {
                        case Age.Ancient: ancientUnitPanel.SetActive(true); break;
                        case Age.Middle: middleUnitPanel.SetActive(true); break;
                        case Age.Modern: modernUnitPanel.SetActive(true); break;
                        case Age.Future: futureUnitPanel.SetActive(true); break;
                    }
                    break;

                case ActivePanelType.Turrets:
                    // ���� �ô뿡 �´� �ͷ� �г��� �մϴ�.
                    switch (currentAge)
                    {
                        case Age.Ancient: if (ancientTurretPanel != null) ancientTurretPanel.SetActive(true); break;
                        case Age.Middle: if (middleAgeTurretPanel != null) middleAgeTurretPanel.SetActive(true); break;
                        case Age.Modern: if (modernTurretPanel != null) modernTurretPanel.SetActive(true); break;
                        case Age.Future: if (futureTurretPanel != null)
                            {
                                futureTurretPanel.SetActive(true);
                                evolveButton.interactable = false; //�̷� �ô�� ���� ��ư ��Ȱ��ȭ
                                evolveButton.gameObject.SetActive(false); //��ư ��ü�� ��Ȱ��ȭ
                            } break;
                    }
                    break;
            }

            Debug.Log("���� �ô�: " + currentAge.ToString() + " �г� Ȱ��ȭ��");
        }

        //Unit ��ư ������ ȣ��
        public void ShowUnitPnale()
        {
            //���� �г� Ȱ��ȭ
            activePanel = ActivePanelType.Units;
            UpdateUnitPanelVisibility();
        }
        //Turret ��ư ������ ȣ��
        public void ShowTurretPanel()
        {
            //�ͷ� �г� Ȱ��ȭ
            activePanel = ActivePanelType.Turrets;
            UpdateUnitPanelVisibility();
        }
        //����/�ͷ� �г��� ���ư��� ��ư ������ selection �гη� ���ư�
        public void ShowSelectionPanel()
        {
            //�ʱ� ���� �г� Ȱ��ȭ
            activePanel = ActivePanelType.Selection;
            UpdateUnitPanelVisibility();
        }

        // �ô븦 ���� �ܰ�� �ѱ�� ���, Ư�� �ô�� ���� �����ϴ� �Լ�
        public void SetAge(Age newAge)
        {
            // ������ �ô뺸�� ���� ������ �������� �ʵ��� ����
            if (newAge <= Age.Future) // Age enum�� ������ ��
            {
                currentAge = newAge;
                UpdateUnitPanelVisibility(); // �ش� ������ �´� �г��� ������
                Debug.Log($"�ô밡 {newAge}�� ���� �����Ǿ����ϴ�.");
            }
        }

        // �ô� ���� ��ư�� ������ ���� �Լ�, ���� �ô�� ������Ű�� ����
        public void EvolveToNextAge()
        {
            Debug.Log($"EvolveToNextAge() �����. ���� UnitPanelManager ID: <color=lime>{GetInstanceID()}</color>");
            //������ �ô밡 �ƴ� ��쿡�� �ô븦 ����
            if (currentAge < Age.Future)
            {
                currentAge++;
                UpdateUnitPanelVisibility();
            }
            else
            {
                //������ �ô��� ��� ���̻� ���� �� �� ����.
                //���� ��ư ��Ȱ��ȭ,
                evolveButton.interactable = false;
                evolveButton.gameObject.SetActive(false);
                //��� ������ �ô� ���� ��ư Ȱ��ȭ
                lastAgeUnitButton.gameObject.SetActive(true);

            }
        }

    }

}