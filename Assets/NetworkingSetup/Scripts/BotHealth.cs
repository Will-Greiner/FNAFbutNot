using Unity.Netcode;
using UnityEngine;

public class BotHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int hitsToDestroy = 3;

    [Tooltip("Min time (seconds) between damage applications to this bot.")]
    [SerializeField] private float damageCooldown = 2;

    // Server-only replicated number of hits remaining (handy for UI if needed)
    public readonly NetworkVariable<int> RemainingHits = new(writePerm: NetworkVariableWritePermission.Server);

    private double nextAllowedDamageTime;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        RemainingHits.Value = hitsToDestroy;
        nextAllowedDamageTime = 0;
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
            // Bot is dead: respawn the host back at the guard spawn
            // and prevent re-using this spawn.
            if (GuardPrefabSwapper.Instance != null)
            {
                GuardPrefabSwapper.Instance.OnControlledBotDestroyed();
            }

            //// Despawn bot
            //if (NetworkObject && NetworkObject.IsSpawned)
            //    NetworkObject.Despawn(true);
            //else
            //    Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void PlayHitFxClientRpc()
    {
        // Optional: flash, play sound, etc.
    }
}
