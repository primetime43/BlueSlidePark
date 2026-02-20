using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the death/game-over screen with Mac Miller-themed score tiers.
/// Decompiled source: DeadMenu.as
///
/// Original uses strictly greater-than (>) for tier comparisons.
/// Prize claiming available at score >= 1500.
/// Restart via: Space, Enter, KeypadEnter, R, or restart button click.
/// Original: restartButton/restartButtonHover textures, GUIStyle-based.
/// Thresholds are serialized (set in scene data, not hardcoded in script).
///
/// Tier sprites are auto-loaded from Resources/Tiers/ if not assigned in inspector.
/// Background and restart images auto-loaded from Resources/UI/.
/// </summary>
public class DeadMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image tierImage;
    [SerializeField] private Image restartImage;
    [SerializeField] private Image prizeMessageImage;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private Text tierTitleText;

    [Header("Tier Textures (auto-loaded from Resources/Tiers/ if null)")]
    [SerializeField] private Sprite mostDopeSprite;
    [SerializeField] private Sprite myTeamSprite;
    [SerializeField] private Sprite myHomieSprite;
    [SerializeField] private Sprite myDudeSprite;
    [SerializeField] private Sprite dopeSprite;
    [SerializeField] private Sprite weatherSprite;
    [SerializeField] private Sprite notBadSprite;
    [SerializeField] private Sprite loserSprite;

    [Header("Score Thresholds (original tiers from SWF)")]
    [SerializeField] private int mostDopeThresh = 5000;
    [SerializeField] private int myTeamThresh = 3500;
    [SerializeField] private int myHomieThresh = 2500;
    [SerializeField] private int myDudeThresh = 1500;
    [SerializeField] private int dopeThresh = 1000;
    [SerializeField] private int weatherThresh = 500;
    [SerializeField] private int notBadThresh = 200;

    [Header("Prize System")]
    [SerializeField] private int prizeThreshold = 1500;

    private Sprite backgroundSprite;
    private Sprite restartSprite;
    private Sprite prizeOverSprite;
    private Sprite prizeClaimSprite;

    private const string BestScoreKey = "BestScore";

    private void Awake()
    {
        LoadTierSprites();
        LoadUISprites();
    }

    private void LoadTierSprites()
    {
        if (mostDopeSprite == null) mostDopeSprite = LoadSprite("Tiers/MOST_DOPE");
        if (myTeamSprite == null) myTeamSprite = LoadSprite("Tiers/MYTEAM");
        if (myHomieSprite == null) myHomieSprite = LoadSprite("Tiers/MYHOMIE");
        if (myDudeSprite == null) myDudeSprite = LoadSprite("Tiers/MYDUDE");
        if (dopeSprite == null) dopeSprite = LoadSprite("Tiers/DOPE");
        if (weatherSprite == null) weatherSprite = LoadSprite("Tiers/UNDERTHEWEATHER");
        if (notBadSprite == null) notBadSprite = LoadSprite("Tiers/NOT_BAD");
        if (loserSprite == null) loserSprite = LoadSprite("Tiers/LOSER");
    }

    private void LoadUISprites()
    {
        backgroundSprite = LoadSprite("UI/GAME_OVER_002");
        restartSprite = LoadSprite("UI/PRESS_ANY_BUTTON");
        prizeOverSprite = LoadSprite("UI/over_1500");
        prizeClaimSprite = LoadSprite("UI/claimprize");
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

    public void ShowDeathScreen(int score)
    {
        if (deathPanel == null) return;

        deathPanel.SetActive(true);

        // Set background to original GAME_OVER_002
        if (backgroundImage != null && backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.color = Color.white;
        }

        // Update best score (stored in PlayerPrefs like original)
        int bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        // Set score texts
        if (finalScoreText != null)
            finalScoreText.text = score.ToString();

        if (bestScoreText != null)
            bestScoreText.text = bestScore.ToString();

        // Determine tier
        string tierName;
        Sprite tierSprite;
        GetTier(score, out tierName, out tierSprite);

        if (tierTitleText != null)
            tierTitleText.text = tierName;

        if (tierImage != null && tierSprite != null)
        {
            tierImage.sprite = tierSprite;
            tierImage.enabled = true;
            tierImage.preserveAspect = true;
        }

        // Show restart image (original PRESS_ANY_BUTTON texture)
        if (restartImage != null && restartSprite != null)
        {
            restartImage.sprite = restartSprite;
            restartImage.enabled = true;
            restartImage.preserveAspect = true;
        }

        // Show prize message if applicable
        if (prizeMessageImage != null)
        {
            if (WonPrize(score) && prizeClaimSprite != null)
            {
                prizeMessageImage.sprite = prizeClaimSprite;
                prizeMessageImage.enabled = true;
                prizeMessageImage.preserveAspect = true;
            }
            else if (prizeOverSprite != null)
            {
                prizeMessageImage.sprite = prizeOverSprite;
                prizeMessageImage.enabled = true;
                prizeMessageImage.preserveAspect = true;
            }
        }
    }

    /// <summary>
    /// Original DeadMenu.Start uses strictly greater-than (>) for all tier checks:
    ///   if (score > mostdopeThresh) -> mostdopeTex
    ///   else if (score > myteamThresh) -> myteamTex
    ///   ...etc
    ///   else -> loserTex
    /// </summary>
    private void GetTier(int score, out string name, out Sprite sprite)
    {
        if (score > mostDopeThresh)
        {
            name = "MOST DOPE!";
            sprite = mostDopeSprite;
        }
        else if (score > myTeamThresh)
        {
            name = "MY TEAM!";
            sprite = myTeamSprite;
        }
        else if (score > myHomieThresh)
        {
            name = "MY HOMIE!";
            sprite = myHomieSprite;
        }
        else if (score > myDudeThresh)
        {
            name = "MY DUDE!";
            sprite = myDudeSprite;
        }
        else if (score > dopeThresh)
        {
            name = "DOPE!";
            sprite = dopeSprite;
        }
        else if (score > weatherThresh)
        {
            name = "UNDER THE WEATHER";
            sprite = weatherSprite;
        }
        else if (score > notBadThresh)
        {
            name = "NOT BAD!";
            sprite = notBadSprite;
        }
        else
        {
            name = "LOSER!";
            sprite = loserSprite;
        }
    }

    public bool WonPrize(int score)
    {
        return score >= prizeThreshold;
    }

    public int GetBestScore()
    {
        return PlayerPrefs.GetInt(BestScoreKey, 0);
    }
}
