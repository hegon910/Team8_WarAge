using UnityEngine;

namespace LHE
{
    public class CameraController : MonoBehaviour
    {
        [Header("카메라 이동 설정")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float edgeThreshold = 20f;
        [SerializeField] private float maxSpeed = 30f;        // 최대 속도
        [SerializeField] private float acceleration = 8f;     // 초당 가속도

        [Header("카메라 이동 제한")]
        [SerializeField] private float minX = -20f;
        [SerializeField] private float maxX = 20f;

        private Camera mainCamera;
        private float currentSpeed = 0f;
        private float holdTime = 0f; // 접근된 시간 체크
        private int moveDirection = 0; // -1: 왼쪽, 1: 오른쪽, 0: 정지

        private void Awake()
        {
            // 마우스 화면 가두기
            Cursor.lockState = CursorLockMode.Confined;

            mainCamera = Camera.main;
        }

        private void Update()
        {
            Vector3 mousePos = Input.mousePosition;
            float screenWidth = Screen.width;

            // 방향 판단
            if (mousePos.x <= edgeThreshold)
            {
                moveDirection = -1;
            }
            else if (mousePos.x >= screenWidth - edgeThreshold)
            {
                moveDirection = 1;
            }
            else
            {
                moveDirection = 0;
            }

            if (moveDirection != 0)
            {
                // 시간 누적과 가속 적용
                holdTime += Time.deltaTime;
                currentSpeed = Mathf.Min(moveSpeed + holdTime * acceleration, maxSpeed);
            }
            else
            {
                // 방향 해제 시 초기화
                holdTime = 0f;
                currentSpeed = 0f;
            }

            // 실제 이동
            Vector3 pos = transform.position;
            pos.x += moveDirection * currentSpeed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            transform.position = pos;
        }

        /// <summary>
        /// 외부에서 카메라의 X축 이동 제한을 설정
        /// </summary>
        /// <param name="min">최소</param>
        /// <param name="max">최대</param>
        public void SetXLimits(float min, float max)
        {
            minX = min;
            maxX = max;
        }
    }
}
