using System.Collections.Generic;
using UnityEngine;

public class WorldMover : MonoBehaviour
{
    [SerializeField] private List<GameObject> slides = new List<GameObject>();
    [SerializeField] private Transform groundTrans;
    [SerializeField] private Transform pickups;
    [SerializeField] private float lerpSpeed = 25f;
    [SerializeField] private float totalSlideLength;

    private int slideChildCount;
    private Vector3 startingPos;

    private void Awake()
    {
        SlideCounter();
    }

    private void Start()
    {
        SlideCounter();
    }

    private void Update()
    {
        groundTrans.position += Vector3.back * lerpSpeed * Time.deltaTime;
        pickups.position += Vector3.back * lerpSpeed * Time.deltaTime;

        if (groundTrans.position.z <= -100f)
            ResetWorld();
    }

    public void ResetWorld()
    {
        groundTrans.position = startingPos;
    }

    private void SlideCounter()
    {
        slides.Clear();
        int count = 0;

        foreach (Transform child in groundTrans)
        {
            slides.Add(child.gameObject);
            child.position = Vector3.zero;

            if (count != 0)
            {
                float previousLength = slides[count - 1].GetComponentInChildren<Collider>().bounds.size.z;
                child.position = new Vector3(0, 0, previousLength * count);
            }

            totalSlideLength += child.GetComponentInChildren<Collider>().bounds.size.z;
            count++;
        }

        slideChildCount = count;
    }
}
