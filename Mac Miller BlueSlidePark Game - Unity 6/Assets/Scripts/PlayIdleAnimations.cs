using UnityEngine;

/// <summary>
/// Idle animation controller matching original PlayIdleAnimations class from SWF.
/// Decompiled source: PlayIdleAnimations.as
///
/// Original behavior:
///   Start: finds Animation component in children
///     - "idle" clip → layer 0 (base idle)
///     - clips starting with "idle" → layer 1 (break animations)
///   Update: if mNextBreak < Time.time → play random break
///     - Single break: interval = clip.length + Random(5, 15)
///     - Multiple breaks: interval = clip.length + Random(2, 8)
///     - Avoids same break twice in a row (index + 1 wrapping)
///
/// Original fields: mAnim, mIdle, mBreaks, mNextBreak, mLastIndex.
/// </summary>
public class PlayIdleAnimations : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string[] breakTriggers;
    // Original intervals: single break = 5-15s, multiple breaks = 2-8s
    [SerializeField] private float minBreakInterval = 2f;
    [SerializeField] private float maxBreakInterval = 8f;

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
