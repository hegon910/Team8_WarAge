
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections.Generic; // Added for the new finding logic
using System.Linq; // Added for the new finding logic

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private PhotonView photonView;

    [Header("����� �ҽ�")]
    [SerializeField] private AudioSource bgmSource; // ������ǿ�
    [SerializeField] private AudioSource sfxSource; // UI ȿ������ (����)

    [Header("����� Ŭ��")]
    public AudioClip lobbyBGM;
    public AudioClip backgroundMusic;
    public AudioClip uiClickSound;
    public AudioClip evolveSuccessSound;
    public AudioClip addTurretSlot;
    // --- RPC ����ȭ�� �ʿ��� ���� ---
    public AudioClip unitHitSound;
    public AudioClip ultimateSkillSound;
    public AudioClip unitDeadSound;

    [Range(0f, 1f)] public float worldSfxVolume = 1f;

    [Header("���� ����")]
    [SerializeField] private Slider bgmSlider;    // BGM ���� ���� �����̴�
    [SerializeField] private Slider sfxSlider;    // SFX ���� ���� �����̴�

    // confirmButton�� OptionManager���� ó���ϹǷ� ���⼭�� ����
    // [SerializeField] private Button confirmButton; 

    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // PhotonView ������Ʈ�� SoundManager�� �߰��Ǿ�� �մϴ�.
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
        // �� �ε� �� �����̴� ���Ҵ� �� �̺�Ʈ ������ ����
        if (scene.name == "LobbyScene")
        {
            // ������ FindObjectOfType<Slider>()�� ���� �����̴��� 2�� �̻��� �� ������ ����ŵ�ϴ�.
            // �̸��̳� �±׸� ����ϰų�, Ư�� �θ� ������Ʈ ������ ã�� ������� �����մϴ�.
            // ���⼭�� FindObjectsOfType�� ����Ͽ� ���� �����̴��� ã��, �̸����� �����ϴ� ����� ����մϴ�.

            // ��� �����̴��� ã�Ƽ� Dictionary�� �����մϴ�.
            Dictionary<string, Slider> sliders = FindObjectsOfType<Slider>(true)
                .ToDictionary(s => s.name);

            // BGM �����̴� �Ҵ�
            if (sliders.TryGetValue("BGM Sound Slider", out Slider foundBgmSlider))
            {
                bgmSlider = foundBgmSlider;
                bgmSlider.onValueChanged.RemoveAllListeners(); // ���� ������ ����
                bgmSlider.onValueChanged.AddListener(SetBGMVolume);
                bgmSlider.value = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
                Debug.Log("LobbyScene���� BGM �����̴��� ã�� �����߽��ϴ�.");
            }
            else
            {
                Debug.LogWarning("LobbyScene���� BGM �����̴�(BGM Sound Slider)�� ã�� �� �����ϴ�.");
            }

            // SFX �����̴� �Ҵ�
            if (sliders.TryGetValue("SFX Sound Slider", out Slider foundSfxSlider))
            {
                sfxSlider = foundSfxSlider;
                sfxSlider.onValueChanged.RemoveAllListeners(); // ���� ������ ����
                sfxSlider.onValueChanged.AddListener(SetSFXVolume);
                sfxSlider.value = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
                Debug.Log("LobbyScene���� SFX �����̴��� ã�� �����߽��ϴ�.");
            }
            else
            {
                Debug.LogWarning("LobbyScene���� SFX �����̴�(SFX Sound Slider)�� ã�� �� �����ϴ�.");
            }

            // �����̴��� ã������, UI�� ���� ������ ������ �� �ֵ��� ����
            ApplySavedVolumesToAudio();
        }
    }

    private void Start()
    {
        // Start���� UI�� ã�� ������ OnSceneLoaded�� �̵��Ǿ����Ƿ� �����մϴ�.
        // BGM ����� ���⼭ �����մϴ�.
        PlayLobbyBGM();
    }

    private void ApplySavedVolumesToAudio()
    {
        float bgm = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        float sfx = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        if (bgm > 1f) bgm *= 0.01f;
        if (sfx > 1f) sfx *= 0.01f;

        // ���� AudioSource ������ ����
        if (bgmSource) bgmSource.volume = bgm;
        if (sfxSource) sfxSource.volume = sfx;
    }

    // 'Ȯ��' ��ư���� ȣ��: �����̴� �� �� ���� ���� + ����
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

    // ���� ���� Play... �� RPC ���� �Լ����� ���� ����
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