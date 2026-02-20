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
/// </summary>
public class DeadMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private Text tierTitleText;
    [SerializeField] private Text retryText;
    [SerializeField] private Image tierImage;

    [Header("Tier Textures (highest to lowest)")]
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

    private const string BestScoreKey = "BestScore";

    public void ShowDeathScreen(int score)
    {
        if (deathPanel == null) return;

        deathPanel.SetActive(true);

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
            bestScoreText.text = "Best: " + bestScore.ToString();

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
        }

        if (retryText != null)
            retryText.text = "Press Space to Retry";
    }

    /// <summary>
    /// Original DeadMenu.Start uses strictly greater-than (>) for all tier checks:
    ///   if (score > mostdopeThresh) → mostdopeTex
    ///   else if (score > myteamThresh) → myteamTex
    ///   ...etc
    ///   else → loserTex
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
            name = "WEATHER!";
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
