using UnityEngine;

/// <summary>
/// Obstacle/pickup positioning matching original ObsticlePosition class from SWF.
/// Original field: Offset — used to position obstacles on the slide.
/// The original SlideController would place obstacles using this offset
/// relative to each slide piece.
/// </summary>
public class ObstaclePosition : MonoBehaviour
{
    [SerializeField] private float offset;
    [SerializeField] private bool randomizeX = true;
    [SerializeField] private float slideHalfWidth = 4f;

    private void Start()
    {
        if (randomizeX)
        {
            // Randomize X position within slide bounds
            Vector3 pos = transform.localPosition;
            pos.x = Random.Range(-slideHalfWidth, slideHalfWidth);
            transform.localPosition = pos;
        }
    }

    public float Offset => offset;
}
