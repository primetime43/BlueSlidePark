using UnityEngine;
using UnityEngine.UI;

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
/// Now uses the original extracted button textures from Resources/UI/.
/// </summary>
public class MuteQuality : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light directionalLight;

    [Header("UI Buttons (optional - auto-loaded sprites from Resources/UI/)")]
    [SerializeField] private Image muteButtonImage;
    [SerializeField] private Image qualityButtonImage;

    private bool muted;
    private bool lowQuality;
    private bool dirty;

    private Sprite soundOnSprite;
    private Sprite soundOffSprite;
    private Sprite highQualitySprite;
    private Sprite lowQualitySprite;

    private void Start()
    {
        LoadSprites();

        // Original: load from PlayerPrefs
        muted = PlayerPrefs.GetInt("Muted", 0) != 0;
        lowQuality = PlayerPrefs.GetInt("Low Quality", 0) != 0;
        dirty = true;
        ApplySettings();
    }

    private void LoadSprites()
    {
        soundOnSprite = LoadSprite("UI/SOUND_ON");
        soundOffSprite = LoadSprite("UI/SOUND_OFF");
        highQualitySprite = LoadSprite("UI/HIGH_QUALITY");
        lowQualitySprite = LoadSprite("UI/LOW_QUALITY");
    }

    private Sprite LoadSprite(string path)
    {
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
        }
        return null;
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
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                source.mute = true;
        }
        else
        {
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                source.mute = false;
        }

        // Original quality: toggle directional light shadows (Hard vs None)
        if (directionalLight != null)
        {
            directionalLight.shadows = lowQuality
                ? LightShadows.None
                : LightShadows.Hard;
        }

        // Update button sprites
        if (muteButtonImage != null)
        {
            Sprite s = muted ? soundOffSprite : soundOnSprite;
            if (s != null) muteButtonImage.sprite = s;
        }
        if (qualityButtonImage != null)
        {
            Sprite s = lowQuality ? lowQualitySprite : highQualitySprite;
            if (s != null) qualityButtonImage.sprite = s;
        }
    }

    public bool IsMuted => muted;
    public bool IsLowQuality => lowQuality;
}
