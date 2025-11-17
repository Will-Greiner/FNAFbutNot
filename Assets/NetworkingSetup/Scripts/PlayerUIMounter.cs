using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerUIMounter : NetworkBehaviour
{
    [Header("Assign your per-character UI prefab here")]
    [SerializeField] private GameObject uiPrefab;

    [Header("Where to put the UI")]
    [Tooltip("If empty, a UIRoot will be created under the player.")]
    [SerializeField] private Transform uiMountParent;
    [Tooltip("Spawn under the player (true) or under a top-level screen-space Canvas (false).")]
    [SerializeField] private bool parentUnderPlayer = true;

    [Header("Optional visibility filters")]
    [Tooltip("If true, only the Host will see this UI for their local player.")]
    [SerializeField] private bool onlyForHost = false;
    [Tooltip("If true, non-host clients will see this UI for their local player, host will not.")]
    [SerializeField] private bool onlyForNonHost = false;

    [Header("Safety")]
    [SerializeField] private bool forceActivateSpawnedUI = true;

    private GameObject _spawnedUiInstance;
    private static GameObject s_GlobalScreenCanvas; // reused when parentUnderPlayer == false

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        if (onlyForHost && !IsHost) return;
        if (onlyForNonHost && IsHost) return;
        if (uiPrefab == null) return;

        Transform parent = null;

        if (parentUnderPlayer)
        {
            parent = uiMountParent;
            if (parent == null)
            {
                var uiRoot = new GameObject($"UIRoot_{OwnerClientId}");
                uiRoot.transform.SetParent(transform, false);
                parent = uiRoot.transform;
            }
        }
        else
        {
            parent = GetOrCreateGlobalScreenCanvas().transform;
        }

        _spawnedUiInstance = Instantiate(uiPrefab, parent, false);

        if (forceActivateSpawnedUI)
            ActivateHierarchy(_spawnedUiInstance);

        AttachCanvasCameraIfAny(_spawnedUiInstance);
        EnsureEventSystemExists();

        WireAttackCooldownUI();
    }

    public override void OnNetworkDespawn()
    {
        // Clean up if we parented under the player
        if (parentUnderPlayer && _spawnedUiInstance != null)
        {
            Destroy(_spawnedUiInstance);
            _spawnedUiInstance = null;
        }
    }

    private GameObject GetOrCreateGlobalScreenCanvas()
    {
        if (s_GlobalScreenCanvas != null) return s_GlobalScreenCanvas;

        // Create a top-level Screen Space - Overlay canvas for local UI
        s_GlobalScreenCanvas = new GameObject("LocalScreenCanvas");
        var canvas = s_GlobalScreenCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // make sure it sits above default

        s_GlobalScreenCanvas.AddComponent<GraphicRaycaster>();
        return s_GlobalScreenCanvas;
    }

    private void ActivateHierarchy(GameObject root)
    {
        // ensure root and parents are active
        for (var t = root.transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
        }
        // canvases on
        foreach (var c in root.GetComponentsInChildren<Canvas>(true))
            c.enabled = true;

        // canvas groups interactive
        foreach (var g in root.GetComponentsInChildren<CanvasGroup>(true))
        {
            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }
    }

    private void AttachCanvasCameraIfAny(GameObject root)
    {
        var cam = Camera.main;
        if (!cam) return;

        var canvases = root.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.WorldSpace || c.renderMode == RenderMode.ScreenSpaceCamera)
                c.worldCamera = cam;
        }
    }

    private void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }
    }

    private void WireAttackCooldownUI()
    {
        if (_spawnedUiInstance == null) return;

        // Find any AttackCooldownUI components inside the spawned UI hierarchy
        var cooldownUIs = _spawnedUiInstance.GetComponentsInChildren<AttackCooldownUI>(true);
        if (cooldownUIs == null || cooldownUIs.Length == 0)
            return;

        // Find the attack script on THIS player
        // (PlayerUIMounter is on the same player / character object)
        MonoBehaviour attackSource = null;

        // Try AnimatronicAttack first
        var animAttack = GetComponentInChildren<AnimatronicAttack>(true);
        if (animAttack != null)
        {
            attackSource = animAttack;
        }
        else
        {
            // Fall back to BotAttack
            var botAttack = GetComponentInChildren<BotAttack>(true);
            if (botAttack != null)
            {
                attackSource = botAttack;
            }
        }

        if (attackSource == null)
        {
            Debug.LogWarning($"PlayerUIMounter: No AnimatronicAttack/BotAttack found on player {OwnerClientId} for cooldown UI.");
            return;
        }

        // Initialize all cooldown UI instances with this attack source
        foreach (var ui in cooldownUIs)
        {
            ui.InitializeFromAttack(attackSource);
        }
    }

}
