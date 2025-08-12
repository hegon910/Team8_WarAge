
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections.Generic; // Added for the new finding logic
using System.Linq; // Added for the new finding logic

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

    // confirmButton은 OptionManager에서 처리하므로 여기서는 제거
    // [SerializeField] private Button confirmButton; 

    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // PhotonView 컴포넌트가 SoundManager에 추가되어야 합니다.
            photonView = GetComponent<PhotonView>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 씬 로드 시 슬라이더 재할당 및 이벤트 리스너 연결
        if (scene.name == "LobbyScene")
        {
            // 기존의 FindObjectOfType<Slider>()는 씬에 슬라이더가 2개 이상일 때 문제를 일으킵니다.
            // 이름이나 태그를 사용하거나, 특정 부모 오브젝트 내에서 찾는 방식으로 변경합니다.
            // 여기서는 FindObjectsOfType을 사용하여 여러 슬라이더를 찾고, 이름으로 구분하는 방식을 사용합니다.

            // 모든 슬라이더를 찾아서 Dictionary에 저장합니다.
            Dictionary<string, Slider> sliders = FindObjectsOfType<Slider>(true)
                .ToDictionary(s => s.name);

            // BGM 슬라이더 할당
            if (sliders.TryGetValue("BGM Sound Slider", out Slider foundBgmSlider))
            {
                bgmSlider = foundBgmSlider;
                bgmSlider.onValueChanged.RemoveAllListeners(); // 기존 리스너 제거
                bgmSlider.onValueChanged.AddListener(SetBGMVolume);
                bgmSlider.value = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
                Debug.Log("LobbyScene에서 BGM 슬라이더를 찾고 연결했습니다.");
            }
            else
            {
                Debug.LogWarning("LobbyScene에서 BGM 슬라이더(BGM Sound Slider)를 찾을 수 없습니다.");
            }

            // SFX 슬라이더 할당
            if (sliders.TryGetValue("SFX Sound Slider", out Slider foundSfxSlider))
            {
                sfxSlider = foundSfxSlider;
                sfxSlider.onValueChanged.RemoveAllListeners(); // 기존 리스너 제거
                sfxSlider.onValueChanged.AddListener(SetSFXVolume);
                sfxSlider.value = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
                Debug.Log("LobbyScene에서 SFX 슬라이더를 찾고 연결했습니다.");
            }
            else
            {
                Debug.LogWarning("LobbyScene에서 SFX 슬라이더(SFX Sound Slider)를 찾을 수 없습니다.");
            }

            // 슬라이더가 찾아지면, UI를 통해 볼륨을 제어할 수 있도록 설정
            ApplySavedVolumesToAudio();
        }
    }

    private void Start()
    {
        // Start에서 UI를 찾는 로직은 OnSceneLoaded로 이동되었으므로 삭제합니다.
        // BGM 재생은 여기서 유지합니다.
        PlayLobbyBGM();
    }

    private void ApplySavedVolumesToAudio()
    {
        float bgm = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        float sfx = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        if (bgm > 1f) bgm *= 0.01f;
        if (sfx > 1f) sfx *= 0.01f;

        // 실제 AudioSource 볼륨에 적용
        if (bgmSource) bgmSource.volume = bgm;
        if (sfxSource) sfxSource.volume = sfx;
    }

    // '확인' 버튼에서 호출: 슬라이더 값 → 실제 적용 + 저장
    public void ApplyAudioSettings()
    {
        float bgm = bgmSlider ? Mathf.Clamp01(bgmSlider.value) : PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        float sfx = sfxSlider ? Mathf.Clamp01(sfxSlider.value) : PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

        SetBGMVolume(bgm);
        SetSFXVolume(sfx);

        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgm);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfx);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float v)
    {
        v = Mathf.Clamp01(v);
        if (bgmSource) bgmSource.volume = v;
    }

    public void SetSFXVolume(float v)
    {
        v = Mathf.Clamp01(v);
        if (sfxSource) sfxSource.volume = v;
    }

    // 이하 기존 Play... 및 RPC 관련 함수들은 변경 없음
    public void PlayLobbyBGM()
    {
        if (bgmSource == null || lobbyBGM == null) return;
        if (bgmSource.isPlaying && bgmSource.clip == lobbyBGM) return;

        bgmSource.Stop();
        bgmSource.clip = lobbyBGM;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlayBGM()
    {
        if (bgmSource == null || backgroundMusic == null) return;
        if (bgmSource.isPlaying && bgmSource.clip == backgroundMusic) return;

        bgmSource.Stop();
        bgmSource.clip = backgroundMusic;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    public void PlayUIClick()
    {
        if (uiClickSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(uiClickSound);
        }
    }

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

    public void PlayUltimateSkillSound()
    {
        if (ultimateSkillSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(ultimateSkillSound);
        }
    }

    public void PlayUnitHitSound(Vector3 position)
    {
        if (unitHitSound != null)
        {
            AudioSource.PlayClipAtPoint(unitHitSound, position, worldSfxVolume);
        }
    }

    public void PlayUnitDeadSound(Vector3 position)
    {
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
            AudioSource.PlayClipAtPoint(clipToPlay, position, worldSfxVolume);
        }
    }
}