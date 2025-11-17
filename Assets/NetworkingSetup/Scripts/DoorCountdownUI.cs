using UnityEngine;
using TMPro;

/// <summary>
/// Shows a single countdown text "Stage N - MM:SS"
/// using three stage times and the same global time increase as TimedDoor.
/// Attach this to a UI prefab and drop that prefab into any player UI.
/// All instances share the same global timer.
/// </summary>
public class DoorCountdownUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text stageTimerText;

    [Header("Stage 1 Settings")]
    [Tooltip("Base delay before Stage 1 event. Match this to your Stage 1 TimedDoor baseDelay.")]
    [SerializeField] private float stage1BaseDelay = 30f;
    [SerializeField] private float stage1MinimumDelay = 1f;

    [Header("Stage 2 Settings")]
    [Tooltip("Base delay before Stage 2 event.")]
    [SerializeField] private float stage2BaseDelay = 60f;
    [SerializeField] private float stage2MinimumDelay = 1f;

    [Header("Stage 3 Settings")]
    [Tooltip("Base delay before Stage 3 (end-game) triggers.")]
    [SerializeField] private float stage3BaseDelay = 90f;
    [SerializeField] private float stage3MinimumDelay = 1f;

    [Tooltip("Start the timer automatically the first time any instance enables.")]
    [SerializeField] private bool autoStartOnEnable = true;

    // ---------- GLOBAL SHARED TIMER STATE ----------
    private static bool s_started = false;
    private static float s_startTime;
    private static bool s_stage3Triggered = false;

    /// <summary>
    /// Call this once when the game starts (e.g. from a GameManager),
    /// so all UIs share the same start time.
    /// </summary>
    public static void StartGlobalCountdown()
    {
        if (s_started) return;

        s_started = true;
        s_startTime = Time.time;
        s_stage3Triggered = false;
    }

    private void OnEnable()
    {
        // If you don't start from a GameManager, the first UI instance can start it.
        if (autoStartOnEnable && !s_started)
        {
            StartGlobalCountdown();
        }
    }

    private void Update()
    {
        if (!s_started || stageTimerText == null)
            return;

        float elapsed = Time.time - s_startTime;
        float globalIncrease = TimedDoor.GlobalTimeIncrease; // shared extra time for all stages

        // Effective stage times after upgrades
        float stage1Effective = Mathf.Max(stage1MinimumDelay, stage1BaseDelay + globalIncrease);
        float stage2Effective = Mathf.Max(stage2MinimumDelay, stage2BaseDelay + globalIncrease);
        float stage3Effective = Mathf.Max(stage3MinimumDelay, stage3BaseDelay + globalIncrease);

        int currentStage;
        float stageEndTime;
        float stageRemaining;

        // Determine which stage we're currently in and how much time remains to the next stage boundary
        if (elapsed < stage1Effective)
        {
            currentStage = 1;
            stageEndTime = stage1Effective;
        }
        else if (elapsed < stage2Effective)
        {
            currentStage = 2;
            stageEndTime = stage2Effective;
        }
        else if (elapsed < stage3Effective)
        {
            currentStage = 3;
            stageEndTime = stage3Effective;
        }
        else
        {
            // Past Stage 3: clamp to 0 and treat as "Stage 3 complete"
            currentStage = 3;
            stageEndTime = stage3Effective;
        }

        stageRemaining = Mathf.Max(0f, stageEndTime - elapsed);

        // Update the single text: "Stage N - MM:SS"
        stageTimerText.text = $"Stage {currentStage} - {FormatTime(stageRemaining)}";

        // When Stage 3 hits zero the first time, trigger end-game once
        if (!s_stage3Triggered && elapsed >= stage3Effective)
        {
            s_stage3Triggered = true;

            if (GuardPrefabSwapper.Instance != null)
            {
                // Only actually does work on host/server in your existing implementation
                GuardPrefabSwapper.Instance.TriggerEndGame();
            }
        }
    }

    private string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = total / 60;
        int secs = total % 60;
        return $"{minutes:00}:{secs:00}";
    }
}
