using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] AudioSource MusicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("Audio Clips")]
    public AudioClip background;
    public AudioClip walking;
    public AudioClip running;
    public AudioClip jumping;
    public AudioClip interact;

    void Start()
    {
        // Auto-play BGM when AudioManager starts in Game scene
        MusicSource.clip = background;
        MusicSource.loop = true;
        MusicSource.Play();
    }

    void Update()
    {
        if (MemMiniGameManager.Instance != null && MemMiniGameManager.Instance.IsMiniGameActive())
        {
            if (MusicSource.isPlaying)
                MusicSource.Pause();
        }
        else
        {
            if (!MusicSource.isPlaying)
                MusicSource.UnPause();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}