using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class RoleGatedUI : MonoBehaviour
{
    [Tooltip("What to show/hide. Defaults to this GameObject.")]
    [SerializeField] private GameObject target;

    [Header("Who should see this?")]
    [Tooltip("Visible on the Host (server + local client).")]
    [SerializeField] private bool showToHost = true;

    [Tooltip("Visible on remote clients only (NOT the host).")]
    [SerializeField] private bool showToRemoteClients = false;

    [Tooltip("Visible when Netcode hasn't started yet (useful for pre-lobby menus).")]
    [SerializeField] private bool showWhenNoNetwork = false;

    [Header("Hide behavior")]
    [Tooltip("If enabled, uses a CanvasGroup instead of SetActive to hide (keeps layout).")]
    [SerializeField] private bool useCanvasGroup = false;

    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (!target) target = gameObject;
        if (useCanvasGroup && !canvasGroup)
            canvasGroup = target.GetComponent<CanvasGroup>() ?? target.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        Refresh();

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // Refresh when the local role changes or connections happen
        nm.OnServerStarted += OnServerStarted;
        nm.OnClientConnectedCallback += OnClientChanged;
        nm.OnClientDisconnectCallback += OnClientChanged;
    }

    private void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnServerStarted -= OnServerStarted;
        nm.OnClientConnectedCallback -= OnClientChanged;
        nm.OnClientDisconnectCallback -= OnClientChanged;
    }

    private void OnServerStarted() => Refresh();
    private void OnClientChanged(ulong _) => Refresh();

    public void Refresh()
    {
        var nm = NetworkManager.Singleton;
        bool hasNet = nm && (nm.IsServer || nm.IsClient);

        bool isHost = nm && nm.IsHost;              // server + local client
        bool isRemoteClient = nm && nm.IsClient && !nm.IsHost;

        bool visible =
              (!hasNet && showWhenNoNetwork)
           || (isHost && showToHost)
           || (isRemoteClient && showToRemoteClients);

        ApplyVisibility(visible);
    }

    private void ApplyVisibility(bool visible)
    {
        if (useCanvasGroup && canvasGroup)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        else
        {
            target.SetActive(visible);
        }
    }

#if UNITY_EDITOR
    // Update live while tweaking in Play Mode
    private void OnValidate()
    {
        if (Application.isPlaying) Refresh();
    }
#endif
}
