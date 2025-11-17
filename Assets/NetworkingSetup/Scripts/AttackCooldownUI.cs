using UnityEngine;
using UnityEngine.UI;

public class AttackCooldownUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;

    [Header("Source (optional)")]
    [SerializeField] private MonoBehaviour attackComponent;
    // Can be left null on the prefab – we'll set it from code.

    private AnimatronicAttack _animAttack;
    private BotAttack _botAttack;

    private float _cooldownDuration;
    private float _cooldownRemaining;

    private void Awake()
    {
        if (!fillImage)
            fillImage = GetComponent<Image>();

        if (fillImage)
            fillImage.fillAmount = 1f; // ready by default

        // If someone wired attackComponent in the inspector, hook it up now
        if (attackComponent != null)
        {
            HookFromSource(attackComponent);
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    // --------- PUBLIC API FOR RUNTIME WIRING ---------

    /// <summary>
    /// Call this from PlayerUIMounter after the UI is spawned to hook the correct attack script.
    /// </summary>
    public void InitializeFromAttack(MonoBehaviour source)
    {
        if (source == null) return;

        attackComponent = source;
        HookFromSource(attackComponent);
    }

    // --------- INTERNAL HOOKUP ---------

    private void HookFromSource(MonoBehaviour source)
    {
        // Clear old subscriptions if any
        Unsubscribe();

        _animAttack = source as AnimatronicAttack;
        _botAttack = source as BotAttack;

        if (_animAttack != null)
        {
            _cooldownDuration = _animAttack.AttackCooldown;
            _animAttack.LocalAttackFired += OnLocalAttackFired;
        }
        else if (_botAttack != null)
        {
            _cooldownDuration = _botAttack.AttackCooldown;
            _botAttack.LocalAttackFired += OnLocalAttackFired;
        }
        else
        {
            Debug.LogWarning($"{nameof(AttackCooldownUI)}: Source is not AnimatronicAttack or BotAttack.");
        }
    }

    private void Unsubscribe()
    {
        if (_animAttack != null)
            _animAttack.LocalAttackFired -= OnLocalAttackFired;
        if (_botAttack != null)
            _botAttack.LocalAttackFired -= OnLocalAttackFired;

        _animAttack = null;
        _botAttack = null;
    }

    private void OnLocalAttackFired()
    {
        _cooldownRemaining = _cooldownDuration;
    }

    private void Update()
    {
        if (_cooldownDuration <= 0f || fillImage == null)
            return;

        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining < 0f)
                _cooldownRemaining = 0f;
        }

        float normalized = 1f - (_cooldownRemaining / _cooldownDuration);
        fillImage.fillAmount = Mathf.Clamp01(normalized);
    }
}
