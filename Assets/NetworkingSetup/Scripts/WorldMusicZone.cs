using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Plays looping 3D audio from this object's position for all players.
/// You can have as many of these in the scene as you like.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WorldAudioZone : NetworkBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Auto-start the loop when this object spawns on the network.")]
    [SerializeField] private bool playOnNetworkStart = true;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            // Typical 3D loop setup
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            // spatialBlend, min/max distance, etc. set in inspector
        }
        else
        {
            Debug.LogWarning("WorldAudioZone: No AudioSource assigned.", this);
        }
    }

    public override void OnNetworkSpawn()
    {
        // Only the server decides when to start/stop
        if (IsServer && playOnNetworkStart)
        {
            PlayClientRpc();
        }
    }

    // -------- Public API (server-side) --------

    /// <summary>Server: start this zone's loop on all clients.</summary>
    [ContextMenu("Server Start Audio")]
    public void ServerStartAudio()
    {
        if (!IsServer) return;
        PlayClientRpc();
    }

    /// <summary>Server: stop this zone's loop on all clients.</summary>
    [ContextMenu("Server Stop Audio")]
    public void ServerStopAudio()
    {
        if (!IsServer) return;
        StopClientRpc();
    }

    /// <summary>
    /// Optional: let a client (button, trigger, etc.) request this zone to start.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestStartAudioServerRpc()
    {
        if (!IsServer) return;
        PlayClientRpc();
    }

    /// <summary>
    /// Optional: let a client request this zone to stop.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestStopAudioServerRpc()
    {
        if (!IsServer) return;
        StopClientRpc();
    }

    // -------- RPCs actually controlling audio on each client --------

    [ClientRpc]
    private void PlayClientRpc()
    {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    [ClientRpc]
    private void StopClientRpc()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
}
