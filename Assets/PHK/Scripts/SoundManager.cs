// SoundManager.cs
using UnityEngine;
using Photon.Pun;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private PhotonView photonView;

    [Header("����� �ҽ�")]
    [SerializeField] private AudioSource bgmSource; // ������ǿ�
    [SerializeField] private AudioSource sfxSource; // UI ȿ������ (����)

    [Header("����� Ŭ��")]
    public AudioClip backgroundMusic;
    public AudioClip uiClickSound;
    public AudioClip evolveSuccessSound;
    // --- RPC ����ȭ�� �ʿ��� ���� ---
    public AudioClip unitHitSound;
    public AudioClip ultimateSkillSound;
    public AudioClip unitDeadSound;

    public float worldSfxVolume = 5f;

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

    // --- RPC�� ��� �÷��̾�� ����ȭ�Ǵ� ���� ---

    // 3. �ñر� ��� �� ȣ�� (��� �÷��̾ ����)
    public void PlayUltimateSkillSound()
    {
        // ��� Ŭ���̾�Ʈ���� RPC_PlaySound�� ����ǵ��� ��û
        photonView.RPC("RPC_PlaySound", RpcTarget.All, "ultimate");
    }

    // 4. ���� �ǰ� �� ȣ�� (��� �÷��̾ ����)
    public void PlayUnitHitSound(Vector3 position)
    {
        // ��� Ŭ���̾�Ʈ���� RPC_PlaySoundAtPoint�� ����ǵ��� ��û
        photonView.RPC("RPC_PlaySoundAtPoint", RpcTarget.All, "hit", position);
    }
    public void PlayUnitDeadSound(Vector3 position)
    {
        // ��� ���嵵 ��ġ���� �����ϵ��� �����մϴ�.
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