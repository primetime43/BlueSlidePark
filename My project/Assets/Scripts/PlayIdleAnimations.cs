using UnityEngine;

/// <summary>
/// Idle animation controller matching original PlayIdleAnimations class from SWF.
/// Original fields: mAnim, mIdle, mBreaks (array of break animations),
/// mNextBreak (time of next break), mLastIndex (last break animation played).
/// Plays idle animation with occasional break animations for variety.
/// </summary>
public class PlayIdleAnimations : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string[] breakTriggers;
    [SerializeField] private float minBreakInterval = 5f;
    [SerializeField] private float maxBreakInterval = 15f;

    private float nextBreakTime;
    private int lastBreakIndex = -1;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        ScheduleNextBreak();
    }

    private void Update()
    {
        if (animator == null || breakTriggers == null || breakTriggers.Length == 0)
            return;

        if (Time.time >= nextBreakTime)
        {
            PlayBreakAnimation();
            ScheduleNextBreak();
        }
    }

    private void PlayBreakAnimation()
    {
        // Pick a random break animation, avoiding the same one twice in a row
        int index;
        do
        {
            index = Random.Range(0, breakTriggers.Length);
        } while (index == lastBreakIndex && breakTriggers.Length > 1);

        lastBreakIndex = index;
        animator.SetTrigger(breakTriggers[index]);
    }

    private void ScheduleNextBreak()
    {
        nextBreakTime = Time.time + Random.Range(minBreakInterval, maxBreakInterval);
    }
}
