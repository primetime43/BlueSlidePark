using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Player controller matching original SliderMovement class from SWF.
/// Decompiled source: SliderMovement.as (cil2as ActionScript3 from Flash SWF).
///
/// Original movement used RotateAround(Offset, forward, RotationAmount) in FixedUpdate.
/// RotationAmount = (settle * settleFactor + input) * RotationSpeed * deltaTime.
/// This version converts that angular rotation to linear velocity on the curved slide.
/// The physics capsule stays upright (frozen rotation); visual lean goes on child model.
/// The curved MeshCollider on the slide makes the player ride up the walls naturally.
/// Death when player goes past slide edge (original: eulerAngles.z exceeds deathAngle).
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
    private Transform visualModel;
    private bool boostRequested;

    private void Awake()
    {
        isGrounded = false;
        isDead = false;
        startingPos = transform.position;
        startTime = Time.time;
        rb = GetComponent<Rigidbody>();
        if (gameCon != null)
            gameScript = gameCon.GetComponent<WorldMover>();

        // Keep physics capsule upright — visual tilt goes on child model only
        rb.freezeRotation = true;
        // Prevent Z drift (world scrolls past the player, player doesn't move forward)
        rb.constraints |= RigidbodyConstraints.FreezePositionZ;
    }

    private void Start()
    {
        if (canvasObj != null)
            inGameUI = canvasObj.GetComponent<InGameUI>();

        // Find visual model child (created by OriginalAssetLoader at runtime)
        Transform macModel = transform.Find("MacModel");
        if (macModel != null)
            visualModel = macModel;
    }

    public void ResetCharacter()
    {
        isDead = false;
        rb.isKinematic = false;
        rb.freezeRotation = true;
        transform.position = startingPos;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector3.zero;

        if (gameScript != null)
        {
            gameScript.enabled = true;
            gameScript.ResetWorld();
        }

        if (SlideController.Instance != null)
            SlideController.Instance.StartGame();

        isGrounded = true;
        lean = 0f;
        dampedRotZ = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
            isGrounded = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
            isGrounded = true;
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.CompareTag("Killbox"))
            ResetCharacter();
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

        if (SlideController.Instance != null)
            SlideController.Instance.StopGame();

        if (gameScript != null)
            gameScript.enabled = false;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopSlideNoise();
            SoundManager.Instance.PlayDeath();
        }

        int finalScore = 0;
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.StopScoring();
            finalScore = ScoreManager.Instance.GetScore();

            if (LeaderboardManager.Instance != null)
                LeaderboardManager.Instance.SubmitScore(finalScore, SystemInfo.deviceUniqueIdentifier);
        }

        if (deadMenuManager != null)
            deadMenuManager.ShowDeathScreen(finalScore);

        if (LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.FetchLeaderboard(finalScore, GetPlayerName());
    }

    private void CollectPickup(GameObject pickup)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddBonus(100);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPickup();

        var effect = pickup.GetComponent<PickupEffect>();
        if (effect != null)
            effect.PlayEffect();

        var spinner = pickup.GetComponent<PickupSpinner>();
        if (spinner != null)
            spinner.ShowPickup();

        bool isThumbsUp = pickup.name.StartsWith("ThumbsUp");
        if (isThumbsUp && inGameUI != null)
            inGameUI.CallAnimator("ThumbUp");

        var col = pickup.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void Update()
    {
        if (isDead)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        // Capture boost input in Update (more reliable for key presses than FixedUpdate)
        if (Input.GetKeyDown(KeyCode.UpArrow))
            boostRequested = true;

        // Apply visual lean rotation to child model only (physics capsule stays upright)
        if (visualModel != null)
        {
            Vector3 rot = visualModel.localEulerAngles;
            rot.z = -dampedRotZ;
            visualModel.localEulerAngles = rot;
        }

        // Edge death: player went past slide boundary
        if (isGrounded && !isDead && Mathf.Abs(transform.position.x) > slideHalfWidth)
        {
            if (!immortal)
                Die();
        }
    }

    /// <summary>
    /// Physics movement in FixedUpdate matching original SliderMovement.FixedUpdate.
    /// Original used RotateAround(Offset, forward, RotationAmount).
    /// We convert that to lateral velocity — the curved MeshCollider makes the
    /// player naturally ride up the slide walls when pushed sideways.
    /// </summary>
    private void FixedUpdate()
    {
        if (isDead || !isGrounded) return;

        // Boost (captured from Update)
        if (boostRequested)
        {
            timeTilBoostEnd = Time.timeSinceLevelLoad + boostDuration;
            boostRequested = false;
        }

        // Original speed formula: min(10, 6 + (time - startTime) / 10)
        float targetSpeed = Mathf.Min(10f, 6f + (Time.time - startTime) / 10f);
        if (timeTilBoostEnd > Time.timeSinceLevelLoad)
            speed = Mathf.Lerp(speed, targetSpeed + boostAmount, speedDampLerp * Time.fixedDeltaTime);
        else
            speed = Mathf.Lerp(speed, targetSpeed, speedDampLerp * Time.fixedDeltaTime);

        float input = Input.GetAxis("Horizontal");

        // Original: lean = Lerp(lean, GetAxis("Horizontal"), leanLerp * deltaTime)
        lean = Mathf.Lerp(lean, input, leanLerp * Time.fixedDeltaTime);

        // dampedRotZ tracks visual lean angle (used for settle + visual tilt)
        dampedRotZ = Mathf.Lerp(dampedRotZ, lean * deathAngle, dampLerp * Time.fixedDeltaTime);

        // Settle calculation (original formula from SliderMovement.as lines 206-213)
        float normZ = ((dampedRotZ % 360f) + 360f) % 360f;
        float settle = 0f;
        if (normZ < 180f)
            settle = -normZ / 180f;
        else
            settle = 1f - (normZ - 180f) / 180f;

        // Original: RotationAmount = (settle * settleFactor + input) * RotationSpeed * dt
        // Converted to lateral velocity for our physics-based slide
        float lateralSpeed = (settle * settleFactor + input) * rotationSpeed;

        // Set X velocity directly (gravity handles Y, Z is frozen)
        Vector3 vel = rb.linearVelocity;
        vel.x = lateralSpeed;
        rb.linearVelocity = vel;
    }

    private string GetPlayerName()
    {
        return PlayerPrefs.GetString("PlayerName", "Player");
    }
}
