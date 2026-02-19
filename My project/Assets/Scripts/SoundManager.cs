using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip deathClip;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayPickup()
    {
        if (audioSource != null && pickupClip != null)
            audioSource.PlayOneShot(pickupClip);
    }

    public void PlayDeath()
    {
        if (audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip);
    }
}
