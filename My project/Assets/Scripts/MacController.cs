using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MacController : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float slideHalfWidth = 5f;
    [SerializeField] private GameObject gameCon;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private GameObject canvasObj;

    private WorldMover gameScript;
    private Rigidbody rb;
    private bool isGrounded;
    private bool isDead;
    private Vector3 startingPos;
    private InGameUI inGameUI;

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
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.StopScoring();

        // Play death sound
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayDeath();

        // Show death panel
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
            if (finalScoreText != null && ScoreManager.Instance != null)
                finalScoreText.text = "Final Score: " + ScoreManager.Instance.GetScore();
        }
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

        // Trigger thumbs up animation
        if (isThumbsUp && inGameUI != null)
            inGameUI.CallAnimator("ThumbUp");

        // Deactivate the pickup
        pickup.SetActive(false);
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
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                rb.AddForce(Vector3.right * speed, ForceMode.Impulse);
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                rb.AddForce(Vector3.left * speed, ForceMode.Impulse);
        }

        // Clamp player to slide boundaries
        Vector3 pos = transform.position;
        if (pos.x < -slideHalfWidth || pos.x > slideHalfWidth)
        {
            pos.x = Mathf.Clamp(pos.x, -slideHalfWidth, slideHalfWidth);
            transform.position = pos;
            Vector3 vel = rb.linearVelocity;
            vel.x = 0f;
            rb.linearVelocity = vel;
        }
    }
}
