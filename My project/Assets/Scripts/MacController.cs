using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Player controller matching original SliderMovement class from SWF.
/// Original fields: Dead, settle, deathAngle, danceAngle, dampedRotZ,
/// dampLerp, speedDampLerp, lean, leanLerp, timeTilBoostEnd,
/// settleFactor, startTime, immortal, pickupSound, deathSound,
/// pickupVol, slideNoise, model, Speed.
/// </summary>
public class MacController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float slideHalfWidth = 5f;

    [Header("Original SliderMovement Fields")]
    [SerializeField] private float lean;
    [SerializeField] private float leanLerp = 5f;
    [SerializeField] private float settleFactor = 3f;
    [SerializeField] private float dampLerp = 5f;
    [SerializeField] private float deathAngle = 45f;

    [Header("Debug (original had immortal flag)")]
    [SerializeField] private bool immortal;

    [Header("References")]
    [SerializeField] private GameObject gameCon;
    [SerializeField] private DeadMenuManager deadMenuManager;
    [SerializeField] private GameObject canvasObj;

    private WorldMover gameScript;
    private Rigidbody rb;
    private bool isGrounded;
    private bool isDead;
    private Vector3 startingPos;
    private InGameUI inGameUI;
    private float dampedRotZ;

    private void Awake()
    {
        isGrounded = false;
        isDead = false;
        startingPos = transform.position;
        rb = GetComponent<Rigidbody>();
        if (gameCon != null)
            gameScript = gameCon.GetComponent<WorldMover>();
    }

    private void Start()
    {
        if (canvasObj != null)
            inGameUI = canvasObj.GetComponent<InGameUI>();
    }

    public void ResetCharacter()
    {
        transform.position = startingPos;
        gameScript.ResetWorld();
        rb.linearVelocity = Vector3.zero;
        isGrounded = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            isGrounded = true;
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.CompareTag("Killbox"))
        {
            ResetCharacter();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.gameObject.CompareTag("Obstacle"))
        {
            if (!immortal)
                Die();
        }
        else if (other.gameObject.CompareTag("Pickup"))
        {
            CollectPickup(other.gameObject);
        }
    }

    private void Die()
    {
        isDead = true;

        // Stop world movement
        if (gameScript != null)
            gameScript.enabled = false;

        // Freeze player
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Stop scoring
        int finalScore = 0;
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.StopScoring();
            finalScore = ScoreManager.Instance.GetScore();

            // Upload score (original: ScoreManager.UploadScore -> ExternalCall.PostScore)
            if (LeaderboardManager.Instance != null)
                LeaderboardManager.Instance.SubmitScore(finalScore, SystemInfo.deviceUniqueIdentifier);
        }

        // Play death sound
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayDeath();

        // Show death panel with tier system
        if (deadMenuManager != null)
            deadMenuManager.ShowDeathScreen(finalScore);

        // Fetch leaderboard (original: Leaderboard.Start -> FetchLeaderboard coroutine)
        if (LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.FetchLeaderboard(finalScore, GetPlayerName());
    }

    private void CollectPickup(GameObject pickup)
    {
        // Determine point value based on name
        bool isThumbsUp = pickup.name.StartsWith("ThumbsUp");
        int points = isThumbsUp ? 50 : 25;

        // Add score
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddBonus(points);

        // Play pickup sound
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPickup();

        // Play particle effect
        var effect = pickup.GetComponent<PickupEffect>();
        if (effect != null)
            effect.PlayEffect();

        // Trigger fly-up animation (original VictoryBall.ShowPickup)
        var spinner = pickup.GetComponent<PickupSpinner>();
        if (spinner != null)
            spinner.ShowPickup();

        // Trigger thumbs up animation
        if (isThumbsUp && inGameUI != null)
            inGameUI.CallAnimator("ThumbUp");

        // Disable collider so it can't be collected again
        var col = pickup.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void Update()
    {
        if (isDead)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (isGrounded)
        {
            float input = 0f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                input = 1f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                input = -1f;

            rb.AddForce(Vector3.right * input * speed, ForceMode.Impulse);

            // Lean towards movement direction (original dampedRotZ/lean mechanic)
            lean = Mathf.Lerp(lean, input * deathAngle * 0.3f, leanLerp * Time.deltaTime);
        }

        // Apply lean rotation (original SliderMovement damped Z rotation)
        dampedRotZ = Mathf.Lerp(dampedRotZ, lean, dampLerp * Time.deltaTime);
        Vector3 rot = transform.eulerAngles;
        rot.z = -dampedRotZ;
        transform.eulerAngles = rot;

        // Settle towards center (original settleFactor)
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x) > 0.1f && isGrounded)
        {
            pos.x = Mathf.Lerp(pos.x, 0f, settleFactor * Time.deltaTime * 0.1f);
        }

        // Clamp player to slide boundaries
        if (pos.x < -slideHalfWidth || pos.x > slideHalfWidth)
        {
            pos.x = Mathf.Clamp(pos.x, -slideHalfWidth, slideHalfWidth);
            Vector3 vel = rb.linearVelocity;
            vel.x = 0f;
            rb.linearVelocity = vel;
        }
        transform.position = pos;
    }

    private string GetPlayerName()
    {
        return PlayerPrefs.GetString("PlayerName", "Player");
    }
}
