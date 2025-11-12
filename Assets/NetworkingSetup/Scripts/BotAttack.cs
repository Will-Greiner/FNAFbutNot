using UnityEngine;
using Unity.Netcode;

public class BotAttack : NetworkBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackRadius = 0.9f;
    [SerializeField] private float stunSeconds = 2.0f;
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private LayerMask hittableLayers;

    [Header("Validation")]
    [SerializeField] private Transform attackOrigin; // e.g., chest or weapon tip
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask losBlockers;  // walls, props, etc.

    private float cooldownLocal;

    [SerializeField] private Animator animator;

    private void Reset()
    {
        attackOrigin = transform; // fallback
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer && animator) animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (!IsOwner) 
            return;

        cooldownLocal -= Time.deltaTime;

        if (Input.GetButtonDown("Fire1") && cooldownLocal <= 0f)
        {
            cooldownLocal = attackCooldown;

            TryAttackServerRpc(attackOrigin ? attackOrigin.position : transform.position, attackOrigin ? attackOrigin.forward : transform.forward);
        }
    }

    [ServerRpc]
    private void TryAttackServerRpc(Vector3 originPos, Vector3 forward)
    {
        // cooldown per-attacker (server-trusted)
        if (!CanServerAttack()) 
            return;
        SetServerCooldown();

        // Overlap sphere in a small capsule/cone in front of attacker
        int hits = Physics.OverlapSphereNonAlloc(originPos + forward.normalized * attackRange * 0.6f, attackRadius, s_overlapCache, hittableLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits; i++)
        {
            Debug.Log("Atacc");
            var collider = s_overlapCache[i];
            if (!collider) continue;

            // Basic facing/range validation
            Vector3 to = collider.transform.position - originPos;
            if (to.sqrMagnitude > attackRange * attackRange) continue;
            if (Vector3.Dot(forward.normalized, to.normalized) > 0.2f) continue;

            if (requireLineOfSight)
            {
                if (Physics.Linecast(originPos, collider.bounds.center, out var hit, losBlockers, QueryTriggerInteraction.Ignore))
                    continue;
            }

            // Find stunstate on target root
            var netObj = collider.GetComponentInParent<NetworkObject>();
            if (netObj == null || netObj.NetworkObjectId == NetworkObjectId) continue; // don't hit yourself

            var targetStun = netObj.GetComponent<StunState>();
            if (targetStun != null)
            {
                targetStun.ApplyStun(stunSeconds);
            }
        }

        // Optional: tell clients to play swing SFX/anim
        PlayAttackFxClientRpc();
    }

    [ClientRpc]
    private void PlayAttackFxClientRpc() 
    {
        animator.SetTrigger("attack");
    }

    // ------- server cooldown tracking -------
    private double _serverNextAttackTime;
    private bool CanServerAttack()
        => NetworkManager != null && NetworkManager.IsServer && NetworkManager.ServerTime.Time >= _serverNextAttackTime;

    private void SetServerCooldown()
    {
        if (!NetworkManager) return;
        _serverNextAttackTime = NetworkManager.ServerTime.Time + attackCooldown;
    }

    // shared overlap buffer (avoid allocs)
    private static readonly Collider[] s_overlapCache = new Collider[16];
}
