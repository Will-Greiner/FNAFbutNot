using UnityEngine;
using Unity.Netcode;
using System;

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

    [Header("Audio & VFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireClip;
    [SerializeField] private AudioClip reloadClip;      
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem smoke;

    [SerializeField] private Animator animator;

    private float cooldownLocal;
    public float AttackCooldown => attackCooldown;
    public event System.Action LocalAttackFired;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    private static readonly Collider[] s_overlapCache = new Collider[16];

    private void Reset()
    {
        attackOrigin = transform;
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

            RequestShotgunFxServerRpc();
        }
    }

    public void AnimationAttackHit()
    {
        if (!IsOwner)
            return;

        Vector3 originPos = attackOrigin ? attackOrigin.position : transform.position;
        Vector3 forward = attackOrigin ? attackOrigin.forward : transform.forward;

        TryShotgunServerRpc(originPos, forward);
    }

    public void AnimationAttackEnd()
    {
        if (!IsOwner)
            return;

        isAttacking = false;

        EndFxClientRpc();
    }


    public void PlayFireSound()
    {
        if (audioSource != null && fireClip != null)
            audioSource.PlayOneShot(fireClip);

        if (muzzleFlash != null)
        {
            muzzleFlash.gameObject.SetActive(true);
            muzzleFlash.Play();
            smoke.gameObject.SetActive(true);
            smoke.Play();
        }
    }

    public void PlayReloadSound()
    {
        if (audioSource != null && reloadClip != null)
            audioSource.PlayOneShot(reloadClip);
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

            if (!netObj.CompareTag("Animatronic"))
                continue;

            if (AnimatronicGameManager.Instance != null)
            {
                AnimatronicGameManager.Instance.KillAnimatronic(netObj);
            }

            break;
        }
    }

    [ClientRpc]
    private void PlayShotgunFxClientRpc()
    {
        if (animator != null)
            animator.SetTrigger("attack");
    }

    [ClientRpc]
    private void EndFxClientRpc()
    {
        muzzleFlash.gameObject.SetActive(false);
        smoke.gameObject.SetActive(false);
    }

    [ServerRpc]
    private void RequestShotgunFxServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        if (!CanServerAttack())
            return;
        SetServerCooldown();

        PlayShotgunFxClientRpc();
    }

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
