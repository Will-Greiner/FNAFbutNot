using UnityEngine;
using Unity.Netcode;
using System;

public class StunState : NetworkBehaviour
{
    // True when stunned (replicated to everyone)
    public readonly NetworkVariable<bool> IsStunned = new(writePerm: NetworkVariableWritePermission.Server);

    // Server side timer
    private double stunEndsAtServerTime = 0;

    public event Action StunStarted;
    public event Action StunEnded;

    private void Awake()
    {
        IsStunned.OnValueChanged += OnStunChanged;
    }

    public override void OnNetworkDespawn()
    {
        IsStunned.OnValueChanged -= OnStunChanged;
    }

    // Call on server
    public void ApplyStun(float seconds)
    {
        if (!IsServer)
            return;

        double now = NetworkManager.ServerTime.Time;

        double newEnds = now + Mathf.Max(0, seconds);
        if (!IsStunned.Value || newEnds > stunEndsAtServerTime)
        {
            stunEndsAtServerTime = newEnds;
            if (!IsStunned.Value)
            {
                IsStunned.Value = true;
                StunStartClientRpc();
            }
        }
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (IsStunned.Value)
        {
            double now = NetworkManager.ServerTime.Time;
            if (now >= stunEndsAtServerTime)
            {
                IsStunned.Value = false;
                StunEndClientRpc();
            }
        }
    }

    private void OnStunChanged(bool oldVal, bool newVal)
    {
        if (newVal)
            StunStarted?.Invoke();
        else
            StunEnded?.Invoke();
    }

    [ClientRpc]
    private void StunStartClientRpc()
    {
        /* hook for client-side FX */
    }

    [ClientRpc]
    private void StunEndClientRpc()
    {
        /* hook for client-side FX */
    }
}
