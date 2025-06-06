using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    AudioSource bgAudioSource;
    AudioSource uiAudioSource;

    [SerializeField] AudioClip[] uiAudioClip;
    [SerializeField] AudioClip[] sfxAudioClip;

    GameObject sfxAudioSource;

    static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (!instance)
            {
                instance = new GameObject("AudioManager").AddComponent<AudioManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        instance = this;

        if (bgAudioSource == null)
        {
            bgAudioSource = new GameObject("BGMusic").AddComponent<AudioSource>();
            bgAudioSource.transform.parent = transform;
            bgAudioSource.loop = true;

            string clipName = SceneManager.GetActiveScene().name.Contains("Battle") ? "Game" : "Intro";
            AudioClip clip = Resources.Load<AudioClip>(clipName);

            if (clip != null)
            {
                bgAudioSource.clip = clip;
                bgAudioSource.Play();
            }
        }

        if (uiAudioSource == null)
        {
            uiAudioSource = new GameObject("UISoundFx").AddComponent<AudioSource>();
            uiAudioSource.transform.parent = transform;
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = new GameObject("SFX");
            sfxAudioSource.transform.parent = transform;
        }
    }

    void Update()
    {
        bgAudioSource.volume = SaveManager.Instance.state.volumeSettings[0]; //? 0.25f : 0f;
        uiAudioSource.volume = SaveManager.Instance.state.volumeSettings[1]; //? 1f : 0f;
    }

    public void PlayUISoundFX(UISoundFx uiSoundFx) { uiAudioSource.PlayOneShot(uiAudioClip[(int)uiSoundFx]); }

    public void PlaySfx(SoundEffect soundEffect, Vector3 vectorPos, float volume = 0.75f)
    {
        AudioSource sfx = new GameObject($"{soundEffect}").AddComponent<AudioSource>();
        sfx.volume = SaveManager.Instance.state.volumeSettings[1];
        sfx.spatialBlend = 0.75f;
        sfx.transform.position = vectorPos;
        sfx.PlayOneShot(sfxAudioClip[(int)soundEffect]);
        sfx.transform.parent = sfxAudioSource.transform;
        Destroy(sfx, sfxAudioClip[(int)soundEffect].length);
    }
}

public enum UISoundFx
{
    Click,
    Confirm
}

public enum SoundEffect
{
    Move,
    Shoot,
    Reload,
    BulletDrop,
    Death,
    Health
}