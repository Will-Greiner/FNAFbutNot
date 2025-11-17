using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interactable station: guard walks into trigger, holds a key,
/// fills a progress bar, and when complete sets a bool on the interacting guard.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ShotgunBox : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Tag the guard/player must have to use this station.")]
    [SerializeField] private string guardTag = "Player"; // or "Guard" if you use that

    [Tooltip("Key to hold down to charge the interaction.")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Tooltip("How long the key must be held to complete the interaction (seconds).")]
    [SerializeField] private float holdDuration = 3f;

    [Tooltip("Can this station only be used once?")]
    [SerializeField] private bool oneUseOnly = true;

    [Header("UI")]
    [Tooltip("Image used as a radial/filled progress bar.")]
    [SerializeField] private Image progressBar;

    [Tooltip("Optional prompt text to show when in range.")]
    [SerializeField] private GameObject promptUI;

    private bool _guardInRange;
    private bool _used;
    private float _holdTimer;
    private Transform _currentGuard;

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
        if (!other.CompareTag(guardTag)) return;

        _guardInRange = true;
        _currentGuard = other.transform;

        if (promptUI)
            promptUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_currentGuard != null && other.transform == _currentGuard)
        {
            _guardInRange = false;
            _currentGuard = null;
            ResetHold();
        }
    }

    private void Update()
    {
        if (_used || !_guardInRange) return;

        bool keyDown = Input.GetKey(interactKey);

        if (keyDown)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= holdDuration)
            {
                _holdTimer = holdDuration;
                CompleteInteraction();
            }
        }
        else if (_holdTimer > 0f)
        {
            // Cancel if they release the key early
            ResetHold();
        }

        UpdateProgressBar();
    }

    private void CompleteInteraction()
    {
        if (_currentGuard != null)
        {
            // Look up the GuardFlag on the interacting guard (or its parents)
            var shotgun = _currentGuard.GetComponent<FPMovement>().showGun = true;
            //if (shotgun != null)
            //{
            //    shotgun.gameObject.SetActive(true);
            //}
        }

        if (oneUseOnly)
        {
            _used = true;
            if (promptUI) promptUI.SetActive(false);
        }

        gameObject.SetActive(false);

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
