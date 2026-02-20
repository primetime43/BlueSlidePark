using UnityEngine;

/// <summary>
/// Mute and quality toggle matching original MuteQuality class from SWF.
/// Decompiled source: MuteQuality.as
///
/// Original behavior:
///   Start: loads "Muted" and "Low Quality" from PlayerPrefs
///   Update: when dirty, applies mute (AudioListener.pause/volume/mute all sources)
///           and quality (directionalLight.shadows = None or Hard)
///   OnGUI: mute toggle button (muteOn/muteOff/muteRollover textures)
///          quality toggle button (qualityOn/qualityOff/qualityRollover textures)
///
/// In our Unity 6 recreation, this is simplified to use UI buttons or keyboard toggles
/// since we don't have the original OnGUI texture assets yet.
/// </summary>
public class MuteQuality : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light directionalLight;

    private bool muted;
    private bool lowQuality;
    private bool dirty;

    private void Start()
    {
        // Original: load from PlayerPrefs
        muted = PlayerPrefs.GetInt("Muted", 0) != 0;
        lowQuality = PlayerPrefs.GetInt("Low Quality", 0) != 0;
        dirty = true;
        ApplySettings();
    }

    private void Update()
    {
        if (dirty)
        {
            ApplySettings();
            dirty = false;
        }

        // Keyboard shortcuts (M for mute, Q for quality)
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMute();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleQuality();
        }
    }

    public void ToggleMute()
    {
        muted = !muted;
        dirty = true;
    }

    public void ToggleQuality()
    {
        lowQuality = !lowQuality;
        dirty = true;
    }

    private void ApplySettings()
    {
        // Original: save to PlayerPrefs
        PlayerPrefs.SetInt("Muted", muted ? 1 : 0);
        PlayerPrefs.SetInt("Low Quality", lowQuality ? 1 : 0);

        // Original mute: AudioListener.pause, AudioListener.volume = 0, mute all AudioSources
        if (muted)
        {
            AudioListener.pause = true;
            AudioListener.volume = 0f;
            foreach (var source in GetComponentsInChildren<AudioSource>())
                source.mute = true;
        }
        else
        {
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            foreach (var source in GetComponentsInChildren<AudioSource>())
                source.mute = false;
        }

        // Original quality: toggle directional light shadows (Hard vs None)
        if (directionalLight != null)
        {
            directionalLight.shadows = lowQuality
                ? LightShadows.None
                : LightShadows.Hard;
        }
    }

    public bool IsMuted => muted;
    public bool IsLowQuality => lowQuality;
}
