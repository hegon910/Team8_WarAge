using UnityEngine;
using Photon.Pun;

namespace LHE
{
    public class CameraController : MonoBehaviour
    {
        [Header("ī�޶� �̵� ����")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float edgeThreshold = 20f;
        [SerializeField] private float maxSpeed = 30f;        // �ִ� �ӵ�
        [SerializeField] private float acceleration = 8f;     // �ʴ� ���ӵ�

        [Header("ī�޶� �̵� ����")]
        [SerializeField] private Transform spawnPointP1;
        [SerializeField] private Transform spawnPointP2;
        [SerializeField] private float minX = -20f;
        [SerializeField] private float maxX = 20f;


        private float currentSpeed = 0f;
        private float holdTime = 0f; // ���ٵ� �ð� üũ
        private int moveDirection = 0; // -1: ����, 1: ������, 0: ����

        private PhotonView photonView;

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();

            // '��' ī�޶� �´��� Ȯ��
            if (photonView != null && photonView.IsMine)
            {
                // �� ���� ������, ������ �ʱ�ȭ ������ �״�� ����
                Cursor.lockState = CursorLockMode.Confined;

                // IsMasterClient�� �ƴ� ActorNumber�� ����ϴ� ���� �� �������Դϴ�.
                Transform targetSpawnPoint = (PhotonNetwork.LocalPlayer.ActorNumber == 1) ? spawnPointP1 : spawnPointP2;

                // ���� ����Ʈ�� �Ҵ�Ǿ� ���� ��쿡�� ��ġ ����
                if (targetSpawnPoint != null)
                {
                    transform.position = new Vector3(targetSpawnPoint.position.x, transform.position.y, transform.position.z);
                }

                SetXLimits(minX, maxX);
            }
            else
            {
                // '��' ī�޶� �ƴϸ�, ī�޶�� ����� �����ʸ� ��� ��Ȱ��ȭ
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

            // ���� �Ǵ�
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

            // �̵� �ӵ� ����
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

            // ���� �̵�
            Vector3 pos = transform.position;
            pos.x += moveDirection * currentSpeed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            transform.position = pos;
        }

        /// <summary>
        /// �ܺο��� ī�޶��� X�� �̵� ������ ����
        /// </summary>
        /// <param name="min">�ּ�</param>
        /// <param name="max">�ִ�</param>
        public void SetXLimits(float min, float max)
        {
            minX = min;
            maxX = max;
        }
    }
}
