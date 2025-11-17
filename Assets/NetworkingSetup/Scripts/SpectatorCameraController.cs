using UnityEngine;

public class SpectatorCameraController : MonoBehaviour
{
    public static SpectatorCameraController Instance { get; private set; }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -4);

    private Transform _target;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void LateUpdate()
    {
        if (_target == null || cameraTransform == null) return;

        cameraTransform.position = _target.position + offset;
        cameraTransform.LookAt(_target);
    }
}
