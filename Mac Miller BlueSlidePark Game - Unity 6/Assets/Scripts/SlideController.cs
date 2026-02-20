using UnityEngine;

/// <summary>
/// Slide management matching original SlideController class from SWF.
/// Decompiled source: SlideController.as
///
/// Original default values from decompiled code:
///   StartingPieces = 14, PiecesAtOnce = 30, Offset = 30,
///   TurnCooldown = 10, TurnDirectionCount = 10,
///   rotation = 0, rotationchange = 0, isLeft = false,
///   CullMap = false, running = true, testAnims = false.
///
/// Original scoring: ScoreManager.SetScore(PieceNumber - StartingPieces + bonusScore)
///
/// Original spawning:
///   Trees: 1/6 chance, placed 30-100 units left or right, +10 up
///   Obstacles: 1/10 chance, rotated -60 to 60 degrees
///   VictoryBalls: 1/6 chance (when no obstacle)
///   CullMap: destroys oldest when childCount > PiecesAtOnce
///
/// Turn direction: TurnDirectionCount resets to Random(20,30),
///   isLeft = Random(1,3)==1 (1/3 chance left), TurnCooldown = 10
/// </summary>
public class SlideController : MonoBehaviour
{
    public static SlideController Instance { get; private set; }

    [Header("Slide Pieces (from original decompiled values)")]
    [SerializeField] private int startingPieces = 14;
    [SerializeField] private int piecesAtOnce = 30;
    [SerializeField] private float offset = 30f;

    [Header("Colors (original OddColour/EvenColour)")]
    [SerializeField] private Color oddColour = new Color(0.2f, 0.5f, 0.8f);
    [SerializeField] private Color evenColour = new Color(0.3f, 0.6f, 0.9f);

    [Header("Turn Mechanics (from original)")]
    [SerializeField] private int turnCooldown = 10;
    [SerializeField] private int turnDirectionCount = 10;
    [SerializeField] private float rotationchange;

    [Header("Obstacle/Pickup Spawn (from original)")]
    [SerializeField] private Vector3 victoryBallOffset;
    [SerializeField] private int obstacleChance = 10;
    [SerializeField] private int victoryBallChance = 6;
    [SerializeField] private int treeChance = 6;

    [Header("State")]
    [SerializeField] private bool running = true;
    [SerializeField] private bool cullMap;
    [SerializeField] private bool testAnims;

    private int pieceNumber;
    private int bonusScore;
    private bool isLeft;
    private float rotation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Properties
    public int PieceNumber => pieceNumber;
    public int StartingPieces => startingPieces;
    public int PiecesAtOnce => piecesAtOnce;
    public int BonusScore { get => bonusScore; set => bonusScore = value; }
    public Color OddColour => oddColour;
    public Color EvenColour => evenColour;
    public bool Running => running;
    public float VictoryBallOffsetY => victoryBallOffset.y;
    public float Offset => offset;

    public void StartGame()
    {
        running = true;
    }

    public void StopGame()
    {
        running = false;
    }

    /// <summary>
    /// Matches original SlideController_Restart:
    /// Destroy self, destroy player, instantiate from Resources.
    /// In our recreation, we just reset state.
    /// </summary>
    public void Restart()
    {
        pieceNumber = 0;
        bonusScore = 0;
        running = true;
    }

    /// <summary>
    /// Matches original SlideController_CreateNextPiece.
    /// Called by WorldMover when recycling segments.
    /// Original: ScoreManager.SetScore(PieceNumber - StartingPieces + bonusScore)
    /// </summary>
    public void OnPieceCreated()
    {
        pieceNumber++;

        // Original scoring formula from decompiled code
        if (ScoreManager.Instance != null)
        {
            int score = pieceNumber - startingPieces + bonusScore;
            if (score < 0) score = 0;
            ScoreManager.Instance.SetScore(score);
        }
    }

    /// <summary>
    /// Should a tree spawn on this piece? Original: Random.Range(1,6) == 1 (1/6 chance)
    /// </summary>
    public bool ShouldSpawnTree()
    {
        return !testAnims && Random.Range(1, treeChance + 1) == 1;
    }

    /// <summary>
    /// Should an obstacle spawn? Original: Random.Range(0,10) == 1 (1/10 chance)
    /// </summary>
    public bool ShouldSpawnObstacle()
    {
        return !testAnims && Random.Range(0, obstacleChance) == 1;
    }

    /// <summary>
    /// Should a victory ball spawn? Original: Random.Range(0,6) == 0 (1/6 chance)
    /// </summary>
    public bool ShouldSpawnVictoryBall()
    {
        return !testAnims && Random.Range(0, victoryBallChance) == 0;
    }
}
