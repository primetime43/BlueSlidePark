using System.Collections.Generic;
using UnityEngine;

public class WorldMover : MonoBehaviour
{
    [SerializeField] private List<GameObject> slides = new List<GameObject>();
    [SerializeField] private Transform groundTrans;
    [SerializeField] private Transform pickups;
    [SerializeField] private float totalSlideLength;

    [Header("Speed (original: ramps from 6 to 10 over ~40s)")]
    [SerializeField] private float lerpSpeed = 25f;
    [SerializeField] private float startSpeedFraction = 0.7f;
    [SerializeField] private float speedRampDuration = 40f;

    [Header("Curves (original: TurnDirectionCount/TurnCooldown)")]
    [SerializeField] private float maxCurveOffset = 8f;
    [SerializeField] private float curveSmoothSpeed = 2f;
    [SerializeField] private float curveChangeMinTime = 1f;
    [SerializeField] private float curveChangeMaxTime = 3f;

    [Header("Pickups")]
    [SerializeField] private float pickupXRange = 4f;
    [SerializeField] private float pickupMinSpacing = 5f;
    [SerializeField] private float pickupMaxSpacing = 15f;

    private int slideChildCount;
    private float segmentLength;
    private Vector3 groundStartPos;
    private Vector3 pickupsStartPos;
    private float gameStartTime;
    private float currentCurveX;
    private float targetCurveX;
    private float curveTimer;

    private void Awake()
    {
        groundStartPos = groundTrans.position;
        pickupsStartPos = pickups.position;
        gameStartTime = Time.time;
    }

    private void Start()
    {
        SlideCounter();
        InitializePickupPositions();
    }

    private void Update()
    {
        // Speed ramps up over time (original: min(10, 6 + (time-startTime)/10))
        float elapsed = Time.time - gameStartTime;
        float speedMul = Mathf.Lerp(startSpeedFraction, 1f, elapsed / speedRampDuration);
        float moveAmount = lerpSpeed * speedMul * Time.deltaTime;

        groundTrans.position += Vector3.back * moveAmount;
        pickups.position += Vector3.back * moveAmount;

        UpdateCurve();

        // Apply curve: shift entire slide + pickups sideways together
        // This creates smooth visible curves as the whole slide sways left/right
        Vector3 gPos = groundTrans.position;
        gPos.x = groundStartPos.x + currentCurveX;
        groundTrans.position = gPos;

        Vector3 pPos = pickups.position;
        pPos.x = pickupsStartPos.x + currentCurveX;
        pickups.position = pPos;

        RecycleSegments();
        RecyclePickups();
    }

    private void UpdateCurve()
    {
        curveTimer -= Time.deltaTime;
        if (curveTimer <= 0f)
        {
            targetCurveX = Random.Range(-maxCurveOffset, maxCurveOffset);
            curveTimer = Random.Range(curveChangeMinTime, curveChangeMaxTime);
        }
        currentCurveX = Mathf.Lerp(currentCurveX, targetCurveX, curveSmoothSpeed * Time.deltaTime);
    }

    private void RecycleSegments()
    {
        if (slides.Count == 0) return;

        float maxLocalZ = float.MinValue;
        foreach (var s in slides)
        {
            if (s.transform.localPosition.z > maxLocalZ)
                maxLocalZ = s.transform.localPosition.z;
        }

        foreach (var segment in slides)
        {
            if (segment.transform.position.z < -segmentLength * 2)
            {
                Vector3 localPos = segment.transform.localPosition;
                localPos.z = maxLocalZ + segmentLength;
                localPos.x = 0;
                segment.transform.localPosition = localPos;
                maxLocalZ = localPos.z;
            }
        }
    }

    private void RecyclePickups()
    {
        float maxLocalZ = float.MinValue;
        foreach (Transform child in pickups)
        {
            if (child.localPosition.z > maxLocalZ)
                maxLocalZ = child.localPosition.z;
        }

        foreach (Transform child in pickups)
        {
            if (child.position.z < -segmentLength)
            {
                Vector3 localPos = child.localPosition;
                localPos.z = maxLocalZ + Random.Range(pickupMinSpacing, pickupMaxSpacing);
                localPos.x = Random.Range(-pickupXRange, pickupXRange);
                child.localPosition = localPos;
                maxLocalZ = localPos.z;

                var col = child.GetComponent<Collider>();
                if (col != null)
                    col.enabled = true;
                var mr = child.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.enabled = true;

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
        gameStartTime = Time.time;
        currentCurveX = 0f;
        targetCurveX = 0f;
        curveTimer = 0f;

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
