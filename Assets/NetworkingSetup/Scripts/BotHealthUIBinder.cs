using UnityEngine;
using UnityEngine.UI;

public class BotHealthUIBinder : MonoBehaviour
{
    [SerializeField] private Image healthbar;
    [SerializeField] private AttackCooldownUI cooldownUI;

    private void Awake()
    {
        if (!healthbar)
            healthbar = GetComponentInChildren<Image>();

        // Find the BotHealth on the parent hierarchy
        var botHealth = GetComponentInParent<BotHealth>();
        if (botHealth != null && healthbar != null)
        {
            botHealth.AttachHealthSlider(healthbar);
        }
        else
        {
            Debug.LogWarning("BotHealthUIBinder: BotHealth or Slider not found.");
        }

        if (!cooldownUI)
            cooldownUI = GetComponentInChildren<AttackCooldownUI>();

        // Find the local player's attack script in parents, or assign explicitly
        var attack = GetComponentInParent<AnimatronicAttack>();
        if (attack != null)
        {
            cooldownUI.enabled = true;
            cooldownUI.GetType().GetField("attackComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cooldownUI, attack);
        }
    }
}
