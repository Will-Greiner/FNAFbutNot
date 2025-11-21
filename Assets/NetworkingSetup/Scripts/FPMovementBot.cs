using UnityEngine;
using Unity.Netcode;
using System;

public class FPMovementBot : NetworkBehaviour
{
    [Header("Look")]
    [SerializeField] private Vector2 sensitivity = new Vector2(800, 800);
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float minPitch = -90;
    [SerializeField] private float maxPitch = 90;

    [Header("Move")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxVelocity = 6;

    [Header("Movement Audio")]
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private AudioClip treads;
    [SerializeField] private float minInputForSound = 0.1f;

    private Rigidbody playerRigidbody;
    private BotAttack botAttack;

    private float xRotation;
    private float yRotation;

    private InputState lastInput;

    // local input magnitude (for owner)
    private float currentMoveInput;

    public struct InputState : INetworkSerializable
    {
        public float moveX;
        public float moveZ;
        public float yaw;
        public float pitch;

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

        botAttack = GetComponent<BotAttack>();

        // --- Audio setup ---
        if (movementAudioSource == null)
        {
            movementAudioSource = GetComponentInChildren<AudioSource>(true);
            if (movementAudioSource == null)
            {
                Debug.LogWarning("[FPMovementBot] No movementAudioSource assigned or found.", this);
            }
        }

        if (movementAudioSource != null)
        {
            movementAudioSource.playOnAwake = false;
            movementAudioSource.loop = true;

            if (movementAudioSource.clip == null && treads != null)
            {
                movementAudioSource.clip = treads;
            }
        }

        if (movementAudioSource != null && movementAudioSource.clip == null && treads == null)
        {
            Debug.LogWarning("[FPMovementBot] Movement AudioSource has no clip and treads is null.", this);
        }
    }

    public override void OnNetworkSpawn()
    {
        playerRigidbody.isKinematic = !IsServer;

        if (IsOwner)
        {
            LockCursor(true);

            if (cameraTransform != null)
                xRotation = NormalizeAngle(cameraTransform.localEulerAngles.x);
            yRotation = transform.eulerAngles.y;
        }
        else
        {
            if (cameraTransform != null)
                cameraTransform.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            LockCursor(false);

        if (movementAudioSource != null && movementAudioSource.isPlaying)
            movementAudioSource.Stop();
    }

    private void Update()
    {
        if (IsOwner)
        {
            // --- Look ---
            float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensitivity.x;
            float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensitivity.y;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(xRotation, 0, 0);
            transform.rotation = Quaternion.Euler(0, yRotation, 0);

            // --- Input ---
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            if (botAttack != null && botAttack.IsAttacking)
            {
                inputX = 0;
                inputZ = 0;
            }

            currentMoveInput = new Vector2(inputX, inputZ).magnitude;

            var input = new InputState
            {
                moveX = Mathf.Clamp(inputX, -1, 1),
                moveZ = Mathf.Clamp(inputZ, -1, 1),
                yaw = yRotation,
                pitch = xRotation
            };

            if (IsServer)
            {
                lastInput = input;
            }
            else
            {
                SubmitInputServerRpc(input);
            }

            // Owner handles movement audio based on input
            UpdateMovementAudio();
        }
        // Non-owners don’t play the loop to avoid duplicates
    }

    private void FixedUpdate()
    {
        if (!IsServer)
            return;

        transform.rotation = Quaternion.Euler(0, lastInput.yaw, 0);

        Vector3 moveDir = (transform.forward * lastInput.moveZ + transform.right * lastInput.moveX);
        if (moveDir.sqrMagnitude > 1e-4f)
            moveDir.Normalize();

        if (moveDir.sqrMagnitude > 0)
            playerRigidbody.AddForce(moveDir * acceleration, ForceMode.Acceleration);
        else
            playerRigidbody.linearVelocity = Vector3.zero;

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

    private void UpdateMovementAudio()
    {
        if (movementAudioSource == null || movementAudioSource.clip == null)
            return;

        bool shouldPlay = currentMoveInput > minInputForSound;

        if (shouldPlay)
        {
            if (!movementAudioSource.isPlaying)
                movementAudioSource.Play();
        }
        else
        {
            if (movementAudioSource.isPlaying)
                movementAudioSource.Stop();
        }
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
