// SoundManager.cs
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Unity.VisualScripting;

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
    [SerializeField] private Button confirmButton;

    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���� �ٲ� �ı����� ����
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
        if (bgm > 1f) bgm *= 0.01f;  // �� 0~100 ����
        if (sfx > 1f) sfx *= 0.01f;

        SetBGMVolume(bgm);
        SetSFXVolume(sfx);
    }

    // 'Ȯ��' ��ư���� ȣ��: �����̴� �� �� ���� ���� + ����
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
        AudioListener.volume = v;                 // ����(��ü) ����
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
        // bgmSource�� lobbyBGM Ŭ���� ������ �������� �ʽ��ϴ�.
        if (bgmSource == null || lobbyBGM == null) return;

        // �̹� ���� ���� ��� ���̸� �ٲ��� �ʽ��ϴ�.
        if (bgmSource.isPlaying && bgmSource.clip == lobbyBGM) return;

        bgmSource.Stop();
        bgmSource.clip = lobbyBGM;
        bgmSource.loop = true; // BGM�� ���� �ݺ�����մϴ�.
        bgmSource.Play();
    }
    public void PlayBGM()
    {
        // �ΰ��� BGM Ŭ������ ��ü�ϴ� ����
        if (bgmSource == null || backgroundMusic == null) return;
        if (bgmSource.isPlaying && bgmSource.clip == backgroundMusic) return;

        bgmSource.Stop();
        bgmSource.clip = backgroundMusic;
        bgmSource.loop = true;
        bgmSource.Play();
    }
    public void StopBGM()
    {
        // ���ʿ��� ���� ����
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }
    // --- ���ÿ����� ����Ǵ� ���� ---

    // 1. UI Ŭ�� �� ȣ��
    public void PlayUIClick()
    {
        if (uiClickSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(uiClickSound);
        }
    }

    // 2. �ô� ��ȭ ���� �� ȣ��
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

    // --- RPC�� ��� �÷��̾�� ����ȭ�Ǵ� ���� ---

    // 3. �ñر� ��� �� ȣ�� (��� �÷��̾ ����)
    public void PlayUltimateSkillSound()
    {
        // UI ȿ���� �ҽ����� ���÷� �ٷ� ���
        if (ultimateSkillSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(ultimateSkillSound);
        }
    }

    // 4. ���� �ǰ� �� ȣ�� (��� �÷��̾ ����)
    public void PlayUnitHitSound(Vector3 position)
    {
        // �ش� ��ġ���� ���÷� �ٷ� ���
        if (unitHitSound != null)
        {
            AudioSource.PlayClipAtPoint(unitHitSound, position, worldSfxVolume);
        }
    }
    public void PlayUnitDeadSound(Vector3 position)
    {
        // �ش� ��ġ���� ���÷� �ٷ� ���
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
            // �� ����� ��ġ�� �߿����� �����Ƿ�, UI ȿ���� �ҽ����� ���
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
            // �ش� ��ġ�� ���带 ����մϴ�.
            AudioSource.PlayClipAtPoint(clipToPlay, position, worldSfxVolume);
        }


    }
}