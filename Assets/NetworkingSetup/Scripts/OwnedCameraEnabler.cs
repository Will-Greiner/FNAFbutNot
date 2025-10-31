using UnityEngine;
using Unity.Netcode;

public class OwnedCameraEnabler : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    public override void OnNetworkSpawn()
    {
        ApplyOwnershipCameraState();
    }
    private void OnEnable()
    {
        if (IsSpawned)
            ApplyOwnershipCameraState();
    }

    public override void OnGainedOwnership()
    {
        ApplyOwnershipCameraState();
    }

    public override void OnLostOwnership()
    {
        ApplyOwnershipCameraState();
    }

    public override void OnNetworkDespawn()
    {
        playerCamera.enabled = false;
        audioListener.enabled = false;
    }

    private void ApplyOwnershipCameraState()
    {
        bool enable = IsOwner;
        if (playerCamera != null)
            playerCamera.enabled = enable;
        if (audioListener != null)
            audioListener.enabled = enable;
    }
}
