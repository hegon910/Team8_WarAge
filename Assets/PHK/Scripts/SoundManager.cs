// SoundManager.cs
using UnityEngine;
using Photon.Pun;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private PhotonView photonView;

    [Header("오디오 소스")]
    [SerializeField] private AudioSource bgmSource; // 배경음악용
    [SerializeField] private AudioSource sfxSource; // UI 효과음용 (로컬)

    [Header("오디오 클립")]
    public AudioClip lobbyBGM;
    public AudioClip backgroundMusic;
    public AudioClip uiClickSound;
    public AudioClip evolveSuccessSound;
    public AudioClip addTurretSlot;
    // --- RPC 동기화가 필요한 사운드 ---
    public AudioClip unitHitSound;
    public AudioClip ultimateSkillSound;
    public AudioClip unitDeadSound;

    public float worldSfxVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
            photonView = GetComponent<PhotonView>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayLobbyBGM();

    }
    public void PlayLobbyBGM()
    {
        // bgmSource나 lobbyBGM 클립이 없으면 실행하지 않습니다.
        if (bgmSource == null || lobbyBGM == null) return;

        // 이미 같은 곡이 재생 중이면 바꾸지 않습니다.
        if (bgmSource.isPlaying && bgmSource.clip == lobbyBGM) return;

        bgmSource.Stop();
        bgmSource.clip = lobbyBGM;
        bgmSource.loop = true; // BGM은 보통 반복재생합니다.
        bgmSource.Play();
    }
    public void PlayBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }
    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }
    // --- 로컬에서만 재생되는 사운드 ---

    // 1. UI 클릭 시 호출
    public void PlayUIClick()
    {
        if (uiClickSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(uiClickSound);
        }
    }

    // 2. 시대 진화 성공 시 호출
    public void PlayEvolveSound()
    {
        if (evolveSuccessSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(evolveSuccessSound);
        }
    }
    public void PlayAddSlotSound()
    {
        if(addTurretSlot != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(addTurretSlot);
        }
    }

    // --- RPC로 모든 플레이어에게 동기화되는 사운드 ---

    // 3. 궁극기 사용 시 호출 (모든 플레이어가 들음)
    public void PlayUltimateSkillSound()
    {
        // 모든 클라이언트에서 RPC_PlaySound가 실행되도록 요청
        photonView.RPC("RPC_PlaySound", RpcTarget.All, "ultimate");
    }

    // 4. 유닛 피격 시 호출 (모든 플레이어가 들음)
    public void PlayUnitHitSound(Vector3 position)
    {
        // 모든 클라이언트에서 RPC_PlaySoundAtPoint가 실행되도록 요청
        photonView.RPC("RPC_PlaySoundAtPoint", RpcTarget.All, "hit", position);
    }
    public void PlayUnitDeadSound(Vector3 position)
    {
        // 사망 사운드도 위치값을 전달하도록 수정합니다.
        photonView.RPC(nameof(RPC_PlaySoundAtPoint), RpcTarget.All, "dead", position);
    }


    [PunRPC]
    private void RPC_PlaySound(string soundType)
    {
        AudioClip clipToPlay = null;
        switch (soundType)
        {
            case "ultimate":
                clipToPlay = ultimateSkillSound;
                break;
        }

        if (clipToPlay != null && sfxSource != null)
        {
            // 이 사운드는 위치가 중요하지 않으므로, UI 효과음 소스에서 재생
            sfxSource.PlayOneShot(clipToPlay);
        }
    }

    [PunRPC]
    private void RPC_PlaySoundAtPoint(string soundType, Vector3 position)
    {
        AudioClip clipToPlay = null;
        switch (soundType)
        {
            case "hit":
                clipToPlay = unitHitSound;
                break;
            case "dead":
                clipToPlay = unitDeadSound;
                break;
        }

        if (clipToPlay != null)
        {
            // 해당 위치에 사운드를 재생합니다.
            AudioSource.PlayClipAtPoint(clipToPlay, position, worldSfxVolume);
        }


    }
}