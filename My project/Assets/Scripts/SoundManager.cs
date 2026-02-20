using UnityEngine;

/// <summary>
/// Sound effects manager. Original game had separate sound references on
/// multiple classes (SliderMovement$pickupSound$, SliderMovement$deathSound$,
/// SliderMovement$slideNoise$, StartMenu$startSound$, StartMenu$buttonHoverSound$,
/// DeadMenu$restartSound$). This centralizes them like the original SoundManager singleton.
/// Original SWF had SOUND_ON2, SOUND_OFF, SOUND_ROLLOVER texture states for a mute toggle.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Game SFX")]
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip slideNoiseClip;

    [Header("UI SFX")]
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] private AudioClip startClip;
    [SerializeField] private AudioClip restartClip;

    [Header("Settings")]
    [SerializeField] private float pickupVolume = 1f;

    private AudioSource audioSource;
    private AudioSource slideNoiseSource;
    private bool soundOn = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Separate source for looping slide noise
        if (slideNoiseClip != null)
        {
            slideNoiseSource = gameObject.AddComponent<AudioSource>();
            slideNoiseSource.clip = slideNoiseClip;
            slideNoiseSource.loop = true;
            slideNoiseSource.volume = 0.3f;
            slideNoiseSource.playOnAwake = false;
        }
    }

    public void PlayPickup()
    {
        if (soundOn && audioSource != null && pickupClip != null)
            audioSource.PlayOneShot(pickupClip, pickupVolume);
    }

    public void PlayDeath()
    {
        if (soundOn && audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip);

        StopSlideNoise();
    }

    public void PlayButtonHover()
    {
        if (soundOn && audioSource != null && buttonHoverClip != null)
            audioSource.PlayOneShot(buttonHoverClip);
    }

    public void PlayStart()
    {
        if (soundOn && audioSource != null && startClip != null)
            audioSource.PlayOneShot(startClip);
    }

    public void PlayRestart()
    {
        if (soundOn && audioSource != null && restartClip != null)
            audioSource.PlayOneShot(restartClip);
    }

    public void StartSlideNoise()
    {
        if (soundOn && slideNoiseSource != null && !slideNoiseSource.isPlaying)
            slideNoiseSource.Play();
    }

    public void StopSlideNoise()
    {
        if (slideNoiseSource != null && slideNoiseSource.isPlaying)
            slideNoiseSource.Stop();
    }

    public void ToggleSound()
    {
        soundOn = !soundOn;
        if (!soundOn)
            StopSlideNoise();
    }

    public bool IsSoundOn() => soundOn;
}
