// StunMovementAdapter.cs
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(StunState))]
public class StunMovementAdapter : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Behaviour movementComponent; // your movement script (MonoBehaviour)
    [SerializeField] private Rigidbody rb;                // optional

    private StunState _stun;

    private void Awake() => _stun = GetComponent<StunState>();

    public override void OnNetworkSpawn()
    {
        // Local-only: disable our own controls locally
        if (!IsOwner) return;

        _stun.StunStarted += OnStunStartLocal;
        _stun.StunEnded += OnStunEndLocal;

        // Apply current state on spawn (late joiners, etc.)
        if (_stun.IsStunned.Value) OnStunStartLocal();
        else OnStunEndLocal();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner || _stun == null) return;
        _stun.StunStarted -= OnStunStartLocal;
        _stun.StunEnded -= OnStunEndLocal;
    }

    private void OnStunStartLocal()
    {
        if (movementComponent) movementComponent.enabled = false;

        // Only legal to touch velocities on non-kinematic bodies.
        if (rb && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep(); // optional, clears residual motion
        }
        // Client-side kinematic proxies don't need velocity changes; disabling input is enough.
    }

    private void OnStunEndLocal()
    {
        if (movementComponent) movementComponent.enabled = true;
    }
}
