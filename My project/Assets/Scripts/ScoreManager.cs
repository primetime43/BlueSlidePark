using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Score tracking matching original ScoreManager from SWF.
/// Original fields: score, bestScore, userID.
/// Original methods: OnEnable, SubmitScore, GameOver, Update, SetScore, UploadScore.
/// Best score persisted in PlayerPrefs (original used PlayerPrefs too).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private float distanceMultiplier = 15f;

    private Text scoreText;
    private float distanceScore;
    private int bonusScore;
    private bool scoring = true;

    private const string BestScoreKey = "BestScore";

    public int BestScore
    {
        get { return PlayerPrefs.GetInt(BestScoreKey, 0); }
        private set
        {
            PlayerPrefs.SetInt(BestScoreKey, value);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find the score number text (child of ScoreTxt)
        GameObject scoreTxtObj = GameObject.Find("ScoreTxt");
        if (scoreTxtObj != null && scoreTxtObj.transform.childCount > 0)
            scoreText = scoreTxtObj.transform.GetChild(0).GetComponent<Text>();
    }

    private void Update()
    {
        if (!scoring) return;

        distanceScore += distanceMultiplier * Time.deltaTime;
        UpdateDisplay();
    }

    public void AddBonus(int points)
    {
        bonusScore += points;
        UpdateDisplay();
    }

    /// <summary>
    /// Matches original ScoreManager.GameOver() - stops scoring and updates best.
    /// </summary>
    public void StopScoring()
    {
        scoring = false;

        int current = GetScore();
        if (current > BestScore)
            BestScore = current;
    }

    public int GetScore()
    {
        return Mathf.FloorToInt(distanceScore) + bonusScore;
    }

    private void UpdateDisplay()
    {
        if (scoreText != null)
            scoreText.text = GetScore().ToString("D4");
    }
}
