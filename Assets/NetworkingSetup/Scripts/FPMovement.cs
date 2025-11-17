using UnityEngine;
using Unity.Netcode;
using System;

public class FPMovement : NetworkBehaviour
{
    [Header("Look")]
    [SerializeField] private Vector2 sensitivity = new Vector2(800,800);
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float minPitch = -90;
    [SerializeField] private float maxPitch = 90;

    [Header("Move")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxVelocity = 6;

    private Rigidbody playerRigidbody;
    [SerializeField] private Animator animator;
    [SerializeField] private Shotgun shotgunScript;
    [SerializeField] private GameObject shotgunPrefab;

    public bool isEndGame = false;
    public bool showGun = false;

    // local client look state
    private float xRotation; // pitch
    private float yRotation; // yaw

    // last input received by server
    private InputState lastInput;

    // payload for RPC
    public struct InputState : INetworkSerializable
    {
        public float moveX;
        public float moveZ;
        public float yaw; // absolute yaw in degrees
        public float pitch; // absolute pitch in degrees

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref moveX);
            serializer.SerializeValue(ref moveZ);
            serializer.SerializeValue(ref yaw);
            serializer.SerializeValue(ref pitch);
        }
    }

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public override void OnNetworkSpawn()
    {
        // Server simulates physics clients don't
        playerRigidbody.isKinematic = !IsServer;

        if (IsOwner)
        {
            LockCursor(false);

            // initialize look from current transforms
            if (cameraTransform != null)
                xRotation = NormalizeAngle(cameraTransform.localEulerAngles.x);
            yRotation = transform.eulerAngles.y;
        }
        else
        {
            // No local camera for non-owners
            if (cameraTransform != null)
                cameraTransform.gameObject.SetActive(false);
        }

        if (!IsServer && animator) animator.applyRootMotion = false;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            LockCursor(false);
    }

    private void Update()
    {
        if (!IsOwner || !isEndGame || GameOverUIController.IsGameOver)
            return;

        if (isEndGame)
        {
            if (showGun)
            {
                shotgunPrefab.SetActive(true);
                shotgunScript.enabled = true;
            }
            animator.SetBool("hasGun", true);
        }

        // Client look
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensitivity.x;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensitivity.y;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.rotation = Quaternion.Euler(0, yRotation, 0);

        // Client move input
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        var input = new InputState
        { 
            moveX = Mathf.Clamp(inputX, -1, 1),
            moveZ = Mathf.Clamp(inputZ, -1, 1),
            yaw = yRotation,
            pitch = xRotation
        };

        if (isEndGame)
            LockCursor(true);

        // Client sends RPC
        if (IsServer)
        {
            lastInput = input;
        }
        else
        {
            SubmitInputServerRpc(input);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) // Server-authoritative physics only
            return;

        if (GameOverUIController.IsGameOver)
        {
            // Hard stop
            playerRigidbody.linearVelocity = Vector3.zero;
            return;
        }

        // Apply authoritative yaw to body
        transform.rotation = Quaternion.Euler(0, lastInput.yaw, 0);

        Vector3 moveDir = (transform.forward * lastInput.moveZ + transform.right * lastInput.moveX);
        if (moveDir.sqrMagnitude > 1e-4f)
            moveDir.Normalize();

        if (moveDir.sqrMagnitude > 0 && isEndGame)
        {
            playerRigidbody.AddForce(moveDir * acceleration, ForceMode.Acceleration);
            animator.SetBool("isMoving", true);
        }
        else
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            animator.SetBool("isMoving", false);
        }

        // Clamp horizontal speed
        Vector3 velocity = playerRigidbody.linearVelocity;
        Vector3 horizontalSpeed = new Vector3(velocity.x, 0, velocity.z);
        if (horizontalSpeed.magnitude > maxVelocity)
        {
            horizontalSpeed = horizontalSpeed.normalized * maxVelocity;
            playerRigidbody.linearVelocity = new Vector3(horizontalSpeed.x, velocity.y, horizontalSpeed.z);
        }
    }

    [ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Unreliable)]
    private void SubmitInputServerRpc(InputState input)
    {
        lastInput = input;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
