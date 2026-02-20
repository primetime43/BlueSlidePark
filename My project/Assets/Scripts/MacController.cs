using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Player controller matching original SliderMovement class from SWF.
/// Decompiled source: SliderMovement.as (cil2as ActionScript3 from Flash SWF).
///
/// Original default values from decompiled code:
///   Speed = 3 (static), RotationOrigin = (0,0,10), RotationSpeed = 20,
///   CutOffHeight = 4, dampLerp = 0.3, speedDampLerp = 0.3, leanLerp = 0.2,
///   settle = 0, deathAngle = 0 (set in scene), danceAngle = 0 (set in scene).
///
/// Movement: Speed ramps from min(10, 6 + (time-startTime)/10).
/// UpArrow gives 0.3s boost of +10 speed.
/// Player rotates around pivot (RotateAround), settle factor pushes back to center.
/// Death when euler Z exceeds deathAngle; "stupiddance" anim near danceAngle.
/// VictoryBall pickup: bonusScore += 100.
/// </summary>
public class MacController : MonoBehaviour
{
    [Header("Movement (from original SliderMovement)")]
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float slideHalfWidth = 5f;

    [Header("Speed (original: static Speed = 3, ramps via formula)")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float speedDampLerp = 0.3f;
    [SerializeField] private float boostDuration = 0.3f;
    [SerializeField] private float boostAmount = 10f;

    [Header("Lean/Settle (from original decompiled values)")]
    [SerializeField] private float lean;
    [SerializeField] private float leanLerp = 0.2f;
    [SerializeField] private float settleFactor;
    [SerializeField] private float dampLerp = 0.3f;
    [SerializeField] private float deathAngle = 45f;
    [SerializeField] private float danceAngle = 30f;

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
    private float startTime;
    private float timeTilBoostEnd;

    private void Awake()
    {
        isGrounded = false;
        isDead = false;
        startingPos = transform.position;
        startTime = Time.time;
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
        if (isDead || immortal) return;
        isDead = true;

        // Original: SlideController.Instance.running = false
        if (SlideController.Instance != null)
            SlideController.Instance.StopGame();

        // Stop world movement
        if (gameScript != null)
            gameScript.enabled = false;

        // Freeze player
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Original: slideNoise.volume = 0
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopSlideNoise();
            SoundManager.Instance.PlayDeath();
        }

        // Original: ScoreManager.instance.UploadScore() -> SubmitScore -> ExternalCall.PostScore
        int finalScore = 0;
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.StopScoring();
            finalScore = ScoreManager.Instance.GetScore();

            if (LeaderboardManager.Instance != null)
                LeaderboardManager.Instance.SubmitScore(finalScore, SystemInfo.deviceUniqueIdentifier);
        }

        // Original: Object.Instantiate(DeathScreen) - shows death panel
        if (deadMenuManager != null)
            deadMenuManager.ShowDeathScreen(finalScore);

        // Original: Leaderboard.Start -> FetchLeaderboard coroutine
        if (LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.FetchLeaderboard(finalScore, GetPlayerName());
    }

    /// <summary>
    /// Original SliderMovement.GetVictoryBall: bonusScore += 100, play pickupSound.
    /// </summary>
    private void CollectPickup(GameObject pickup)
    {
        // Original: SlideController.Instance.bonusScore += 100
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddBonus(100);

        // Original: Camera.main.audio.PlayOneShot(pickupSound, pickupVol)
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPickup();

        // Play particle effect
        var effect = pickup.GetComponent<PickupEffect>();
        if (effect != null)
            effect.PlayEffect();

        // Trigger fly-up animation (original VictoryBall: flyUp = true, ShowPickup(), Invoke("Die", 5))
        var spinner = pickup.GetComponent<PickupSpinner>();
        if (spinner != null)
            spinner.ShowPickup();

        // Trigger thumbs up animation for thumbs up pickups
        bool isThumbsUp = pickup.name.StartsWith("ThumbsUp");
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
            // Original: DeadMenu.OnGUI checks Jump, Space, KeypadEnter, Return, R
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (isGrounded)
        {
            // Original: UpArrow boost - 0.3s of speed +10
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                timeTilBoostEnd = Time.timeSinceLevelLoad + boostDuration;
            }

            // Original speed formula: min(10, 6 + (Time.time - startTime) / 10)
            float targetSpeed = Mathf.Min(10f, 6f + (Time.time - startTime) / 10f);
            if (timeTilBoostEnd > Time.timeSinceLevelLoad)
            {
                speed = Mathf.Lerp(speed, targetSpeed + boostAmount, speedDampLerp * Time.deltaTime);
            }
            else
            {
                speed = Mathf.Lerp(speed, targetSpeed, speedDampLerp * Time.deltaTime);
            }

            float input = Input.GetAxis("Horizontal");

            rb.AddForce(Vector3.right * input * speed, ForceMode.Impulse);

            // Original lean: Lerp(lean, GetAxis("Horizontal"), leanLerp * deltaTime)
            lean = Mathf.Lerp(lean, input, leanLerp * Time.deltaTime);
        }

        // Original: dampedRotZ damped rotation based on slide direction
        dampedRotZ = Mathf.Lerp(dampedRotZ, lean * deathAngle, dampLerp * Time.deltaTime);
        Vector3 rot = transform.eulerAngles;
        rot.z = -dampedRotZ;
        transform.eulerAngles = rot;

        // Original: settle factor pushes player back towards center
        float settle = 0f;
        float normZ = (dampedRotZ % 360f + 360f) % 360f;
        if (normZ < 180f)
            settle = -normZ / 180f;
        else
            settle = 1f - (normZ - 180f) / 180f;

        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x) > 0.1f && isGrounded)
        {
            pos.x = Mathf.Lerp(pos.x, 0f, Mathf.Abs(settle) * settleFactor * Time.deltaTime);
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
