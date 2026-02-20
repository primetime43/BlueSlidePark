using UnityEngine;

/// <summary>
/// Sound effects manager matching original SoundManager from SWF.
/// Decompiled source: SoundManager.as
///
/// Original SoundManager was surprisingly simple:
///   - Singleton with DontDestroyOnLoad
///   - Update(): positions itself at Camera.main position each frame
///   - No audio clip fields (sounds were on individual classes)
///
/// In our recreation, we centralize the sound clips here since the original's
/// approach of storing sounds on SliderMovement/StartMenu/DeadMenu doesn't
/// translate well to Unity 6 URP architecture.
///
/// Original sound references from decompiled code:
///   SliderMovement: pickupSound (AudioClip), pickupVol (float), deathSound (AudioClip),
///                   slideNoise (AudioSource on Camera child "SlideNoise")
///   StartMenu: startSound, buttonHoverSound
///   DeadMenu: restartSound
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Game SFX (from SliderMovement)")]
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip slideNoiseClip;

    [Header("UI SFX (from StartMenu/DeadMenu)")]
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
            Destroy(this);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Original: slideNoise was a separate AudioSource on Camera child "SlideNoise"
        if (slideNoiseClip != null)
        {
            slideNoiseSource = gameObject.AddComponent<AudioSource>();
            slideNoiseSource.clip = slideNoiseClip;
            slideNoiseSource.loop = true;
            slideNoiseSource.volume = 0.3f;
            slideNoiseSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        // Original SoundManager.Update: position at Camera.main position
        if (Camera.main != null)
            transform.position = Camera.main.transform.position;
    }

    /// <summary>
    /// Original: Camera.main.audio.PlayOneShot(pickupSound, pickupVol)
    /// </summary>
    public void PlayPickup()
    {
        if (soundOn && audioSource != null && pickupClip != null)
            audioSource.PlayOneShot(pickupClip, pickupVolume);
    }

    /// <summary>
    /// Original: Camera.main.audio.PlayOneShot(deathSound)
    /// </summary>
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

    /// <summary>
    /// Sets slide noise volume. Original: slideNoise.volume = Clamp01(num2 * 2)
    /// where num2 is based on how close to death angle.
    /// </summary>
    public void SetSlideNoiseVolume(float volume)
    {
        if (slideNoiseSource != null)
            slideNoiseSource.volume = Mathf.Clamp01(volume);
    }

    public void ToggleSound()
    {
        soundOn = !soundOn;
        if (!soundOn)
            StopSlideNoise();
    }

    public bool IsSoundOn() => soundOn;
}
