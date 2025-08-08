// SoundManager.cs
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
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

    [Range(0f, 1f)] public float worldSfxVolume = 1f;

    [Header("볼륨 조절")]
    [SerializeField] private Slider bgmSlider;    // BGM 볼륨 조절 슬라이더
    [SerializeField] private Slider sfxSlider;    // SFX 볼륨 조절 슬라이더
    [SerializeField] private Button confirmButton;

    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

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
        ApplySavedVolumesToAudio();
        if (bgmSlider) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        PlayLobbyBGM();
    }

    private void ApplySavedVolumesToAudio()
    {
        float bgm = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        float sfx = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        if (bgm > 1f) bgm *= 0.01f;  // 옛 0~100 보정
        if (sfx > 1f) sfx *= 0.01f;

        SetBGMVolume(bgm);
        SetSFXVolume(sfx);
    }

    // '확인' 버튼에서 호출: 슬라이더 값 → 실제 적용 + 저장
    public void ApplyAudioSettings()
    {
        float bgm = bgmSlider ? Mathf.Clamp01(bgmSlider.value) : 1f;
        float sfx = sfxSlider ? Mathf.Clamp01(sfxSlider.value) : 1f;

        SetBGMVolume(bgm); 
        SetSFXVolume(sfx);

        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgm);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfx);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float v)
    {
        v = Mathf.Clamp01(v);
        AudioListener.volume = v;                 // 전역(전체) 볼륨
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, v);
    }

    public void SetSFXVolume(float v)
    {
        v = Mathf.Clamp01(v);
        if (sfxSource) sfxSource.volume = v;
        worldSfxVolume = v;
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
        // 인게임 BGM 클립으로 교체하는 로직
        if (bgmSource == null || backgroundMusic == null) return;
        if (bgmSource.isPlaying && bgmSource.clip == backgroundMusic) return;

        bgmSource.Stop();
        bgmSource.clip = backgroundMusic;
        bgmSource.loop = true;
        bgmSource.Play();
    }
    public void StopBGM()
    {
        // 불필요한 조건 제거
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
        if (addTurretSlot != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(addTurretSlot);
        }
    }

    // --- RPC로 모든 플레이어에게 동기화되는 사운드 ---

    // 3. 궁극기 사용 시 호출 (모든 플레이어가 들음)
    public void PlayUltimateSkillSound()
    {
        // UI 효과음 소스에서 로컬로 바로 재생
        if (ultimateSkillSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(ultimateSkillSound);
        }
    }

    // 4. 유닛 피격 시 호출 (모든 플레이어가 들음)
    public void PlayUnitHitSound(Vector3 position)
    {
        // 해당 위치에서 로컬로 바로 재생
        if (unitHitSound != null)
        {
            AudioSource.PlayClipAtPoint(unitHitSound, position, worldSfxVolume);
        }
    }
    public void PlayUnitDeadSound(Vector3 position)
    {
        // 해당 위치에서 로컬로 바로 재생
        if (unitDeadSound != null)
        {
            AudioSource.PlayClipAtPoint(unitDeadSound, position, worldSfxVolume);
        }
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