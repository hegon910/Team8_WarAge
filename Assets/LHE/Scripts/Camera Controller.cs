using UnityEngine;
using Photon.Pun;

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
        [SerializeField] private Transform spawnPointP1;
        [SerializeField] private Transform spawnPointP2;
        [SerializeField] private float minX = -20f;
        [SerializeField] private float maxX = 20f;


        private float currentSpeed = 0f;
        private float holdTime = 0f; // 접근된 시간 체크
        private int moveDirection = 0; // -1: 왼쪽, 1: 오른쪽, 0: 정지

        private PhotonView photonView;

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();

            // '내' 카메라가 맞는지 확인
            if (photonView != null && photonView.IsMine)
            {
                // 내 것이 맞으면, 기존의 초기화 로직을 그대로 수행
                Cursor.lockState = CursorLockMode.Confined;

                // IsMasterClient가 아닌 ActorNumber를 사용하는 것이 더 안정적입니다.
                Transform targetSpawnPoint = (PhotonNetwork.LocalPlayer.ActorNumber == 1) ? spawnPointP1 : spawnPointP2;

                // 스폰 포인트가 할당되어 있을 경우에만 위치 설정
                if (targetSpawnPoint != null)
                {
                    transform.position = new Vector3(targetSpawnPoint.position.x, transform.position.y, transform.position.z);
                }

                SetXLimits(minX, maxX);
            }
            else
            {
                // '내' 카메라가 아니면, 카메라와 오디오 리스너를 모두 비활성화
                GetComponent<Camera>().enabled = false;

                AudioListener listener = GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                }
            }
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

            // 이동 속도 가속
            if (moveDirection != 0)
            {
                holdTime += Time.deltaTime;
                currentSpeed = Mathf.Min(moveSpeed + holdTime * acceleration, maxSpeed);
            }
            else
            {
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
