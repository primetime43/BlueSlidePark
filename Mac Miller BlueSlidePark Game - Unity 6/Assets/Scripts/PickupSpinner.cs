using UnityEngine;

/// <summary>
/// Replaces the simple spinner with the original VictoryBall behavior from the SWF.
/// Decompiled source: VictoryBall.as
///
/// Original behavior:
///   Start: model = transform.Find("ICE_CREAM")
///   Update: model.Rotate(0, rotSpeed, 0); if flyUp: Translate(0, flyUpSpeed, 0, Self)
///   OnTriggerEnter: if other.name == "PlayerObj" → GetVictoryBall, flyUp=true,
///                   ShowPickup(), Invoke("Die", 5)
///   ShowPickup: instantiate pickupParticle at pickupPos, parent to UICamera
///   Die: Destroy(gameObject)
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
            // Original: transform.Translate(0, flyUpSpeed, 0, Space.Self)
            transform.Translate(Vector3.up * flyUpSpeed * Time.deltaTime, Space.Self);
            transform.Rotate(Vector3.up, rotSpeed * 3f * Time.deltaTime, Space.World);
        }
        else
        {
            // Original: model.Rotate(0, rotSpeed, 0)
            transform.Rotate(Vector3.up, rotSpeed * Time.deltaTime, Space.World);
            Vector3 pos = startPos;
            pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = pos;
        }
    }

    /// <summary>
    /// Called on pickup collection. Plays a brief fly-up then hides.
    /// Stays parented so WorldMover can recycle it.
    /// </summary>
    public void ShowPickup()
    {
        flyUp = true;
        Invoke(nameof(HideAfterCollection), 0.5f);
    }

    private void HideAfterCollection()
    {
        flyUp = false;
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
    }

    /// <summary>
    /// Called by WorldMover when recycling this pickup to a new position.
    /// </summary>
    public void ResetForReuse()
    {
        CancelInvoke();
        flyUp = false;
        startPos = transform.localPosition;
    }
}
