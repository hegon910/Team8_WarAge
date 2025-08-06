using Photon.Pun; // 네트워크 연동을 위한 using
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KYG
{
    /// <summary>
    /// 팀 타입을 위한 열거형 임의 지정 추후 네트워크 담당자와 협의후 수정
    /// </summary>
    /*public enum TeamType
    {
        Player1,Player2
    }*/

    public class BaseController : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback // PUN 연동 시 PhotonView 사용 가능
    {
        /// <summary>
        /// 기지 시스템 컨트롤러
        /// 체력, 피격, 업그레이드, 유닛 생성 , 터렛 설치
        /// 네트워크 담당자와 초기 스텟 존재 유무 논의 필요 초기 스텟만 받아오면 될지도
        /// 네트워크와 동기화 로직 필요
        /// </summary>

        [Header("Base")][SerializeField] private int maxHP; // 최대 체력

        private int currentHP; // 임의로 수정 불가능한 현재 체력

        public string TeamTag { get; private set; } // 팀 태그 (1P/2P)
        private string teamTag;

        // 프로퍼티로 외부 접근 캡슐화
        public int MaxHP => maxHP; // 최대 체력 접근용 프로퍼티

        public int CurrentHP => currentHP; // 현재 체력 접근용 프로퍼티

        [SerializeField] private SpriteRenderer mySpriteRenderer;

        [Header("Spawner")] public Transform spawnerPoint; // 유닛 생성 위치
        [SerializeField] private GameObject currentBaseModel;
        [SerializeField] private GameObject defaultUnitPrefab; // 생성할 유닛 프리팹

        [Header("Turret Slot")]
        [SerializeField] public TurretSlot[] turretSlots; // 미리 BasePrefab에 붙여놓을 슬롯 배열
        private int unlockedSlotCount = 0; // 현재 열려 있는 슬롯 개수
        //[SerializeField] private GameObject turretSlotPrefab;

        //[SerializeField] private Transform turretSlotParent;

        //[SerializeField] private int maxTurretSlots = 4;
        //private readonly List<TurretSlot> turretSlots = new();

        private PhotonView pv; // 네트워크 식별용 포톤 뷰
        public event Action<int, int> OnHpChanged; //HP 변동시 이벤트 발생 (최대 체력, 현재 체력) 


        private void Awake()
        {
            pv = GetComponent<PhotonView>();
            
            // 시작할 때 모든 슬롯을 비활성화
            foreach (var slot in turretSlots)
                slot.gameObject.SetActive(false);

        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            // 1. TestNetworkManager가 보낸 InstantiationData에서 팀 태그를 받아옵니다.
            this.TeamTag = (string)info.photonView.InstantiationData[0];

            // 2. GameObject의 태그를 받아온 팀 태그로 설정합니다.
            this.gameObject.tag = this.TeamTag;
            Debug.Log($"기지가 생성되었습니다. 팀: {this.teamTag}, GameObject 태그: {this.gameObject.tag}");

            // 3. 팀 태그가 "BaseP2"인 경우, 스프라이트와 콜라이더를 반전시킵니다.
            if (this.TeamTag == "BaseP2")
            {
                // 객체의 로컬 스케일의 x값을 -1로 바꿔 한 번에 뒤집습니다.
                // 이 방법이 스프라이트와 콜라이더를 각각 제어하는 것보다 간단하고 효율적입니다.
                Vector3 currentScale = transform.localScale;
                currentScale.x *= -1;
                transform.localScale = currentScale;

                Debug.Log("BaseP2 기체의 transform.localScale.x 값을 반전시켰습니다.");
            }

            // 4. InGameManager에 자기 자신을 등록합니다.
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.RegisterBase(this, this.TeamTag);
            }
            else
            {
                Debug.LogError("InGameManager를 찾을 수 없습니다! 씬에 InGameManager가 있는지 확인하세요.");
            }
        }

        /*
         비동기 초기화 순서에서 늦게 호출될 가능성 있음
         OnEnable() 또는 Awake()에서 초기화하는 방식도 추후 테스트 후 고려
        */
        public void Start() // 게임 시작시 
        {
            if (pv != null && pv.InstantiationData != null && pv.InstantiationData.Length > 0)
            {
                string teamFromPhoton = (string)pv.InstantiationData[0];
                InitializeTeam(teamFromPhoton);
            }
            InitBase(); // 기지 초기화
         
           // if (AgeManager.Instance != null)
           //     AgeManager.Instance.OnAgeChangedByTeam += (teamTag, nextAgeData) =>
           //     {
           //         if (teamTag == TeamTag)
           //             UpgradeBaseByAge(nextAgeData); // 시대 변경 이벤트 받아 자동 업그레이드 처리
           //     };
        }
        public void InitializeTeam(string team)
        {
            if (string.IsNullOrEmpty(TeamTag))
            {
                // TestNetworkManager가 보낸 "BaseP1" 또는 "BaseP2"를 TeamTag와 gameObject.tag에 모두 설정
                TeamTag = team;
              //  gameObject.tag = TeamTag;

                if (TeamTag == "BaseP1")
                    gameObject.layer = LayerMask.NameToLayer("P1Base");
                else if (TeamTag == "BaseP2")
                    gameObject.layer = LayerMask.NameToLayer("P2Base");

                Debug.Log($"{gameObject.name}의 TeamTag와 GameObject.tag가 '{TeamTag}'로 설정되었습니다.");
             

                if (InGameManager.Instance != null)
                {
                    // InGameManager에는 "BaseP1" 또는 "BaseP2" 태그를 그대로 전달합니다.
                    InGameManager.Instance.RegisterBase(this, this.TeamTag);
                }
            }
        }
        public void InitBase()
        {
            currentHP = maxHP; // 현재 체력 = 최대 체력으로 초기화
            UpdateHpUI(); // UI연동
        }

        private void UpdateHpUI() => OnHpChanged?.Invoke(currentHP, maxHP); // UI로 HP 갱신 이벤트


        // 슬롯 해금
        public void UnlockNextTurretSlot(int cost)
        {
            // 슬롯 해금은 내 베이스에서만 가능
            // 디버그 모드에서는 isMine이 false일 수 있으므로 PhotonNetwork.IsConnected 조건을 추가
            if (PhotonNetwork.IsConnected && !pv.IsMine) return;

            if (unlockedSlotCount >= turretSlots.Length)
            {
                Debug.Log("더 이상 활성화할 터렛 슬롯이 없습니다.");
                return;
            }

            if (PhotonNetwork.IsConnected)
            {
                // 1. 네트워크 모드일 경우: RPC를 통해 모든 클라이언트에 활성화를 요청
                pv.RPC(nameof(RPC_ActivateSlotAtIndex), RpcTarget.All, unlockedSlotCount);
            }
            else
            {
                // 2. 디버그 모드일 경우: 로컬에서 직접 활성화 함수를 호출
                ActivateSlot(unlockedSlotCount);
            }
        }

        private void ActivateSlot(int slotIndex)
        {
            // 유효한 인덱스인지 확인
            if (slotIndex < 0 || slotIndex >= turretSlots.Length) return;

            // 해당 인덱스의 슬롯을 가져와 활성화
            TurretSlot slotToActivate = turretSlots[slotIndex];
            if (slotToActivate != null)
            {
                slotToActivate.gameObject.SetActive(true);
                Debug.Log($"슬롯 {slotToActivate.name} (인덱스: {slotIndex}) 활성화.");
            }

            // 활성화된 슬롯 개수를 업데이트 (로컬/모든 클라이언트 공통)
            this.unlockedSlotCount = slotIndex + 1;
        }
        /// <summary>
        /// 데미지를 받으면 해당 공력력 만큼 현재체력 감소
        /// 체력 UI에 기지 체력 연동 필요
        /// 데미지를 받아 현재 체력이 0이 되면 게임 매니저에 게임 오버 연동
        /// 체력이 0이 될시 파괴되는 에니메이션은 추가 과제
        /// </summary>
        /// 
        [PunRPC]
        public void RpcTakeDamage(int damage, string attackerTag)
        {
            // 이 RPC는 모든 클라이언트에서 실행되지만,
            // 실제 데미지 처리는 아래 private TakeDamage 함수의 소유권 체크 로직을 따릅니다.
            TakeDamage(damage, attackerTag);
        }
        public void TakeDamage(int damage, string attackerTag)
        {
            if (attackerTag == TeamTag) return; // 아군이면 무시

            // 'pv.IsMine' 체크 덕분에, 이 베이스의 소유자(마스터 클라이언트)만 아래 로직을 실행하게 됩니다.
            if (damage <= 0 || (PhotonNetwork.IsConnected && !pv.IsMine)) return;

            ApplyDamage(damage); // 소유자 클라이언트에서만 체력 감소

            // RPC로 다른 클라이언트(P2)에게 변경된 HP 상태를 동기화합니다.
            if (PhotonNetwork.IsConnected)
                pv.RPC(nameof(RPC_UpdateHP), RpcTarget.Others, currentHP);
        }

        [PunRPC]
        public void RPC_ActivateSlotAtIndex(int slotIndex)
        {
            // 실제 로직은 ActivateSlot 함수에 위임
            ActivateSlot(slotIndex);
            Debug.Log($"RPC 수신: 슬롯 인덱스 {slotIndex} 활성화 로직 실행.");
        }

        /// <summary>
        /// RPC 다른 클라이언트에서 체력 동기화 호출
        /// </summary>
        /// <param name="newHp"></param>
        [PunRPC]
        void RPC_UpdateHP(int newHp)
        {
            currentHP = newHp;
            UpdateHpUI();
            if (currentHP <= 0)
            {
                BaseDestroyed();
            }
        }


        /// <summary>
        /// HP 감소 로직
        /// </summary>
        /// <param name="damage"></param>
        private void ApplyDamage(int damage)
        {
            currentHP = Mathf.Max(0, currentHP - damage);
            UpdateHpUI();
            if (currentHP <= 0)
            {
                BaseDestroyed();
            }
        }

        /// <summary>
        /// 기지 체력 0일때 호출
        /// 게임 매니져 게임오버랑 연동
        /// </summary>

        private void BaseDestroyed()
        {
            Debug.Log("Base destroyed");
            //GameManager.Instance?.GameOver(teamType);
        }

        /// <summary>
        /// PhotonNetwork를 사용하여 멀티플레이에 대응
        /// UI 버튼이 클릭 되면 해당 버튼에 연결된 유닛이
        /// 지정한 스폰 포인트에 생성
        /// </summary>
        public void SpawnUnit(GameObject prefabToSpawn) // 향후 유닛 확장성 가능성 고려 파라미터로 받을수 있게 수정
        {
            if (pv == null || !pv.IsMine) return; // 포톤뷰 사용하여 내 소유 기지에서만 생성가능하게 제한

            prefabToSpawn ??= defaultUnitPrefab;


            PhotonNetwork.Instantiate(prefabToSpawn.name, spawnerPoint.position, Quaternion.identity);
            // 네트워크 상에서 유닛 생성 Quaternion.identity로 회전없이 정방향으로만 생성
        }

        /// <summary>
        /// 일정 경험치가 충족되면 기지 업그레이드 가능
        /// 기지가 업그레이드 되면 최대 체력 증가 및 체력 회복
        /// 추후 시대 변화 시스템과 연동 하여 시대가 변화 할때 기지도 변화
        /// 기지는 부모 해당 시대의 기지는 자식관계로 놓고 해당 시대가 되면 SetActive?
        /// </summary>
        public void UpgradeBaseByAge(AgeData nextAgeData)
        {
            Debug.LogError($"--- UpgradeBaseByAge CALLED for {gameObject.name} ---");
            // TODO 업그래이드 기능 구현
            this.maxHP = nextAgeData.maxHP; // 최대 체력 업그레이드
            this.currentHP = maxHP; // 현재 체력 업그레이드 및 회복
            if (mySpriteRenderer != null && nextAgeData.baseSprite != null)
            {
                Debug.Log("새로운 baseSprite가 있습니다. 스프라이트를 교체합니다.");
                mySpriteRenderer.sprite = nextAgeData.baseSprite;
            }
            else
            {
                Debug.LogWarning("경고: SpriteRenderer 또는 nextAgeData의 baseSprite가 할당되지 않았습니다.");
            }
            // TODO : newAgeData.baseModelPrefab 적용하여 외형 변경 가능하도록 기능 구현
            if (nextAgeData.baseModelPrefab != null)
            {
                if (currentBaseModel != null) Destroy(currentBaseModel);
                currentBaseModel = Instantiate(nextAgeData.baseModelPrefab, transform);
            }

            UpdateHpUI();


        }
        /// <summary>
        /// 유닛과 마찬가지로 터렛 프리펩을 받아
        /// 터렛을 설치 할수있는 위치에 설치
        /// 터렛 설치 할수있는 장소는 총 4곳
        /// 첫번째 장소는 기지에 설치
        /// 수직으로 설치하며 터렛 설치 UI버튼 눌리면 자동으로 터렛 설치 장소 생성
        /// 터렛 설지 가능 장소 미리 구현하고 UI버튼으로 구매 확인후 SetActive?
        /// 설치 장소에만 터렛 설치 가능
        /// </summary>

        /// <summary>
        /// 슬롯 추가: Base 위에 새로운 터렛 설치 공간 생성
        /// </summary>
        /*public void CreateTurretSlot()
        {
            if (turretSlots.Count >= maxTurretSlots || turretSlotPrefab == null) return;

            var slotObj =
                PhotonNetwork.Instantiate(turretSlotPrefab.name, turretSlotParent.position, Quaternion.identity);


            TurretSlot slot = slotObj.GetComponent<TurretSlot>();
            slot.Init(TeamTag);   // 팀 정보 전달
            turretSlots.Add(slot);

            if (PhotonNetwork.IsConnected)
                pv.RPC(nameof(RPC_RepositionTurretSlots), RpcTarget.All);
            else
            {
                RepositionTurretSlots();
            }
        }

        /// <summary>
        /// 슬롯 위치를 정렬 (일렬 배치)
        /// </summary>

        [PunRPC]
        private void RPC_RepositionTurretSlots()
        {
            RepositionTurretSlots();
        }
        private void RepositionTurretSlots()
        {
            float spacing = 1.5f;
            for (int i = 0; i < turretSlots.Count; i++)
            {
                turretSlots[i].transform.SetParent(turretSlotParent, false);
                turretSlots[i].transform.localPosition = new Vector3(i * spacing, 0, 0);
            }
        }*/


    }


}

