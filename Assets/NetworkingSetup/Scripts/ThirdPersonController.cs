using UnityEngine;
using Unity.Netcode;

public class ThirdPersonController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotateSpeed = 600f;

    [Header("Refs")]
    [SerializeField] private Transform forwardRef; // camera rig / boom (OrbitCamera transform)
    [SerializeField] private Transform camFollow;  // pivot on the player the camera orbits
    [SerializeField] private Animator animator;

    private Rigidbody rb;

    // Server-side movement direction (world space, XZ only)
    private Vector3 lastMoveDir = Vector3.zero;

    // Client send throttle
    private float sendInterval = 1f / 60f;
    private float sendTimer = 0f;

    private OrbitCamera _localOrbit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Rigidbody config for character-style motion
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public override void OnNetworkSpawn()
    {
        // Server simulates; clients are kinematic followers (via NetworkTransform)
        rb.isKinematic = !IsServer;

        if (IsOwner)
        {
            BindLocalCameraRig();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Server drives Animator; clients get it via NetworkAnimator
        if (!IsServer && animator) animator.applyRootMotion = false;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Finds the local OrbitCamera at runtime and wires follow + forwardRef
    private void BindLocalCameraRig()
    {
        if (_localOrbit == null)
            _localOrbit = FindFirstObjectByType<OrbitCamera>();

        if (_localOrbit != null)
        {
            // Ensure camera rig is following THIS player
            if (camFollow != null)
                _localOrbit.SetFollowTarget(camFollow);

            // Movement forward reference = camera rig transform
            forwardRef = _localOrbit.ForwardReference;
        }
        else if (forwardRef == null && Camera.main != null)
        {
            // Fallback: still move relative to main camera if rig isn't found yet
            forwardRef = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (!IsOwner || GameOverUIController.IsGameOver)
            return;

        BindLocalCameraRig();

        // --- Read input on the local client ---
        float vertical = Input.GetAxisRaw("Vertical");   // W/S
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D

        // --- Build camera-aligned movement vector in world space ---
        Vector3 moveDir = Vector3.zero;

        if (forwardRef != null)
        {
            // Camera rig forward/right flattened on XZ
            Vector3 fwd = forwardRef.forward;
            fwd.y = 0f;
            fwd.Normalize();

            Vector3 right = forwardRef.right;
            right.y = 0f;
            right.Normalize();

            // WASD relative to camera rig
            moveDir = right * horizontal + fwd * vertical;
        }

        // Clamp diagonals
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        // Throttle input sends to the server
        sendTimer += Time.deltaTime;
        if (sendTimer >= sendInterval)
        {
            sendTimer = 0f;
            // Send world-space XZ components to server
            SendInputServerRpc(new Vector2(moveDir.x, moveDir.z));
        }
    }

    [ServerRpc]
    private void SendInputServerRpc(Vector2 moveXZ)
    {
        // Rebuild world-space vector on the server
        Vector3 w = new Vector3(moveXZ.x, 0f, moveXZ.y);

        if (w.sqrMagnitude > 1f)
            w.Normalize();

        lastMoveDir = w;
    }

    private void FixedUpdate()
    {
        if (!IsServer || GameOverUIController.IsGameOver)
            return;

        // --- Move with physics on the server ---
        Vector3 delta = lastMoveDir.sqrMagnitude > 1e-4f
            ? lastMoveDir * (speed * Time.fixedDeltaTime)
            : Vector3.zero;

        rb.MovePosition(rb.position + delta);

        // --- Rotate to face movement direction ---
        if (lastMoveDir.sqrMagnitude > 1e-6f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lastMoveDir, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(
                rb.rotation,
                targetRot,
                rotateSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(newRot);
        }

        // --- Animator flag ---
        bool isMoving = lastMoveDir.sqrMagnitude > 1e-4f;
        if (animator != null)
            animator.SetBool("isMoving", isMoving);
    }
}
