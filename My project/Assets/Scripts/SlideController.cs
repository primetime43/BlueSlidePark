using UnityEngine;

/// <summary>
/// Slide management matching original SlideController class from SWF.
/// Original fields: PieceNumber, Pieces, PieceLeft, PieceRight, treePrefabs,
/// LastPosition, LastPiece, rotation, rotationchange, TurnDirectionCount,
/// TurnCooldown, LastPos, Offset, isLeft, LastTransform, Player,
/// OddColour, EvenColour, bonusScore, StartingPieces, PiecesAtOnce,
/// Obsticle, victoryBall, victoryBallOffset, player, CullMap,
/// SlideControllerPrefab, startPos, startRot, camStartPos, camStartRot,
/// playerStartPos, running, testAnims.
///
/// This singleton provides access to slide configuration values that other
/// scripts (WorldMover, MacController, etc.) need.
/// </summary>
public class SlideController : MonoBehaviour
{
    public static SlideController Instance { get; private set; }

    [Header("Slide Configuration (from original SWF)")]
    [SerializeField] private int startingPieces = 7;
    [SerializeField] private int piecesAtOnce = 7;
    [SerializeField] private int bonusScore = 25;

    [Header("Colors (original OddColour/EvenColour)")]
    [SerializeField] private Color oddColour = new Color(0.2f, 0.5f, 0.8f);
    [SerializeField] private Color evenColour = new Color(0.3f, 0.6f, 0.9f);

    [Header("Turn Mechanics")]
    [SerializeField] private float turnCooldown = 0.5f;
    [SerializeField] private int turnDirectionCount;

    [Header("Obstacle/Pickup Spawn")]
    [SerializeField] private float victoryBallOffset = 0.5f;

    [Header("State")]
    [SerializeField] private bool running;

    private int pieceNumber;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int PieceNumber => pieceNumber;
    public int BonusScore => bonusScore;
    public Color OddColour => oddColour;
    public Color EvenColour => evenColour;
    public bool Running => running;
    public float VictoryBallOffset => victoryBallOffset;

    public void StartGame()
    {
        running = true;
    }

    public void StopGame()
    {
        running = false;
    }

    /// <summary>
    /// Matches original SlideController_Restart.
    /// </summary>
    public void Restart()
    {
        pieceNumber = 0;
        running = true;
    }

    /// <summary>
    /// Matches original SlideController_CreateNextPiece.
    /// Called by WorldMover when recycling segments.
    /// </summary>
    public void OnPieceCreated()
    {
        pieceNumber++;
    }
}
