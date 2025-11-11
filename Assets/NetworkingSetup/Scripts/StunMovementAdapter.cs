using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(StunState))]
public class StunMovementAdapter : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Behaviour movementComponent; // your movement script (MonoBehaviour)
    [SerializeField] private Rigidbody rb;                // optional

    private StunState _stun;

    private void Awake()
    {
        _stun = GetComponent<StunState>();
    }

    public override void OnNetworkSpawn()
    {
        // Local-only: we only disable our own controls locally
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
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // TODO: play stun VFX/UI here (e.g., screen flash)
    }

    private void OnStunEndLocal()
    {
        if (movementComponent) movementComponent.enabled = true;
        // TODO: stop VFX/UI
    }
}
