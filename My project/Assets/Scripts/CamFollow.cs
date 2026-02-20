using UnityEngine;

/// <summary>
/// Camera follow controller matching original CamFollow class from SWF.
/// Original fields: target, positionDampTime, rotationDampTime,
/// rotationZDampTime, rotationXDampTime, lerpXfactor, lerpYfactor,
/// lerpZfactor, damp, tiltUp, startRot, dampRotVelocity,
/// dampRotZVelocity, dampRotXVelocity, dampVelocity, precorrectedRot.
/// Uses smooth damping for position and rotation following the player.
/// </summary>
public class CamFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Position Damping")]
    [SerializeField] private float positionDampTime = 0.3f;
    [SerializeField] private float lerpXfactor = 0.5f;
    [SerializeField] private float lerpYfactor = 0.2f;
    [SerializeField] private float lerpZfactor = 0f;

    [Header("Rotation Damping")]
    [SerializeField] private float rotationDampTime = 0.5f;
    [SerializeField] private float rotationZDampTime = 0.3f;
    [SerializeField] private float rotationXDampTime = 0.5f;
    [SerializeField] private float tiltUp = 5f;

    private Vector3 offset;
    private Quaternion startRot;
    private Vector3 dampVelocity;
    private float dampRotZVelocity;
    private float dampRotXVelocity;

    private void Awake()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
            startRot = transform.rotation;
        }
    }

    /// <summary>
    /// Matches original CamFollow_ResetCamera.
    /// </summary>
    public void ResetCamera()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = startRot;
            dampVelocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Position follow with per-axis lerp factors (original lerpXfactor/Y/Z)
        Vector3 targetPos = target.position + offset;
        Vector3 currentPos = transform.position;

        Vector3 desiredPos = new Vector3(
            Mathf.Lerp(currentPos.x, targetPos.x, lerpXfactor),
            Mathf.Lerp(currentPos.y, targetPos.y, lerpYfactor),
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
