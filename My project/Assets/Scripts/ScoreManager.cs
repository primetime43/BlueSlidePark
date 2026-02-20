using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Score tracking matching original ScoreManager from SWF.
/// Decompiled source: ScoreManager.as
///
/// Original fields: score, bestScore, playerName, userID.
/// Original methods: OnEnable (singleton + load best), SetScore(int),
///   GameOver, SubmitScore, UploadScore.
///
/// Scoring: Score is SET by SlideController.CreateNextPiece:
///   score = PieceNumber - StartingPieces + bonusScore
/// Best score persisted in PlayerPrefs "BestScore".
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private float distanceMultiplier = 15f;

    private Text scoreText;
    private int score;
    private int bestScore;
    private bool scoring = true;
    private float distanceAccumulator;

    private const string BestScoreKey = "BestScore";

    public int BestScore
    {
        get { return bestScore; }
    }

    private void Awake()
    {
        // Original: OnEnable singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate ScoreManagers");
            Destroy(this);
            return;
        }
        Instance = this;

        // Original: if PlayerPrefs.GetInt("BestScore") > bestScore, load it
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    private void Start()
    {
        GameObject scoreTxtObj = GameObject.Find("ScoreTxt");
        if (scoreTxtObj != null && scoreTxtObj.transform.childCount > 0)
            scoreText = scoreTxtObj.transform.GetChild(0).GetComponent<Text>();
    }

    private void Update()
    {
        if (!scoring) return;

        // Distance-based scoring as fallback (original used PieceNumber-based)
        distanceAccumulator += distanceMultiplier * Time.deltaTime;
        UpdateDisplay();
    }

    /// <summary>
    /// Original: ScoreManager.SetScore(int) — called by SlideController.CreateNextPiece.
    /// score = PieceNumber - StartingPieces + bonusScore
    /// Also updates best score in PlayerPrefs.
    /// </summary>
    public void SetScore(int newScore)
    {
        score = newScore;
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
        }
        UpdateDisplay();
    }

    /// <summary>
    /// Original: bonusScore was on SlideController, incremented by 100 per pickup.
    /// This adds to the bonus which gets factored into the score formula.
    /// </summary>
    public void AddBonus(int points)
    {
        if (SlideController.Instance != null)
        {
            SlideController.Instance.BonusScore += points;
        }
        // Also update score immediately for display
        score += points;
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
        }
        UpdateDisplay();
    }

    /// <summary>
    /// Matches original ScoreManager.GameOver() — stops scoring.
    /// </summary>
    public void StopScoring()
    {
        scoring = false;
    }

    public int GetScore()
    {
        return score;
    }

    private void UpdateDisplay()
    {
        if (scoreText != null)
            scoreText.text = score.ToString("D4");
    }
}
