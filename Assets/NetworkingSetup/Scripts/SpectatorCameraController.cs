using UnityEngine;

public class SpectatorCameraController : MonoBehaviour
{
    public static SpectatorCameraController Instance { get; private set; }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Camera spectatorCamera;
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -4);

    [Header("UI")]
    [SerializeField] private GameObject spectatorUiPrefab;
    private GameObject uiInstance;

    private Transform _target;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SpawnAndAttachUI();
    }

    public void SpawnAndAttachUI()
    {
        if (spectatorUiPrefab == null || spectatorCamera == null)
        {
            Debug.LogWarning("SpectatorCameraController: Missing spectatorUiPrefab or spectatorCamera.");
            return;
        }

        if (uiInstance != null)
            return; // already spawned

        uiInstance = Instantiate(spectatorUiPrefab);
        uiInstance.name = "SpectatorUI";

        // Find the Canvas inside the prefab and point it at the spectator camera
        var canvas = uiInstance.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = spectatorCamera;
        }
        else
        {
            Debug.LogWarning("SpectatorCameraController: Spectator UI prefab has no Canvas.");
        }

        // Optionally: make sure it survives scene reload while spectating
        // DontDestroyOnLoad(_uiInstance);
    }

    public void ShowSpectatorUI(bool active)
    {
        if (uiInstance == null) return;

        var uiRoot = uiInstance.GetComponentInChildren<SpectatorUIRoot>(true);
        if (uiRoot != null)
        {
            uiRoot.SetSpectating(active);
        }
    }


    public void SetTarget(Transform target)
    {
        _target = target;

        if (spectatorCamera != null)
            UICameraManager.SetCamera(spectatorCamera);
    }

    private void LateUpdate()
    {
        if (_target == null || cameraTransform == null) return;

        cameraTransform.position = _target.position + offset;
        cameraTransform.LookAt(_target);
    }
}
