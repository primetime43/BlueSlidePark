using System.Collections.Generic;
using UnityEngine;

public class WorldMover : MonoBehaviour
{
    [SerializeField] private List<GameObject> slides = new List<GameObject>();
    [SerializeField] private Transform groundTrans;
    [SerializeField] private Transform pickups;
    [SerializeField] private float lerpSpeed = 25f;
    [SerializeField] private float totalSlideLength;
    [SerializeField] private float pickupXRange = 4f;
    [SerializeField] private float pickupMinSpacing = 10f;
    [SerializeField] private float pickupMaxSpacing = 25f;

    private int slideChildCount;
    private float segmentLength;
    private Vector3 groundStartPos;
    private Vector3 pickupsStartPos;

    private void Awake()
    {
        groundStartPos = groundTrans.position;
        pickupsStartPos = pickups.position;
    }

    private void Start()
    {
        SlideCounter();
        InitializePickupPositions();
    }

    private void Update()
    {
        float moveAmount = lerpSpeed * Time.deltaTime;

        groundTrans.position += Vector3.back * moveAmount;
        pickups.position += Vector3.back * moveAmount;

        RecycleSegments();
        RecyclePickups();
    }

    private void RecycleSegments()
    {
        if (slides.Count == 0) return;

        // Find the frontmost segment local Z
        float maxLocalZ = float.MinValue;
        foreach (var s in slides)
        {
            if (s.transform.localPosition.z > maxLocalZ)
                maxLocalZ = s.transform.localPosition.z;
        }

        foreach (var segment in slides)
        {
            // When a segment's world position is far behind the player, recycle it to the front
            if (segment.transform.position.z < -segmentLength * 2)
            {
                Vector3 localPos = segment.transform.localPosition;
                localPos.z = maxLocalZ + segmentLength;
                segment.transform.localPosition = localPos;
                maxLocalZ = localPos.z;
            }
        }
    }

    private void RecyclePickups()
    {
        // Find the furthest forward pickup (local Z)
        float maxLocalZ = float.MinValue;
        foreach (Transform child in pickups)
        {
            if (child.localPosition.z > maxLocalZ)
                maxLocalZ = child.localPosition.z;
        }

        // Recycle each item individually when it scrolls behind the player
        foreach (Transform child in pickups)
        {
            if (child.position.z < -segmentLength)
            {
                Vector3 localPos = child.localPosition;
                localPos.z = maxLocalZ + Random.Range(pickupMinSpacing, pickupMaxSpacing);
                localPos.x = Random.Range(-pickupXRange, pickupXRange);
                child.localPosition = localPos;
                maxLocalZ = localPos.z;

                // Re-enable collected pickups
                var col = child.GetComponent<Collider>();
                if (col != null)
                    col.enabled = true;
                var mr = child.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.enabled = true;

                // Reset spinner state for reuse
                var spinner = child.GetComponent<PickupSpinner>();
                if (spinner != null)
                    spinner.ResetForReuse();
            }
        }
    }

    private void InitializePickupPositions()
    {
        float z = 15f;
        foreach (Transform child in pickups)
        {
            Vector3 localPos = child.localPosition;
            localPos.x = Random.Range(-pickupXRange, pickupXRange);
            localPos.z = z;
            child.localPosition = localPos;
            z += Random.Range(pickupMinSpacing, pickupMaxSpacing);
        }
    }

    public void ResetWorld()
    {
        groundTrans.position = groundStartPos;
        pickups.position = pickupsStartPos;

        // Reset segment layout to original positions
        for (int i = 0; i < slides.Count; i++)
        {
            slides[i].transform.localPosition = new Vector3(0, 0, segmentLength * i);
        }

        InitializePickupPositions();
    }

    private void SlideCounter()
    {
        slides.Clear();
        totalSlideLength = 0;
        int count = 0;

        foreach (Transform child in groundTrans)
        {
            slides.Add(child.gameObject);
            child.localPosition = Vector3.zero;

            if (count != 0)
            {
                float previousLength = slides[count - 1].GetComponentInChildren<Collider>().bounds.size.z;
                child.localPosition = new Vector3(0, 0, previousLength * count);
            }

            totalSlideLength += child.GetComponentInChildren<Collider>().bounds.size.z;
            count++;
        }

        slideChildCount = count;
        segmentLength = slideChildCount > 0 ? totalSlideLength / slideChildCount : 15f;
    }
}
