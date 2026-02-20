using UnityEngine;

/// <summary>
/// Replaces the simple spinner with the original VictoryBall behavior from the SWF.
/// Original fields: rotSpeed, flyUpSpeed, pickupPos, flyUp, model.
/// Spins and bobs while idle, flies upward when collected (ShowPickup/Die).
/// </summary>
public class PickupSpinner : MonoBehaviour
{
    [Header("Idle Animation")]
    [SerializeField] private float rotSpeed = 90f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.15f;

    [Header("Collection Animation (original VictoryBall)")]
    [SerializeField] private float flyUpSpeed = 8f;

    private Vector3 startPos;
    private bool flyUp;

    private void Start()
    {
        startPos = transform.localPosition;
    }

    private void Update()
    {
        if (flyUp)
        {
            // Original VictoryBall fly-up behavior
            transform.Translate(Vector3.up * flyUpSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.up, rotSpeed * 3f * Time.deltaTime, Space.World);

            // Self-destruct after flying high enough
            if (transform.position.y > 20f)
                Destroy(gameObject);
        }
        else
        {
            // Idle spin and bob
            transform.Rotate(Vector3.up, rotSpeed * Time.deltaTime, Space.World);
            Vector3 pos = startPos;
            pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = pos;
        }
    }

    /// <summary>
    /// Called on pickup collection. Matches original VictoryBall.ShowPickup().
    /// Detaches from parent and flies upward.
    /// </summary>
    public void ShowPickup()
    {
        flyUp = true;
        // Detach so it doesn't scroll with the world
        transform.SetParent(null);
    }
}
