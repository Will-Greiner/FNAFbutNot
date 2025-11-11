using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GuardPrefabSwapper : NetworkBehaviour
{
    [SerializeField] private GameObject guardPrefab;
    [SerializeField] private Transform initialSpawnPoint;
    [SerializeField] private GameObject cleaningBotPrefab;
    [SerializeField] private List<Transform> botSpawnPoints = new();

    private ulong HostClientId => NetworkManager.Singleton.LocalClientId;

    private void Update()
    {
        if (IsServer && Input.GetButtonDown("Jump"))
            RespawnHostToOriginal();
    }

    public void HostSwapTo(int spawnIndex)
    {
        if (!IsServer || !NetworkManager.Singleton.IsHost)
            return;

        Transform spawnTransform = botSpawnPoints[spawnIndex];
        ServerRespawnPlayer(cleaningBotPrefab, spawnTransform.position, spawnTransform.rotation);
    }

    public void RespawnHostToOriginal()
    {
        if (!IsServer || !NetworkManager.Singleton.IsHost)
            return;
        ServerRespawnPlayer(guardPrefab, initialSpawnPoint.position, initialSpawnPoint.rotation);
    }

    private void ServerRespawnPlayer(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(HostClientId, out var conn) || conn.PlayerObject == null)
        {
            Debug.LogWarning("Host PlayerObject not found.");
            return;
        }

        var oldPlayer = conn.PlayerObject;
        oldPlayer.Despawn(true);

        GameObject go = Instantiate(prefab, position, rotation);
        var newNO = go.GetComponent<NetworkObject>();
        newNO.SpawnAsPlayerObject(HostClientId, destroyWithScene: true);
    }
}
