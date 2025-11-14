using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interactable station: bot walks into trigger, holds a key,
/// fills a progress bar, and when complete reduces the timer on all TimedDoor instances.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class DoorFixStation : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Tag the bot/player must have to use this station.")]
    [SerializeField] private string botTag = "Player"; // or "Bot" if you use that

    [Tooltip("Key to hold down to charge the upgrade.")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Tooltip("How long the key must be held to complete the upgrade (seconds).")]
    [SerializeField] private float holdDuration = 3f;

    [Tooltip("How much time is added to all doors when complete (seconds).")]
    [SerializeField] private float doorTimeIncrease = 10;

    [Tooltip("Can this station only be used once?")]
    [SerializeField] private bool oneUseOnly = true;

    [Header("UI")]
    [Tooltip("Image used as a radial/filled progress bar.")]
    [SerializeField] private Image progressBar;

    [Tooltip("Optional prompt text to show when in range.")]
    [SerializeField] private GameObject promptUI;

    private bool _botInRange;
    private bool _used;
    private float _holdTimer;
    private Transform _currentBot;

    private void Awake()
    {
        var sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;

        if (progressBar)
            progressBar.fillAmount = 0f;

        if (promptUI)
            promptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(botTag)) return;

        _botInRange = true;
        _currentBot = other.transform;

        if (promptUI)
            promptUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_currentBot != null && other.transform == _currentBot)
        {
            _botInRange = false;
            _currentBot = null;
            ResetHold();
        }
    }

    private void Update()
    {
        if (_used || !_botInRange) return;

        bool keyDown = Input.GetKey(interactKey);

        if (keyDown)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= holdDuration)
            {
                _holdTimer = holdDuration;
                CompleteUpgrade();
            }
        }
        else if (_holdTimer > 0f)
        {
            // Cancel if they release the key early
            ResetHold();
        }

        UpdateProgressBar();
    }

    private void CompleteUpgrade()
    {
        // Apply reduction globally to all timed doors
        TimedDoor.AddGlobalTimeReduction(doorTimeIncrease);

        if (oneUseOnly)
        {
            _used = true;
            if (promptUI) promptUI.SetActive(false);
        }

        // Optionally reset progress bar for repeat use if not oneUseOnly
        ResetHold();
    }

    private void ResetHold()
    {
        _holdTimer = 0f;
        UpdateProgressBar();
    }

    private void UpdateProgressBar()
    {
        if (!progressBar) return;
        progressBar.fillAmount = Mathf.Clamp01(_holdTimer / holdDuration);
    }
}
