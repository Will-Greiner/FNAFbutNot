using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GuardPrefabSwapper : NetworkBehaviour
{
    [SerializeField] private GameObject guardPrefab;
    [SerializeField] private Transform initialSpawnPoint;
    [SerializeField] private GameObject cleaningBotPrefab;
    [SerializeField] private List<Transform> botSpawnPoints = new();

    // Track spawn usage
    private readonly HashSet<int> usedSpawnIndices = new();
    private int currentSpawnIndex = -1; // -1 = in guard form / no bot

    // Simple singleton so bot health can call into this on the server
    public static GuardPrefabSwapper Instance { get; private set; }
    public ulong HostClientId => NetworkManager.Singleton.LocalClientId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (IsServer && Input.GetButtonDown("Jump"))
            RespawnHostToOriginal();
    }

    public bool IsSpawnAvailable(int spawnIndex)
    {
        return spawnIndex >= 0
            && spawnIndex < botSpawnPoints.Count
            && !usedSpawnIndices.Contains(spawnIndex);
    }

    public void HostSwapTo(int spawnIndex)
    {
        if (!IsServer || !NetworkManager.Singleton.IsHost)
            return;


        if (!IsSpawnAvailable(spawnIndex))
        {
            Debug.LogWarning($"Spawn index {spawnIndex} is not available (already used or out of range).");
            return;
        }

        Transform spawnTransform = botSpawnPoints[spawnIndex];
        ServerRespawnPlayer(cleaningBotPrefab, spawnTransform.position, spawnTransform.rotation);
    }

    public void RespawnHostToOriginal()
    {
        if (!IsServer || !NetworkManager.Singleton.IsHost)
            return;

        currentSpawnIndex = -1;

        ServerRespawnPlayer(guardPrefab, initialSpawnPoint.position, initialSpawnPoint.rotation);
    }

    public void OnControlledBotDestroyed()
    {
        if (!IsServer || !NetworkManager.Singleton.IsHost)
            return;

        if (currentSpawnIndex >= 0)
        {
            usedSpawnIndices.Add(currentSpawnIndex); // this spawn can’t be used again
        }

        RespawnHostToOriginal();
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
