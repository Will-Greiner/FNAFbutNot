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
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask losBlockers;

    [Header("Visuals / Anim")]
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField, Range(0f, 1f)] private float attackVolume = 1f;

    [SerializeField] private ParticleSystem shockParticles;

    private float cooldownLocal;
    public float AttackCooldown => attackCooldown;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    public event System.Action LocalAttackFired;

    private double _serverNextAttackTime;
    private static readonly Collider[] s_overlapCache = new Collider[16];

    private void Reset()
    {
        attackOrigin = transform;
    }

    private void Awake()
    {
        // Only auto-wire if you really want to; otherwise require explicit assign.
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        // Do NOT auto-grab attackAudioSource; we want a separate, dedicated one.
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
            LocalAttackFired?.Invoke();

            // Ask the server to start the attack FX on everyone
            RequestAttackFxServerRpc();
        }
    }

    // Animation events (via BotAttackAnimationRelay)
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
        EndAttackFxClientRpc();
    }

    // Server side gating for attack + FX
    [ServerRpc]
    private void RequestAttackFxServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only owner may request
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        if (!CanServerAttack())
            return;

        SetServerCooldown();

        // Play anim + SFX on ALL clients
        PlayAttackFxClientRpc();
    }

    [ClientRpc]
    private void PlayAttackFxClientRpc()
    {
        if (animator != null)
            animator.SetTrigger("attack");

        PlayAttackSoundLocal();

        shockParticles.gameObject.SetActive(true);
        shockParticles.Play();
    }

    [ClientRpc]
    private void EndAttackFxClientRpc()
    {
        shockParticles.gameObject.SetActive(false);
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
        // No cooldown here; handled in RequestAttackFxServerRpc.

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
