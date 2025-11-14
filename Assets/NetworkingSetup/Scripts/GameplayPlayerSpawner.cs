using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System;

public class GameplayPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private Transform[] gameplaySpawnPoints;
    [SerializeField] private NetworkObject hostGameplayPrefab;
    [SerializeField] private NetworkObject[] animatronicGameplayPrefabs;

    private int nextClientSpawnCursor = 0;

    private readonly Dictionary<ulong, int> clientToSpawnIndex = new();

    private NetworkManager nm;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        nm = NetworkManager.Singleton;
        if (nm == null) 
            return;

        //Hook into scene load completion for safety if this object exists before/through the load.
        if (nm.SceneManager != null)
        {
            // Unsubscribe from the events from last scene?
            nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            nm.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }

        nm.OnClientConnectedCallback -= OnClientConnected;
        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback -= OnClientDisconnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (nm != null)
        {
            if (nm.SceneManager != null)
                nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;

            nm.OnClientConnectedCallback -= OnClientConnected;
            nm.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // ---- Scene / Connection events ----

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer)
            return;


        Debug.Log($"[GameplayPlayerSpawner] OnLoadEventCompleted for scene {sceneName}. " + $"Completed: {string.Join(",", clientsCompleted)} | " + $"TimedOut: {string.Join(",", clientsTimedOut)}");

        // Only act in *gameplay* scene(s) where this spawner lives.
        // If you have multiple gameplay scenes, either place this component only in those scenes,
        // or extend this check with a scene name allowlist.
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
        {
            Debug.Log("[GameplayPlayerSpawner] Scene not valid/loaded on this object, skipping.");
            return;
        }

        // Reposition everyone to new spawnpoints
        foreach (var connectedClient in nm.ConnectedClientsList)
        {
            Debug.Log($"[GameplayPlayerSpawner] Spawning/repositioning client {connectedClient.ClientId}");
            SpawnOrReposition(connectedClient.ClientId, forceRespawn: false);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) 
            return;

        // Late joiners in gameplay scene get assigned and spawned here
        SpawnOrReposition(clientId, forceRespawn: false);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        clientToSpawnIndex.Remove(clientId);
    }

    // ---- Core logic ----
    private int AssignSpawnIndex(ulong clientId)
    {
        if (clientToSpawnIndex.TryGetValue(clientId, out var index))
            return index;

        bool isHost = nm.IsHost && clientId == nm.LocalClientId;
        if (isHost)
        {
            clientToSpawnIndex[clientId] = 0;
            return 0;
        }

        int assignedIndex = AllocateClientIndex();
        clientToSpawnIndex[clientId] = assignedIndex;
        return assignedIndex;
    }

    private int AllocateClientIndex()
    {
        if (gameplaySpawnPoints == null || gameplaySpawnPoints.Length == 0)
            return -1;

        int start = 1;
        int usable = Mathf.Max(0, gameplaySpawnPoints.Length - start);
        if (usable <= 0)
            return -1;

        int index = start + (nextClientSpawnCursor % usable);
        nextClientSpawnCursor++;
        return index;
    }

    private (Vector3 position, Quaternion rotation) PoseForIndex(int index)
    {
        if (index >= 0 && gameplaySpawnPoints != null && index < gameplaySpawnPoints.Length && gameplaySpawnPoints[index] != null)
        {
            var transform = gameplaySpawnPoints[index];
            return (transform.position, transform.rotation);
        }

        // Return world center if array is broken
        return (Vector3.zero, Quaternion.identity);
    }

    private NetworkObject PrefabFor(ulong clientId)
    {
        bool isHost = nm.IsHost && clientId == nm.LocalClientId;

        if (isHost)
            return hostGameplayPrefab;

        // Get lobby player selection
        int lobbyIndex = PlayerSelectionStore.GetChosenPrefabIndex(clientId);

        if (animatronicGameplayPrefabs == null && animatronicGameplayPrefabs.Length == 0)
            return null;

        lobbyIndex = Mathf.Clamp(lobbyIndex, 0, animatronicGameplayPrefabs.Length - 1);

        Debug.Log($"[GameplayPlayerSpawner] Using lobby index {lobbyIndex} for client {clientId}");


        return animatronicGameplayPrefabs[lobbyIndex];
    }

    private void SpawnOrReposition(ulong clientId, bool forceRespawn)
    {
        if (!IsServer)
            return;
        if (!nm.ConnectedClients.TryGetValue(clientId, out var connectedClient))
            return;

        int spawnIndex = AssignSpawnIndex(clientId);
        var (position, rotation) = PoseForIndex(spawnIndex);

        var currentPlayerObject = connectedClient.PlayerObject;
        var gameplayPrefab = PrefabFor(clientId);

        bool mustRespawn = gameplayPrefab != null;

        if (!mustRespawn)
        {
            // Just move them
            if (currentPlayerObject != null)
            {
                currentPlayerObject.transform.SetPositionAndRotation(position, rotation);
            }
            return;
        }

        // Replace player object
        if (currentPlayerObject != null)
            currentPlayerObject.Despawn(true);

        NetworkObject prefabToUse = gameplayPrefab;

        var newObject = Instantiate(prefabToUse, position, rotation);
        newObject.SpawnAsPlayerObject(clientId);
    }
}
