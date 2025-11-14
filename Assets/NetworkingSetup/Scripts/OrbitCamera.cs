using UnityEngine;

[DefaultExecutionOrder(-100)]
public class OrbitCamera : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private Vector2 sensitivity = new Vector2(1000, 1000);
    [SerializeField] private float yaw = 0f;   // degrees
    [SerializeField] private float pitch = 15f; // degrees
    [SerializeField] private Vector2 minMaxPitch = new Vector2(-30, 70);

    [Header("Rig")]
    [SerializeField] private Transform cameraTransform;  // the actual camera
    [Tooltip("Layers considered 'solid' for the camera. Exclude the player layer.")]
    [SerializeField] private LayerMask obstructionMask = ~0; // everything by default

    [Header("Collision")]
    [Tooltip("Desired distance from pivot when unobstructed.")]
    [SerializeField] private float maxDistance = 4.0f;
    [Tooltip("How close the camera is allowed to get to the pivot.")]
    [SerializeField] private float minDistance = 0.2f;
    [Tooltip("Radius used for the sphere cast (treat camera like a ball).")]
    [SerializeField] private float collisionRadius = 0.25f;
    [Tooltip("Extra space to keep off walls.")]
    [SerializeField] private float wallBuffer = 0.05f;

    [Header("Smoothing")]
    [Tooltip("Speed when pushing INTO obstacles.")]
    [SerializeField] private float pushInSpeed = 30f;
    [Tooltip("Speed when returning back OUT to max distance.")]
    [SerializeField] private float returnSpeed = 8f;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;

    private float currentDistance;
    private Camera cam;
    private AudioListener audioListener;

    void Awake()
    {
        if (cameraTransform)
            currentDistance = Mathf.Clamp(Vector3.Distance(transform.position, cameraTransform.position), minDistance, maxDistance);
        else
            currentDistance = maxDistance;

        cam = cameraTransform.GetComponent<Camera>();
        audioListener = cameraTransform.GetComponent<AudioListener>();
    }

    private void OnEnable()
    {
        if (cam) cam.enabled = true;
        if (audioListener) audioListener.enabled = true;
    }

    public void SetFollowTarget(Transform t) => followTarget = t;

    // The controller will use this to read "camera forward" each frame.
    public Transform ForwardReference => transform;

    void Update()
    {
        float inputX = Input.GetAxisRaw("Mouse X") * sensitivity.x * Time.deltaTime;
        float inputY = Input.GetAxisRaw("Mouse Y") * sensitivity.y * Time.deltaTime;
        yaw += inputX;
        pitch -= inputY;
        pitch = Mathf.Clamp(pitch, minMaxPitch.x, minMaxPitch.y);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    void LateUpdate()
    {
        if (!cameraTransform) 
            return;

        if (followTarget)
            transform.position = followTarget.position;

        // Follow the player root without parenting
        Vector3 pivot = followTarget ? followTarget.position : transform.position;

        // Where we WANT the camera (unobstructed)
        Vector3 desiredPos = pivot - transform.forward * maxDistance;

        // SphereCast from pivot toward desired position to find obstructions
        Vector3 dir = (desiredPos - pivot).normalized;
        float targetDistance = maxDistance;

        if (Physics.SphereCast(
                pivot,
                collisionRadius,
                dir,
                out RaycastHit hit,
                maxDistance,
                obstructionMask,
                QueryTriggerInteraction.Ignore))
        {
            // Stop just in front of the surface, with a small buffer
            targetDistance = Mathf.Clamp(hit.distance - wallBuffer, minDistance, maxDistance);
        }

        // Smooth distance change (faster when pushing in, slower when returning)
        float speed = (targetDistance < currentDistance) ? pushInSpeed : returnSpeed;
        currentDistance = Mathf.MoveTowards(currentDistance, targetDistance, speed * Time.deltaTime);

        // Apply final camera position/rotation
        cameraTransform.position = pivot - transform.forward * currentDistance;
        cameraTransform.rotation = transform.rotation; // keep camera aligned to rig's yaw/pitch
    }
}
