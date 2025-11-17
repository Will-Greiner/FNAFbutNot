using UnityEngine;
using Unity.Netcode;

public class Shotgun : NetworkBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float attackRadius = 1.5f;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private LayerMask hittableLayers;

    [Header("Validation")]
    [SerializeField] private Transform attackOrigin; // muzzle or camera
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask losBlockers;

    [SerializeField] private Animator animator;

    private float cooldownLocal;
    public float AttackCooldown => attackCooldown;
    public event System.Action LocalAttackFired;

    private static readonly Collider[] s_overlapCache = new Collider[16];

    private void Reset()
    {
        attackOrigin = transform;
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

            Vector3 originPos = attackOrigin ? attackOrigin.position : transform.position;
            Vector3 forward = attackOrigin ? attackOrigin.forward : transform.forward;

            TryShotgunServerRpc(originPos, forward);
            LocalAttackFired?.Invoke();
        }
    }

    [ServerRpc]
    private void TryShotgunServerRpc(Vector3 originPos, Vector3 forward)
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
                if (Physics.Linecast(originPos, collider.bounds.center,
                                     out var hit, losBlockers,
                                     QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
            }

            var netObj = collider.GetComponentInParent<NetworkObject>();
            if (netObj == null || netObj.NetworkObjectId == NetworkObjectId)
                continue;

            // Only handle animatronic players
            if (!netObj.CompareTag("Animatronic")) // or use a component check if you prefer
                continue;

            // Let the manager handle despawn, camera cycling, and gameover checks
            if (AnimatronicGameManager.Instance != null)
            {
                AnimatronicGameManager.Instance.KillAnimatronic(netObj);
            }

            // Shotgun: stop after first animatronic hit
            break;
        }

        PlayShotgunFxClientRpc();
    }

    [ClientRpc]
    private void PlayShotgunFxClientRpc()
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
}
