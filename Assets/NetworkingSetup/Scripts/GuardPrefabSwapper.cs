using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GuardPrefabSwapper : NetworkBehaviour
{
    [SerializeField] private GameObject guardPrefab;
    [SerializeField] private Transform initialSpawnPoint;
    [SerializeField] private GameObject cleaningBotPrefab;
    [SerializeField] private List<Transform> botSpawnPoints = new();

    [Header("End Game")]
    private GameObject guardNormalUIParent;
    private GameObject guardEndGameUIParent;
    [Tooltip("Prefab to spawn when Stage 3 ends.")]
    [SerializeField] private GameObject shotgunPrefab;
    [Tooltip("Spawn point for the end-game prefab.")]
    [SerializeField] private Transform shotgunSpawnPoint;

    // Track spawn usage
    private readonly HashSet<int> usedSpawnIndices = new();
    private int currentSpawnIndex = -1; // -1 = in guard form / no bot

    // Simple singleton so bot health can call into this on the server
    public static GuardPrefabSwapper Instance { get; private set; }
    public ulong HostClientId => NetworkManager.Singleton.LocalClientId;

    private bool endGameTriggered = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterGuardUIParents(GameObject normalParent, GameObject endGameParent)
    {
        guardNormalUIParent = normalParent;
        guardEndGameUIParent = endGameParent;

        // Set initial visible state based on whether end game already happened.
        if (!endGameTriggered)
        {
            if (guardNormalUIParent) guardNormalUIParent.SetActive(true);
            if (guardEndGameUIParent) guardEndGameUIParent.SetActive(false);
        }
        else
        {
            if (guardNormalUIParent) guardNormalUIParent.SetActive(false);
            if (guardEndGameUIParent) guardEndGameUIParent.SetActive(true);
        }
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

        currentSpawnIndex = spawnIndex;

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

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(HostClientId, out var conn) || conn.PlayerObject == null)
        {
            Debug.LogWarning("Host PlayerObject not found in OnControlledBotDestroyed.");
            return;
        }

        var oldPlayer = conn.PlayerObject;

        // If it has BotHealth, reset UI before despawning
        var botHealth = oldPlayer.GetComponent<BotHealth>();
        if (botHealth != null)
        {
            botHealth.ResetHealthUI();
        }

        if (currentSpawnIndex >= 0)
        {
            usedSpawnIndices.Add(currentSpawnIndex); // this spawn can’t be used again
        }

        RespawnHostToOriginal();
    }

    public void TriggerEndGame()
    {
        if (!IsServer || !NetworkManager.Singleton.IsHost)
            return;

        if (endGameTriggered)
            return;

        endGameTriggered = true;

        // Ensure host is in guard form
        RespawnHostToOriginal();

        if (shotgunPrefab != null && shotgunSpawnPoint != null)
        {
            GameObject go = Instantiate(shotgunPrefab,
                                        shotgunSpawnPoint.position,
                                        shotgunSpawnPoint.rotation);

            var no = go.GetComponent<NetworkObject>();
            if (no != null)
                no.Spawn();  // networked so everyone sees it
        }

        // Switch UIs on all clients
        SetEndGameUIClientRpc();

        // Enable end-game movement on the guard's FPMovement
        EnableEndGameMovementOnGuard();
    }

    private void EnableEndGameMovementOnGuard()
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(HostClientId, out var conn) || conn.PlayerObject == null)
        {
            Debug.LogWarning("Host PlayerObject not found in EnableEndGameMovementOnGuard.");
            return;
        }

        var fp = conn.PlayerObject.GetComponent<FPMovement>();
        if (fp != null)
        {
            fp.isEndGame = true;
        }
    }

    [ClientRpc]
    private void SetEndGameUIClientRpc()
    {
        // This will be called after Stage 3 ends
        endGameTriggered = true;

        if (guardNormalUIParent)
            guardNormalUIParent.SetActive(false);

        if (guardEndGameUIParent)
            guardEndGameUIParent.SetActive(true);
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

        if (endGameTriggered)
        {
            var fp = go.GetComponent<FPMovement>();
            if (fp != null)
                fp.isEndGame = true;
        }
    }
}
