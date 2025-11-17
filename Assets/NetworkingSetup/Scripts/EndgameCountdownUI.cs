using UnityEngine;
using TMPro;

/// <summary>
/// Simple global countdown: shows "MM:SS", and when it hits 0
/// it triggers the endgame once via GuardPrefabSwapper.
/// Put this on a UI prefab and drop it into any/all UIs.
/// </summary>
public class EndgameCountdownUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Settings")]
    [Tooltip("How long (seconds) until endgame triggers.")]
    [SerializeField] private float countdownDuration = 120f;

    [Tooltip("Start automatically the first time any instance enables.")]
    [SerializeField] private bool autoStartOnEnable = true;

    // ---------- GLOBAL SHARED TIMER STATE ----------
    private static bool s_started = false;
    private static float s_startTime = 0f;
    private static bool s_endgameTriggered = false;

    /// <summary>
    /// Optionally call this once from a GameManager when the match starts.
    /// All instances share this start time and duration.
    /// </summary>
    public static void StartGlobalEndgameCountdown()
    {
        if (s_started) return;

        s_started = true;
        s_startTime = Time.time;
        s_endgameTriggered = false;
    }

    private void OnEnable()
    {
        // If no one has started the countdown yet, the first UI can start it.
        if (autoStartOnEnable && !s_started)
        {
            StartGlobalEndgameCountdown();
        }
    }

    private void Update()
    {
        if (!s_started || timerText == null)
            return;

        float elapsed = Time.time - s_startTime;
        float remaining = Mathf.Max(0f, countdownDuration - elapsed);

        // Update UI
        timerText.text = FormatTime(remaining);

        // Trigger endgame once when we hit 0
        if (!s_endgameTriggered && remaining <= 0f)
        {
            s_endgameTriggered = true;

            if (GuardPrefabSwapper.Instance != null)
            {
                // Your GuardPrefabSwapper.TriggerEndGame() already checks IsServer/IsHost,
                // so it's safe to call from any client.
                GuardPrefabSwapper.Instance.TriggerEndGame();
            }
        }
    }

    private string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = total / 60;
        int secs = total % 60;
        return $"Delivery in: {minutes:00}:{secs:00}";
    }
}
