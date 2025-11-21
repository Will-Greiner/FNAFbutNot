using UnityEngine;
using Unity.Netcode;

public class AnimatronicAttack : NetworkBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackRadius = 0.9f;
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private LayerMask hittableLayers;

    [Header("Validation")]
    [SerializeField] private Transform attackOrigin; // e.g., chest or weapon tip
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask losBlockers;  // walls, props, etc.

    [Header("Visuals / Anim")]
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField, Range(0f, 1f)] private float attackVolume = 1f;

    private float cooldownLocal;
    public float AttackCooldown => attackCooldown;

    public event System.Action LocalAttackFired;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    private double _serverNextAttackTime;
    private static readonly Collider[] s_overlapCache = new Collider[16];

    private void Reset()
    {
        attackOrigin = transform;
    }

    private void Awake()
    {
        // Auto-wire animator / audio if not set in inspector
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (attackAudioSource == null)
            attackAudioSource = GetComponentInChildren<AudioSource>(true);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer && animator)
            animator.applyRootMotion = false;
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
            LocalAttackFired?.Invoke(); // cooldown UI, etc.

            // Ask the server to trigger attack FX on everyone
            RequestAttackFxServerRpc();
        }
    }

    // ------ animation events (called via AnimatronicAttackAnimationRelay) ------

    public void AnimationAttackHit()
    {
        if (!IsOwner)
            return;

        Vector3 originPos = attackOrigin ? attackOrigin.position : transform.position;
        Vector3 forward = attackOrigin ? attackOrigin.forward : transform.forward;

        TryAttackServerRpc(originPos, forward);
    }

    public void AnimationAttackEnd()
    {
        if (!IsOwner)
            return;

        isAttacking = false;
    }

    // ------ server-side attack + FX gating ------

    [ServerRpc]
    private void RequestAttackFxServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only the owner may request
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        // Server-side cooldown (authoritative)
        if (!CanServerAttack())
            return;

        SetServerCooldown();

        // Kick off animation + audio on all clients
        PlayAttackFxClientRpc();
    }

    [ClientRpc]
    private void PlayAttackFxClientRpc()
    {
        if (animator != null)
            animator.SetTrigger("attack");

        PlayAttackSoundLocal();
    }

    private void PlayAttackSoundLocal()
    {
        if (attackAudioSource == null || attackClips == null || attackClips.Length == 0)
            return;

        var clip = attackClips[Random.Range(0, attackClips.Length)];
        if (clip == null) return;

        attackAudioSource.PlayOneShot(clip, attackVolume);
    }

    [ServerRpc]
    private void TryAttackServerRpc(Vector3 originPos, Vector3 forward)
    {
        // NOTE: no cooldown check here; it's handled in RequestAttackFxServerRpc.

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
            if (netObj == null || netObj.NetworkObjectId == NetworkObjectId)
                continue;

            // apply stun / damage here...
        }
    }

    // ------ server cooldown helpers ------

    private bool CanServerAttack()
        => NetworkManager != null &&
           NetworkManager.IsServer &&
           NetworkManager.ServerTime.Time >= _serverNextAttackTime;

    private void SetServerCooldown()
    {
        if (!NetworkManager) return;
        _serverNextAttackTime = NetworkManager.ServerTime.Time + attackCooldown;
    }
}
