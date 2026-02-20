using UnityEngine;

/// <summary>
/// Obstacle/pickup positioning matching original ObsticlePosition class from SWF.
/// Decompiled source: ObsticlePosition.as
///
/// Original: Offset = Vector3(0, 12, 0) — positions obstacle 12 units above slide.
/// Used by SlideController.CreateNextPiece to place obstacles.
/// Original also rotated child[0] by random -60 to 60 degrees around forward axis.
/// </summary>
public class ObstaclePosition : MonoBehaviour
{
    // Original default: new Vector3(0, 12, 0)
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, 0f);
    [SerializeField] private bool randomizeX = true;
    [SerializeField] private float slideHalfWidth = 4f;

    private void Start()
    {
        if (randomizeX)
        {
            // Original: RotateAround with random -60 to 60 degrees
            Vector3 pos = transform.localPosition;
            pos.x = Random.Range(-slideHalfWidth, slideHalfWidth);
            transform.localPosition = pos;
        }
    }

    public Vector3 Offset => offset;
}
