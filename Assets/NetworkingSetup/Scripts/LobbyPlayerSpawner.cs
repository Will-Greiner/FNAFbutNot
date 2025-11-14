using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class LobbyPlayerSpawner : NetworkBehaviour
{
    [Serializable]
    public class SessionData
    {
        public int chosenPrefabIndex; // which animatronic is picked
        public int spawnIndex; // which spawn point they get
    }

    private readonly Dictionary<ulong, SessionData> sessionData = new();

    [SerializeField] NetworkObject hostPrefab;
    [SerializeField] NetworkObject[] animatronicPrefabs;
    [SerializeField] Transform[] playerSpawnPoints;

    private int nextSpawnIndex = 0;

    NetworkManager nm;

    public static LobbyPlayerSpawner Instance { get; private set; }

    private void OnEnable()
    {
        Instance = this;
    }

    private void Awake()
    {
        StartCoroutine(Init());
    }
    private IEnumerator Init()
    {
        //Wait until NetworkManager exists
        while (NetworkManager.Singleton == null)
            yield return null;

        nm = NetworkManager.Singleton;
        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback += OnConnectionApproval;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
        nm.OnClientConnectedCallback += OnClientConnected;
    }


    public override void OnDestroy()
    {
        if (nm)
        {
            nm.ConnectionApprovalCallback -= OnConnectionApproval;
            nm.OnClientDisconnectCallback -= OnClientDisconnected;
            nm.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // ---- Utilities ----

    private SessionData GetOrCreate(ulong clientId)
    {
        //Create data
        if (!sessionData.TryGetValue(clientId, out var data))
        {
            data = new SessionData
            {
                chosenPrefabIndex = 0,
                spawnIndex = -1,
            };
            sessionData[clientId] = data;

            // Store data
            PlayerSelectionStore.SetChosenPrefabIndex(clientId, data.chosenPrefabIndex);
        }
        // Get existing data
        return data;
    }

    private int AllocateSpawnIndex(bool reserveZeroForHost)
    {
        // round-robin, optionally skipping index 0 (for host)
        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            return -1;
        int start = reserveZeroForHost ? 1 : 0;
        int usable = Mathf.Max(1, playerSpawnPoints.Length - start);

        if (usable <= 0)
            return -1;

        int allocatedSpawnIndex = start + (nextSpawnIndex++ % usable);
        return allocatedSpawnIndex;
    }

    private (Vector3 position, Quaternion rotation) PoseForIndex(int index)
    {
        if (playerSpawnPoints != null && index >= 0 && index < playerSpawnPoints.Length)
        {
            Transform transform = playerSpawnPoints[index];
            return (transform.position, transform.rotation);
        }
        return (Vector3.zero, Quaternion.identity);
    }

    private NetworkObject PrefabFor(ulong clientId, bool hostClient)
    {
        if (hostClient)
            return hostPrefab;

        var data = GetOrCreate(clientId);
        data.chosenPrefabIndex = Mathf.Clamp(data.chosenPrefabIndex, 0, Mathf.Max(0, animatronicPrefabs.Length - 1));
        return animatronicPrefabs[data.chosenPrefabIndex];
    }

    // Initial spawn decision (per-join)
    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Default approval; we spawn manually
        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!nm.IsServer)
            return;

        bool isHost = nm.IsHost && clientId == nm.LocalClientId;
        var data = GetOrCreate(clientId);

        // Set spawn point
        if (data.spawnIndex < 0)
            data.spawnIndex = isHost ? 0 : AllocateSpawnIndex(true);

        var (pos, rot) = PoseForIndex(data.spawnIndex); //retrieve the position and rotation from the playerSpawnPoints[i] 
        var prefab = PrefabFor(clientId, isHost);  // retrieve the correct prefab for client/host

        var playerObject = Instantiate(prefab, pos, rot);
        playerObject.SpawnAsPlayerObject(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        sessionData.Remove(clientId);
        PlayerSelectionStore.Clear(clientId);
    }

    /// <summary>
    /// Server-side swap: despawn current player object and spawn a new one
    /// from animatronicPrefabs[selectedIndex], preserving position/rotation.
    /// </summary>
    
    public void SwapPlayerPrefab(ulong clientId, int selectedIndex)
    {
        // Only let the server handle the spawns
        if (!nm.IsServer)
            return;

        // Host doesn't have a choice
        if (clientId == nm.LocalClientId && nm.IsHost)
            return;

        if (selectedIndex < 0 || selectedIndex >= animatronicPrefabs.Length)
            return;

        var client = nm.ConnectedClients[clientId];
        var oldPlayerObject = client.PlayerObject;
        if (oldPlayerObject == null)
            return;

        // Update session choice
        var data = GetOrCreate(clientId);
        data.chosenPrefabIndex = selectedIndex;

        // Persist across scenes
        PlayerSelectionStore.SetChosenPrefabIndex(clientId, selectedIndex);

        // Cache transform before despawn
        Vector3 position = oldPlayerObject.transform.position;
        Quaternion rotation = oldPlayerObject.transform.rotation;

        // Despawn the existing player object
        oldPlayerObject.Despawn(true);

        //Spawn the new prefab as the player's object
        var newPlayerObject = Instantiate(animatronicPrefabs[selectedIndex], position, rotation);
        newPlayerObject.SpawnAsPlayerObject(clientId);
    }

    public void SwapToIndex(int index)
    {
        Debug.Log($"[UI] Requesting swap to {index} (client={nm?.IsClient})");
        if (nm == null || !nm.IsClient) 
            return;
        RequestSwapServerRpc(index);
    }

    public int GetChosenPrefabIndex(ulong clientId)
    {
        // falls back to 0 if not set
        var data = GetOrCreate(clientId);
        return Mathf.Clamp(data.chosenPrefabIndex, 0, Mathf.Max(0, animatronicPrefabs.Length - 1));
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSwapServerRpc(int index, ServerRpcParams param = default)
    {
        var senderId = param.Receive.SenderClientId;
        SwapPlayerPrefab(senderId, index);
    }

    // Client UI calls this
    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGameServerRpc(string sceneName)
    {
        if (nm == null || nm.SceneManager == null)
            return;

        Debug.Log($"[Server] Loading gameplay scene '{sceneName}' for {nm.ConnectedClientsList.Count} clients");
        nm.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
