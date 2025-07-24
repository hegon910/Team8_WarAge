using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������ �������� ���¿� �ٽ� ������ �����ϴ� �̱��� �޴��� Ŭ����
/// </summary>
public class InGameManager : MonoBehaviour
{
    public static InGameManager Instance { get; private set; }

    [Header("�������")]

    public PHK.UnitPanelManager unitPanelManager;

    [Header("���� �⺻ ����")]
    [SerializeField] private int startingGold = 175; //���� ���۽� ���� ���
    [SerializeField] private int maxBaseHealth = 1000; //������ �ִ� ü��
    [Tooltip("�ô� ������ �ʿ��� ���� ����ġ")]
    [SerializeField] private int[] expForEvolve;

    //���� ���� ���� ����
    private int currentGold;
    private int currentEXP;
    private int currentBaseHealth;

    //����
    readonly Unit unit;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        //���� ���� �ʱ�ȭ
        currentGold = startingGold;
        currentEXP = 0;
        currentBaseHealth = maxBaseHealth;

        //�ʱ� ���� ���¸� UI�� �ݿ�
        InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
        InGameUIManager.Instance.UpdateBaseHpUI(currentBaseHealth, maxBaseHealth);

        //���� ���۽� �ô� ���� ��ư�� ��Ȱ��ȭ ���·� ����
        if(unitPanelManager != null && unitPanelManager.evolveButton !=null)
        {
            unitPanelManager.evolveButton.interactable = false;
        }
    }

    #region �ڿ� �� ü�� ���� �Լ�
    ///<summary>
    ///��� ȹ�� (����� ���� óġ ��)
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold += amount;
        InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
        Debug.Log($"{amount} ��� ȹ��. ���� ��� : {currentGold}");
    }
    ///<summary>
    ///��� �Ҹ�(���� �� �ͷ� ���� ��)
    /// </summary>
    public bool SpenGold(int amount)
    {
        if(currentGold>=amount)
        {
            currentGold -= amount;
            InGameUIManager.Instance.UpdateResourceUI(currentGold,currentEXP);
            Debug.Log($"{amount}��� �Ҹ�. ���� ��� : {currentGold}");
         
            return true;
        }
        else
        {
            Debug.Log("��尡 �����մϴ�.");
            //UIManager, InGameInfoText���� �˸� ���� �߰�
            InGameUIManager.Instance.inGameInfoText.text = $"Can't Spawn {unit.unitName} !! Not Enough Gold!";
            InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
            return false;
        }        
    }

    ///<summary>
    ///����ġ ȹ�� (�ַ� �� ���� óġ)
    /// </summary>
    /// 
    
    public void AddExp(int amount)
    {
        //������ �ô밡 �ƴ� ��쿡�� ����ġ ȹ���ϸ�, �ô� ������ üũ
        if(unitPanelManager.currentAge < PHK.UnitPanelManager.Age.Future) //������������ �ϸ� Age.Modern���� ����
        {
            currentEXP += amount;
            InGameUIManager.Instance.UpdateResourceUI(currentGold, currentEXP);
            CheckForAgeUp();
            Debug.Log($"{amount} ����ġ ȹ��.");
        }
    }
    ///<summary>
    ///���� ü�� ����(���� �޾��� ��)
    /// </summary>

    public void TakeBaseDamage(int damage)
    {
        currentBaseHealth -= damage;
        currentBaseHealth = Mathf.Max(currentBaseHealth, 0); //ü���� 0 �̸����� �������� �ʵ��� ����
        InGameUIManager.Instance.UpdateBaseHpUI(currentBaseHealth, maxBaseHealth);

        if (currentBaseHealth <= 0) GameOver();
    }
    #endregion

    #region �ô� ���� ����
    ///<summary>
    ///�ô� ���� �õ�(UnitPanel�� �ô� ���� ��ư Onclick �̺�Ʈ)
    /// </summary>
    public void AttemptEvolve()
    {
        int currentAgeIndex = (int)unitPanelManager.currentAge;
        //���� ������ ���� �ô�, �ʿ� ����ġ ������ Ȯ��
        if(currentAgeIndex < expForEvolve.Length && currentEXP >= expForEvolve[currentAgeIndex])
        {
            //UnitPanelManager�� �ô� ���� �Լ��� ȣ���Ͽ� UI�� ���� �ô� �гη� ��ü
            unitPanelManager.EvolveToNextAge();
            //�ô� ���� �� ��ư ��Ȱ��ȭ
            unitPanelManager.evolveButton.interactable = false;

            //Ȥ�� ���� �ô� ���� ���� �������� ��츦 ����� �ٽ� �� �� üũ
            CheckForAgeUp();
        }
    }

    ///<summary>
    ///����ġ�� ȹ���� ������ �ô� ������ �������� Ȯ�� �� ��ư Ȱ��ȭ�� ����
    /// </summary>
    private void CheckForAgeUp()
    { 
        int currentAgeIndex = (int )unitPanelManager.currentAge;

        //���� �ô�� ���� ��ư���� ��Ȱ��ȭ ������ �� üũ
        if(currentAgeIndex < expForEvolve.Length && !unitPanelManager.evolveButton.interactable)
        {
            //���� ����ġ�� �ʿ� ����ġ �̻��̸� ���� ��ư Ȱ��ȭ
            if(currentEXP >= expForEvolve[currentAgeIndex])
            {
                unitPanelManager.evolveButton.interactable=true;
                InGameUIManager.Instance.inGameInfoText.text = "You Can Evolve Age!";
                InGameUIManager.Instance.inGameInfoText.gameObject.SetActive(true);
            }
        }
    }
    #endregion


    ///<summary>
    ///���� ���� ó��
    /// </summary>
    private void GameOver()
    {
        Debug.Log("���� ���� ó�� �߰� �۾� �ʿ�");
        Time.timeScale = 0f;
    }
}
