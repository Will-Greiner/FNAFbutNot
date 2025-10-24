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
    private Transform[] spawnPoints; // active scene spawn set

    NetworkManager nm;
    bool isHost;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
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

        // Subscribe to networked scene load completion
        if (nm.SceneManager != null)
            nm.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

        // Prime spawn points for current scene
        RefreshSceneSpawnPoints();
    }


    public override void OnDestroy()
    {
        if (nm)
        {
            nm.ConnectionApprovalCallback -= OnConnectionApproval;
            nm.OnClientDisconnectCallback -= OnClientDisconnected;
            nm.OnClientConnectedCallback -= OnClientConnected;

            if (nm.SceneManager != null)
                nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }
    }

    // ---- Utilities ----

    private void RefreshSceneSpawnPoints()
    {
        var sceneSet = SceneSpawnPoints.Get();
        spawnPoints = (sceneSet != null && sceneSet.Length > 0) ? sceneSet : playerSpawnPoints;
        // reset round-robin when entering a new scene 
        nextSpawnIndex = 0;

        // Clear per-scene spawn indices so each scene can assign afresh
        foreach (var kv in sessionData)
            kv.Value.spawnIndex = -1;
    }

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
        }
        // Get existing data
        return data;
    }

    private int AllocateSpawnIndex(bool reserveZeroForHost)
    {
        // round-robin, optionally skipping index 0 (for host)
        if (spawnPoints == null || spawnPoints.Length == 0)
            return -1;
        int start = reserveZeroForHost ? 1 : 0;
        int usable = Mathf.Max(1, spawnPoints.Length - start);
        int allocatedSpawnIndex = start + (nextSpawnIndex++ % usable);
        return allocatedSpawnIndex;
    }

    private (Vector3 position, Quaternion rotation) PoseForIndex(int index)
    {
        if (spawnPoints != null && index >= 0 && index < spawnPoints.Length)
        {
            Transform transform = spawnPoints[index];
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
        // Default approval
        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!nm.IsServer)
            return;

        isHost = nm.IsHost && clientId == nm.LocalClientId;
        var data = GetOrCreate(clientId);

        // Set spawn point
        if (data.spawnIndex < 0)
            data.spawnIndex = isHost ? 0 : AllocateSpawnIndex(true);

        //Vector3 spawnPosition = Vector3.zero;
        //Quaternion spawnRotation = Quaternion.identity;
        //if (data.spawnIndex >= 0 && data.spawnIndex < playerSpawnPoints.Length)
        //{
        //    Transform spawnTransform = playerSpawnPoints[data.spawnIndex];
        //    spawnPosition = spawnTransform.position;
        //    spawnRotation = spawnTransform.rotation;
        //}

        var (pos, rot) = PoseForIndex(data.spawnIndex);
        var prefab = PrefabFor(clientId, isHost);

        //// Choose prefab
        //NetworkObject prefab;
        //if (isHost)
        //{
        //    prefab = hostPrefab;
        //}
        //else
        //{
        //    data.chosenPrefabIndex = Mathf.Clamp(data.chosenPrefabIndex, 0, animatronicPrefabs.Length - 1);
        //    prefab = animatronicPrefabs[data.chosenPrefabIndex];
        //}

        // Manually create the PlayerObject for this client
        // var playerObject = Instantiate(prefab, spawnPosition, spawnRotation);
        var playerObject = Instantiate(prefab, pos, rot);
        playerObject.SpawnAsPlayerObject(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        sessionData.Remove(clientId);
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
        nm.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // Called when the networked scene finished loading on server + all clients that completed
    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer)
            return;

        // Make the loaded scene active (safety)
        var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        if (loaded.IsValid() && loaded.isLoaded)
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(loaded);

        // Pull spawn points for the new scene ONLY (no silent lobby fallback)
        spawnPoints = SceneSpawnPoints.Get(sceneName);

        foreach (var clientId in clientsCompleted)
        {
            if (!nm.ConnectedClients.TryGetValue(clientId, out var connectedClient))
                continue;
            var hostClient = nm.IsHost && clientId == nm.LocalClientId;
            var data = GetOrCreate(clientId);

            if (data.spawnIndex < 0)
                data.spawnIndex = hostClient ? 0 : AllocateSpawnIndex(true);

            var (pos, rot) = PoseForIndex(data.spawnIndex);
            var prefab = PrefabFor(clientId, hostClient);

            // Replace whatever PlayerObject exists (or create new if null)
            if (connectedClient?.PlayerObject != null)
                connectedClient.PlayerObject.Despawn(true);

            var newObj = Instantiate(prefab, pos, rot);
            newObj.SpawnAsPlayerObject(clientId);
        }

    }
}
