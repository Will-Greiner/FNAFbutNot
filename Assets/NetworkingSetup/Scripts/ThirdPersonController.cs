//using UnityEngine;
//using Unity.Netcode;

//public class TestMovement : NetworkBehaviour
//{
//    [Header("Movement")]
//    [SerializeField] private float speed = 5;
//    [SerializeField] private float rotateSpeed = 300;

//    [Header("Refs")]
//    [SerializeField] private Transform forwardRef;

//    private void Update()
//    {
//        float vertical = Input.GetAxisRaw("Vertical");
//        float horizontal = Input.GetAxisRaw("Horizontal");

//        //if (Mathf.Abs(horizontal) > 0.15f)
//        //    transform.Rotate(transform.up *  horizontal * 600 * Time.deltaTime);

//        if (Mathf.Abs(vertical) > 0.15f | Mathf.Abs(horizontal) > 0.15f)
//        {
//            Vector3 moveDir = forwardRef.right * horizontal + forwardRef.forward * vertical;
//            moveDir.y = 0;
//            moveDir.Normalize();

//            transform.Translate(moveDir * speed * Time.deltaTime, Space.World);

//            if (moveDir != Vector3.zero)
//            {
//                Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);

//                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
//            }
//        }
//    }
//}

using UnityEngine;
using Unity.Netcode;

public class ThirdPersonController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotateSpeed = 300f;

    [Header("Refs")]
    [SerializeField] private Transform forwardRef; // camera boom/pivot
    [SerializeField] private Transform camFollow;
    [SerializeField] private Animator animator;

    private Rigidbody rb;

    // Server state
    Vector3 lastMoveDir = Vector3.zero;

    // Client send throttle
    private float sendInterval = 1f / 60f; // ~60 Hz input; raise to 1/30f to save bandwidth
    private float sendTimer = 0f;

    private OrbitCamera _localOrbit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Rigidbody config for character-style motion
        rb.constraints = RigidbodyConstraints.FreezeRotation;   // prevent physics tipping
        rb.interpolation = RigidbodyInterpolation.Interpolate;  // smoother visuals
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // better against fast hits
    }

    public override void OnNetworkSpawn()
    {
        // Server simulates; clients are kinematic followers of NetworkTransform (server authority)
        rb.isKinematic = !IsServer;

        if (IsOwner)
        {
            BindLocalCameraRig();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Only server should drive Animator state; clients just receive via NetworkAnimator
        if (!IsServer && animator) animator.applyRootMotion = false;
    }
    public override void OnNetworkDespawn()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void BindLocalCameraRig()
    {
        if (_localOrbit == null)
            _localOrbit = FindFirstObjectByType<OrbitCamera>();

        if (_localOrbit != null)
        {
            // Always set (or re-set) the follow target
            _localOrbit.SetFollowTarget(camFollow);

            // Only set forwardRef if we don't already have one.
            if (forwardRef == null)
                forwardRef = _localOrbit.ForwardReference;
        }
        else if (forwardRef == null && Camera.main != null)
        {
            // Fallback: still drive movement even if the rig isn't found yet.
            forwardRef = Camera.main.transform;
        }
    }

    void Update()
    {
        if (!IsOwner) return; // NGO: only the local owner drives input

        BindLocalCameraRig();

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        Vector3 moveDir;

        // Camera-aligned input
        Vector3 fwd = forwardRef.forward; 
        fwd.y = 0f; 
        fwd.Normalize();
        Vector3 right = forwardRef.right; 
        right.y = 0f; 
        right.Normalize();
        moveDir = right * horizontal + fwd * vertical;

        // Clamp diagonals and tiny drift
        if (moveDir.sqrMagnitude > 1)
            moveDir.Normalize();

        // Send to server at a reasonable rate
        sendTimer += Time.deltaTime;
        if (sendTimer >= sendInterval)
        {
            sendTimer = 0f;
            SendInputServerRpc(new Vector2(moveDir.x, moveDir.z));
        }
    }

    [ServerRpc]
    private void SendInputServerRpc(Vector2 moveXZ)
    {
        var w = new Vector3(moveXZ.x, 0, moveXZ.y);
        if (w.sqrMagnitude > 1)
            w.Normalize();
        lastMoveDir = w;
    }

    void FixedUpdate()
    {
        if (!IsServer) return;

        // --- Move with physics on the server ---

        Vector3 delta = lastMoveDir.sqrMagnitude > 1e-4f ? lastMoveDir * (speed * Time.fixedDeltaTime) : Vector3.zero;

        rb.MovePosition(rb.position + delta);

        // --- Face movement direction ---
        if (lastMoveDir.sqrMagnitude > 1e-6f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lastMoveDir, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime));
        }

        bool isMoving = lastMoveDir.sqrMagnitude > 1e-4f;
        animator.SetBool("isMoving", isMoving);
    }
}
