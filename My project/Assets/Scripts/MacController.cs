using UnityEngine;

public class MacController : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float slideHalfWidth = 5f;
    [SerializeField] private GameObject gameCon;

    private WorldMover gameScript;
    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 startingPos;

    private void Awake()
    {
        isGrounded = false;
        startingPos = transform.position;
        rb = GetComponent<Rigidbody>();
        if (gameCon != null)
            gameScript = gameCon.GetComponent<WorldMover>();
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

    // Hits obstacle (ice cream, poop, etc.)
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            ResetCharacter();
        }
    }

    private void Update()
    {
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
            // Kill sideways velocity so they don't keep pushing against the wall
            Vector3 vel = rb.linearVelocity;
            vel.x = 0f;
            rb.linearVelocity = vel;
        }
    }
}
