using UnityEngine;
using UnityEngine.EventSystems;

// 네임스페이스를 PHK로 지정하여 다른 스크립트와 통일합니다.
namespace PHK
{
    /// <summary>
    /// 터렛 구매 버튼에 부착되어 마우스 호버 및 클릭 이벤트를 처리합니다.
    /// UnitButton.cs와 거의 동일한 구조입니다.
    /// </summary>
    public class TurretButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("터렛 프리팹")]
        [Tooltip("이 버튼을 클릭했을 때 생성될 터렛의 프리팹입니다.")]
        public GameObject turretPrefab;

        // private TurretData turretData; // 나중에 터렛 정보(이름, 비용 등)를 담을 데이터 클래스

        private void Awake()
        {
            // 나중에 터렛 프리팹에 TurretController 같은 스크립트가 생기면
            // 그곳에서 이름이나 비용 같은 데이터를 가져올 수 있습니다.
            // if (turretPrefab != null)
            // {
            //     turretData = turretPrefab.GetComponent<TurretController>().turretData;
            // }
        }

        // 마우스가 버튼 위에 올라왔을 때
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 터렛 정보를 UI에 표시합니다. (비용 등)
            // if (turretData != null)
            // {
            //     InGameUIManager.Instance.ShowInfoText($"{turretData.name} (Cost: {turretData.cost})");
            // }
            // 임시로 프리팹 이름만 표시
            if (turretPrefab != null)
            {
                InGameUIManager.Instance.inGameInfoText.text=$"{turretPrefab.name}";
            }
        }

        // 마우스가 버튼에서 벗어났을 때
        public void OnPointerExit(PointerEventData eventData)
        {
            InGameUIManager.Instance.HideInfoText();
        }

        /// <summary>
        /// [OnClick 이벤트용] 터렛 생성을 InGameManager에 요청합니다.
        /// </summary>
        public void OnClick_RequestTurret()
        {
            if (turretPrefab == null)
            {
                Debug.LogError("TurretButton에 터렛 프리팹이 할당되지 않았습니다!");
                return;
            }
            //InGameManager.Instance.RequestTurretPlacement(turretPrefab);
        }
    }
}