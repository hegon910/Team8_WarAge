using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using KYG;

namespace PHK
{
    /// <summary>
    /// 터렛 생성 버튼. AgeData에 포함된 TurretData를 받아 자신을 초기화합니다.
    /// 클릭 시 해당 터렛의 설치 모드로 진입을 요청합니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TurretButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TurretData turretData;

        private Button button;
        private Image iconImage;

        private void Awake()
        {
            button = GetComponent<Button>();
            iconImage = GetComponent<Image>();

            button.onClick.AddListener(OnClick_RequestTurret);
        }

        /// <summary>
        /// 외부(UI 관리 스크립트)에서 TurretData를 받아 버튼을 초기화하는 핵심 함수입니다.
        /// </summary>
        public void Init(TurretData data)
        {
            this.turretData = data;

            if (this.turretData != null)
            {
                if (iconImage != null && this.turretData.icon != null)
                {
                    iconImage.sprite = this.turretData.icon;
                }
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // 마우스가 버튼 위에 올라갔을 때 호출
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (turretData != null)
            {
                string info = $"{turretData.turretName}\nCost: {turretData.cost}";
                InGameUIManager.Instance.ShowInfoText(info);
            }
        }

        // 마우스가 버튼에서 벗어났을 때 호출
        public void OnPointerExit(PointerEventData eventData)
        {
            InGameUIManager.Instance.HideInfoText();
        }

        /// <summary>
        /// [OnClick 이벤트] 터렛 설치 모드로 진입하도록 InGameUIManager에 요청합니다.
        /// </summary>
        private void OnClick_RequestTurret()
        {
            if (turretData == null)
            {
                Debug.LogError("TurretButton에 TurretData가 할당되지 않았습니다!");
                return;
            }

            // [수정] 임의로 추가했던 골드 체크 로직을 제거했습니다.
            // 이제 버튼은 원래 로직대로 설치 모드 진입만 요청합니다.
            InGameUIManager.Instance.EnterTurretPlaceMode(turretData);

            // 버튼 중복 클릭 방지
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}