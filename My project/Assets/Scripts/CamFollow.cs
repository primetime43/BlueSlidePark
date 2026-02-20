using UnityEngine;

/// <summary>
/// Camera follow controller matching original CamFollow class from SWF.
/// Decompiled source: CamFollow.as
///
/// Original default values from decompiled code:
///   distance = 10, height = 5,
///   positionDampTime = 0.3, rotationDampTime = 0.3,
///   rotationZDampTime = 0.3, rotationXDampTime = 0.3,
///   lerpXfactor = 0, lerpYfactor = 0, lerpZfactor = 0,
///   damp = 2
///
/// Original LateUpdate behavior:
///   - Finds "PlayerObj" by name if target is null
///   - Position: target.TransformPoint(0, height, -distanceFromSpeed.Evaluate(Speed))
///   - Rotation: LookRotation toward target + height offset, then per-axis LerpAngle
///     using lerpXfactor/Y/Z * deltaTime
///
/// distanceFromSpeed and tiltUp are AnimationCurves in original.
/// </summary>
public class CamFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Position (from original defaults)")]
    [SerializeField] private float distance = 10f;
    [SerializeField] private float height = 5f;
    [SerializeField] private float positionDampTime = 0.3f;
    [SerializeField] private float lerpXfactor = 5f;
    [SerializeField] private float lerpYfactor = 2f;
    [SerializeField] private float lerpZfactor = 0f;

    [Header("Rotation")]
    [SerializeField] private float rotationDampTime = 0.3f;
    [SerializeField] private float rotationZDampTime = 0.3f;
    [SerializeField] private float rotationXDampTime = 0.3f;
    [SerializeField] private float damp = 2f;
    [SerializeField] private float tiltUp = 5f;

    private Vector3 offset;
    private Quaternion startRot;
    private Vector3 dampVelocity;
    private float dampRotZVelocity;
    private float dampRotXVelocity;
    private float dampRotVelocity;

    private void Awake()
    {
        // Original: startRot = transform.rotation
        startRot = transform.rotation;

        if (target != null)
            offset = transform.position - target.position;
    }

    /// <summary>
    /// Matches original CamFollow_ResetCamera:
    /// Reset all velocities, damp=2, rotation = startRot.
    /// </summary>
    public void ResetCamera()
    {
        dampVelocity = Vector3.zero;
        dampRotZVelocity = 0f;
        dampRotXVelocity = 0f;
        dampRotVelocity = 0f;
        damp = 2f;
        transform.rotation = startRot;
    }

    private void LateUpdate()
    {
        // Original: find "PlayerObj" by name if target is null
        if (target == null)
        {
            var playerObj = GameObject.Find("PlayerObj");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (target == null) return;

        // Position follow with per-axis lerp factors
        Vector3 targetPos = target.position + offset;
        Vector3 currentPos = transform.position;

        Vector3 desiredPos = new Vector3(
            Mathf.Lerp(currentPos.x, targetPos.x, lerpXfactor * Time.deltaTime),
            Mathf.Lerp(currentPos.y, targetPos.y, lerpYfactor * Time.deltaTime),
            targetPos.z + offset.z
        );

        transform.position = Vector3.SmoothDamp(
            currentPos, desiredPos, ref dampVelocity, positionDampTime);

        // Rotation - slight tilt based on player X movement
        float targetTiltZ = Mathf.SmoothDampAngle(
            transform.eulerAngles.z,
            -target.eulerAngles.z * 0.3f,
            ref dampRotZVelocity,
            rotationZDampTime);

        float targetTiltX = Mathf.SmoothDampAngle(
            transform.eulerAngles.x,
            startRot.eulerAngles.x + tiltUp,
            ref dampRotXVelocity,
            rotationXDampTime);

        transform.eulerAngles = new Vector3(targetTiltX, startRot.eulerAngles.y, targetTiltZ);
    }
}
