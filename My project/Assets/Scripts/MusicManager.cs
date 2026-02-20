using UnityEngine;

/// <summary>
/// Manages background music playback matching original Music class from SWF.
/// Decompiled source: Music.as
///
/// Original behavior:
///   Start: instance = this
///   Update: if (!source.isPlaying) PlaySong()
///   PlaySong: source.clip = songs[i]; source.Play(); source.loop = true;
///            i = (i + 1) % songs.Count
///
/// Note: Original sets loop=true on each song AND advances index.
/// The loop means each song plays forever until manually stopped/changed.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip[] songs;
    [SerializeField] private float volume = 0.5f;

    private AudioSource source;
    private int currentSongIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        // Original: source.loop = true (set in PlaySong, each song loops)
        source.loop = true;
        source.volume = volume;
        source.playOnAwake = false;
    }

    private void Start()
    {
        if (songs != null && songs.Length > 0)
            PlaySong(0);
    }

    private void Update()
    {
        // Auto-advance to next song when current finishes (original Music_Update behavior)
        if (source != null && !source.isPlaying && songs != null && songs.Length > 0)
        {
            currentSongIndex = (currentSongIndex + 1) % songs.Length;
            PlaySong(currentSongIndex);
        }
    }

    public void PlaySong(int index)
    {
        if (songs == null || index < 0 || index >= songs.Length) return;

        currentSongIndex = index;
        source.clip = songs[index];
        source.Play();
    }

    public void SetMute(bool mute)
    {
        if (source != null)
            source.mute = mute;
    }

    public bool IsMuted()
    {
        return source != null && source.mute;
    }
}
