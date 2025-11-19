using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GuardHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int hitsToDestroy = 3;

    [Tooltip("Min time (seconds) between damage applications to this bot.")]
    [SerializeField] private float damageCooldown = 2;

    [SerializeField] private Image healthbar;

    [Header("End Game")]
    [Tooltip("If true (eg. on the Guard), reaching 0 health will trigger Game Over.")]
    [SerializeField] private bool triggerGameOverOnDeath = false;

    // Server-only replicated number of hits remaining (handy for UI if needed)
    public readonly NetworkVariable<int> RemainingHits = new(writePerm: NetworkVariableWritePermission.Server);

    private double nextAllowedDamageTime;
    private int maxHits;

    public override void OnNetworkSpawn()
    {
        maxHits = hitsToDestroy;

        RemainingHits.OnValueChanged += OnRemainingHitsChanged;

        if (!IsServer) return;

        RemainingHits.Value = hitsToDestroy;
        nextAllowedDamageTime = 0;

        UpdateHealthUI();
    }

    public override void OnNetworkDespawn()
    {
        RemainingHits.OnValueChanged -= OnRemainingHitsChanged;
    }

    public void AttachHealthSlider(Image healthbar)
    {
        this.healthbar = healthbar;

        UpdateHealthUI();
    }

    private void OnRemainingHitsChanged(int oldValue, int newValue)
    {
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (!healthbar)
            return;

        float normalized = maxHits > 0 ? (float)RemainingHits.Value / maxHits : 0;

        healthbar.fillAmount = normalized;
    }

    public void ResetHealthUI()
    {
        if (!healthbar) return;

        healthbar.fillAmount = 1f;
    }

    /// Called on the SERVER when someone successfully hits this bot.
    /// Pass the attackerClientId from your BotAttack script.
    public void ApplyHit(ulong attackerClientId)
    {
        if (!IsServer || NetworkManager == null)
            return;

        double now = NetworkManager.ServerTime.Time;
        if (now < nextAllowedDamageTime)
            return; // per-victim damage cooldown

        nextAllowedDamageTime = now + damageCooldown;

        if (RemainingHits.Value <= 0)
            return;

        RemainingHits.Value--;

        PlayHitFxClientRpc();

        if (RemainingHits.Value <= 0)
        {
            if (triggerGameOverOnDeath)
            {
                TriggerEndGame();
            }
            else
            {
                // Bot is dead: respawn the host back at the guard spawn
                // and prevent re-using this spawn.
                if (GuardPrefabSwapper.Instance != null)
                {
                    GuardPrefabSwapper.Instance.OnControlledBotDestroyed();
                }
            }
        }
    }

    private void TriggerEndGame()
    {
        // We're on the server here; just broadcast GameOver UI to everyone.
        GameOverClientRpc();
    }

    [ClientRpc]
    private void PlayHitFxClientRpc()
    {
        // Optional: flash, play sound, etc.
    }

    [ClientRpc]
    private void GameOverClientRpc()
    {
        if (GameOverUIController.Instance != null)
        {
            GameOverUIController.Instance.ShowGameOver();
        }
    }
}
