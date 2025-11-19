using UnityEngine;

public class UICameraManager : MonoBehaviour
{
    public static UICameraManager Instance { get; private set; }

    [SerializeField] private Camera initialCamera;
    private Camera currentCamera;

    public static Camera CurrentCamera => Instance ? Instance.currentCamera : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentCamera = initialCamera != null ? initialCamera : Camera.main;
    }

    public static void SetCamera(Camera cam)
    {
        if (Instance == null) return;
        Instance.currentCamera = cam;
    }
}
