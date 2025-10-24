using UnityEngine;
using Unity.Netcode;

public class AnimatronicSelector : NetworkBehaviour
{
    public static AnimatronicSelector Local; // quick handle for UI
    private LobbyPlayerSpawner spawner;

    private void Start()
    {
        spawner = FindAnyObjectByType<LobbyPlayerSpawner>();

    }

    // Wire this to UI buttons
    public void RequestSwap(int index)
    {
        if (!IsOwner)
            return;

    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSwapServerRpc(int index, ServerRpcParams param = default)
    {
        var senderId = param.Receive.SenderClientId;
        var playerSpawner = spawner != null ? spawner : FindAnyObjectByType<LobbyPlayerSpawner>();
        playerSpawner?.SwapPlayerPrefab(senderId, index);
    }
}
