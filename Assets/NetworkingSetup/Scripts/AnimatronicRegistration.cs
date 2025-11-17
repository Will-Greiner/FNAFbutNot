using Unity.Netcode;
using UnityEngine;

public class AnimatronicRegistration : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsServer && AnimatronicGameManager.Instance != null)
        {
            AnimatronicGameManager.Instance.RegisterAnimatronic(NetworkObject);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && AnimatronicGameManager.Instance != null)
        {
            AnimatronicGameManager.Instance.UnregisterAnimatronic(NetworkObject);
        }
    }
}
