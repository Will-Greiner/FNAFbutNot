using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows two countdown timers (Stage 1 & Stage 2) in the bot UI,
/// using the same global time reduction as TimedDoor.
/// Attach this to a HUD canvas and assign the two Text fields.
/// </summary>
public class DoorCountdownUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text stage1Text;
    [SerializeField] private TMP_Text stage2Text;

    [Header("Stage 1 Settings")]
    [Tooltip("Base delay before Stage 1 doors close. Match this to your Stage 1 TimedDoor baseDelay.")]
    [SerializeField] private float stage1BaseDelay = 30f;
    [SerializeField] private float stage1MinimumDelay = 1f;

    [Header("Stage 2 Settings")]
    [Tooltip("Base delay before Stage 2 doors close. Match this to your Stage 2 TimedDoor baseDelay.")]
    [SerializeField] private float stage2BaseDelay = 60f;
    [SerializeField] private float stage2MinimumDelay = 1f;

    [Tooltip("Optional: start the timer automatically when this UI enables.")]
    [SerializeField] private bool autoStartOnEnable = true;

    private float _startTime;
    private bool _started;

    private void OnEnable()
    {
        if (autoStartOnEnable)
            StartCountdown();
    }

    /// <summary>
    /// If you want to control when the countdown starts (e.g. from a game manager),
    /// you can call this method instead of using autoStartOnEnable.
    /// </summary>
    public void StartCountdown()
    {
        _startTime = Time.time;
        _started = true;
    }

    private void Update()
    {
        if (!_started) return;

        float elapsed = Time.time - _startTime;
        float globalIncrease = TimedDoor.GlobalTimeIncrease; // from your TimedDoor script

        // Stage 1 effective delay after upgrades
        float stage1Effective = Mathf.Clamp(
            stage1BaseDelay + globalIncrease,
            stage1MinimumDelay,
            stage1BaseDelay + globalIncrease);

        // Stage 2 effective delay after upgrades
        float stage2Effective = Mathf.Clamp(
            stage2BaseDelay + globalIncrease,
            stage2MinimumDelay,
            stage2BaseDelay + globalIncrease);

        float stage1Remaining = Mathf.Max(0f, stage1Effective - elapsed);
        float stage2Remaining = Mathf.Max(0f, stage2Effective - elapsed);

        if (stage1Text)
            stage1Text.text = FormatTime(stage1Remaining);

        if (stage2Text)
            stage2Text.text = FormatTime(stage2Remaining);
    }

    private string FormatTime(float seconds)
    {
        // Clamp negative and round up so you don't see 0: -1 spikes
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = total / 60;
        int secs = total % 60;
        return $"{minutes:00}:{secs:00}";
    }
}
