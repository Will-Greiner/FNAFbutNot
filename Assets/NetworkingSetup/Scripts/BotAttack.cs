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

    [SerializeField] private Animator animator;

    private float cooldownLocal;
    public float AttackCooldown => attackCooldown;

    // NEW: attack state for movement gating
    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    public event System.Action LocalAttackFired;

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

        if (Input.GetButtonDown("Fire1") && cooldownLocal <= 0f && !isAttacking)
        {
            cooldownLocal = attackCooldown;
            isAttacking = true;
            LocalAttackFired?.Invoke();

            // just play animation; hit from animation event
            PlayAttackFxClientRpc();
        }
    }

    // Animation event: called at hit frame
    public void AnimationAttackHit()
    {
        if (!IsOwner)
            return;

        Vector3 originPos = attackOrigin ? attackOrigin.position : transform.position;
        Vector3 forward = attackOrigin ? attackOrigin.forward : transform.forward;

        TryAttackServerRpc(originPos, forward);
    }

    // Animation event: called at end of swing
    public void AnimationAttackEnd()
    {
        if (!IsOwner)
            return;

        isAttacking = false;
    }

    [ServerRpc]
    private void TryAttackServerRpc(Vector3 originPos, Vector3 forward)
    {
        if (!CanServerAttack())
            return;
        SetServerCooldown();

        int hits = Physics.OverlapSphereNonAlloc(
            originPos + forward.normalized * attackRange * 0.6f,
            attackRadius,
            s_overlapCache,
            hittableLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits; i++)
        {
            var collider = s_overlapCache[i];
            if (!collider) continue;

            Vector3 to = collider.transform.position - originPos;
            if (to.sqrMagnitude > attackRange * attackRange) continue;

            if (requireLineOfSight)
            {
                if (Physics.Linecast(originPos,
                                     collider.bounds.center,
                                     out var hit,
                                     losBlockers,
                                     QueryTriggerInteraction.Ignore))
                    continue;
            }

            var netObj = collider.GetComponentInParent<NetworkObject>();
            if (netObj == null || netObj.NetworkObjectId == NetworkObjectId) continue;

            var targetStun = netObj.GetComponent<StunState>();
            if (targetStun != null)
            {
                targetStun.ApplyStun(stunSeconds);
            }
        }
    }

    [ClientRpc]
    private void PlayAttackFxClientRpc()
    {
        if (animator != null)
            animator.SetTrigger("attack");
    }

    // ------- server cooldown tracking -------
    private double _serverNextAttackTime;
    private bool CanServerAttack()
        => NetworkManager != null &&
           NetworkManager.IsServer &&
           NetworkManager.ServerTime.Time >= _serverNextAttackTime;

    private void SetServerCooldown()
    {
        if (!NetworkManager) return;
        _serverNextAttackTime = NetworkManager.ServerTime.Time + attackCooldown;
    }

    private static readonly Collider[] s_overlapCache = new Collider[16];
}
