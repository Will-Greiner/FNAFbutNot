using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BotHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int hitsToDestroy = 3;

    [Tooltip("Min time (seconds) between damage applications to this bot.")]
    [SerializeField] private float damageCooldown = 2;

    [SerializeField] private Image healthbar;

    [Header("Hit FX")]
    [Tooltip("AudioSource on the bot used for hit sounds.")]
    [SerializeField] private AudioSource hitAudioSource;
    [SerializeField] private AudioClip hitClip;

    [Header("Death FX")]
    [Tooltip("Prefab with particle system + audio for death. Will be spawned at the bot's position on all clients.")]
    [SerializeField] private GameObject deathFxPrefab;

    // Server-only replicated number of hits remaining
    public readonly NetworkVariable<int> RemainingHits =
        new(writePerm: NetworkVariableWritePermission.Server);

    private double nextAllowedDamageTime;
    private int maxHits;

    public override void OnNetworkSpawn()
    {
        maxHits = hitsToDestroy;

        RemainingHits.OnValueChanged += OnRemainingHitsChanged;

        // Optional auto-wiring of the hit audio source
        if (hitAudioSource == null)
        {
            hitAudioSource = GetComponentInChildren<AudioSource>(true);
        }

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

        // 🔊 Hit sound on all clients
        PlayHitFxClientRpc();

        if (RemainingHits.Value <= 0)
        {
            // 💥 Death FX (sound + particles) on all clients
            PlayDeathFxClientRpc(transform.position);

            // Bot is dead: notify GuardPrefabSwapper / respawn logic
            if (GuardPrefabSwapper.Instance != null)
            {
                GuardPrefabSwapper.Instance.OnControlledBotDestroyed();
            }
        }
    }

    [ClientRpc]
    private void PlayHitFxClientRpc()
    {
        if (hitAudioSource != null && hitClip != null)
        {
            hitAudioSource.PlayOneShot(hitClip);
        }
        // If you want a small hit flash, you can also trigger a local material flash here.
    }

    [ClientRpc]
    private void PlayDeathFxClientRpc(Vector3 position)
    {
        if (deathFxPrefab != null)
        {
            Instantiate(deathFxPrefab, position, Quaternion.identity);
        }
    }
}
