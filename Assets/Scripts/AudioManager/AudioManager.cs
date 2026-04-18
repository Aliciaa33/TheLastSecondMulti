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

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}