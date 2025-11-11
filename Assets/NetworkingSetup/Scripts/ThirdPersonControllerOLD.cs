//using Unity.Netcode;
//using UnityEngine;

//public class ThirdPersonController : NetworkBehaviour
//{
//    [Header("Movement")]
//    [Tooltip("Degrees per second to rotate toward movement direction.")]
//    [SerializeField] private float turnSpeed = 600f;
//    [SerializeField] private float acceleration = 30;
//    [SerializeField] private float maxVelocity = 6;

//    [Header("Stability")]
//    [SerializeField] private float brakingAcceleration = 35f; // applied when no input
//    [SerializeField] private float moveDrag = 0f;
//    [SerializeField] private float idleDrag = 4f;

//    [Header("Networking")]
//    [SerializeField] private float inputTimeoutSeconds = 0.35f;
//    [SerializeField] private int inputSendRateHz = 60;        // align with server tick

//    // ----------------- input payload (owner -> server) -----------------
//    private struct InputState : INetworkSerializable
//    {
//        public float turn;  
//        public float moveZ;  

//        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
//        {
//            s.SerializeValue(ref turn);
//            s.SerializeValue(ref moveZ);
//        }
//    }

//    private InputState lastInput;
//    private double lastInputServerTime;

//    private Rigidbody playerRigidbody;

//    // owner-side send pacing
//    private float sendInterval, sendAccum;

//    private void Awake()
//    {
//        playerRigidbody = GetComponent<Rigidbody>();
//        playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
//        playerRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
//        playerRigidbody.useGravity = false;
//    }

//    public override void OnNetworkSpawn()
//    {
//        // Critical for NGO: only the server simulates physics
//        playerRigidbody.isKinematic = !IsServer;

//        sendInterval = 1f / Mathf.Max(1, inputSendRateHz);
//        sendAccum = 0f;
//    }

//    private void Update()
//    {
//        // Owner collects input each frame and sends to server
//        if (!IsOwner) 
//            return;

//        // Read raw axes (works with keyboard and most controllers)
//        float inputX = Input.GetAxisRaw("Horizontal");
//        float inputZ = Input.GetAxisRaw("Vertical");

//        // Deadzone + clamp
//        float turn = Mathf.Abs(inputX) < 0.15f ? 0 : Mathf.Clamp(inputX, -1, 1);
//        float move = Mathf.Abs(inputZ) < 0.15f ? 0 : Mathf.Clamp(inputZ, -1, 1);

//        // Send at a fixed cadence to avoid jitter between Update/FixedUpdate
//        sendAccum += Time.unscaledDeltaTime;
//        if (sendAccum >= sendInterval)
//        {
//            sendAccum = 0f;
//            var input = new InputState
//            {
//                turn = turn,
//                moveZ = move,
//            };

//            if (IsServer)
//            {
//                lastInput = input;
//                lastInputServerTime = NetworkManager.ServerTime.Time;
//            }
//            else
//            {
//                SubmitInputServerRpc(input);
//            }
//        }
//    }

//    private void FixedUpdate()
//    {
//        if (!IsServer) return;

//        // Time out stale input
//        double now = NetworkManager.ServerTime.Time;
//        if (now - lastInputServerTime > inputTimeoutSeconds)
//        {
//            lastInput.turn = 0f;
//            lastInput.moveZ = 0f;
//        }

//        // Rotate
//        if (Mathf.Abs(lastInput.turn) > 1e-4f)
//        {
//            float deltaYaw = lastInput.turn * turnSpeed * Time.fixedDeltaTime;
//            playerRigidbody.MoveRotation(playerRigidbody.rotation * Quaternion.Euler(0, deltaYaw, 0));
//        }

//        // Move
//        Vector3 velocity = playerRigidbody.linearVelocity;
//        Vector3 horizVelocity = new Vector3(velocity.x, 0, velocity.z);

//        if (Mathf.Abs(lastInput.moveZ) > 1e-4f)
//        {
//            Vector3 direction = playerRigidbody.rotation * Vector3.forward * Mathf.Sign(lastInput.moveZ);
//            playerRigidbody.AddForce(direction * (acceleration * Mathf.Abs(lastInput.moveZ)), ForceMode.Acceleration);

//            // Clamp speed
//            horizVelocity = new Vector3(playerRigidbody.linearVelocity.x, 0, playerRigidbody.linearVelocity.z);
//            if (horizVelocity.magnitude > maxVelocity)
//            {
//                horizVelocity = horizVelocity.normalized * maxVelocity;
//                playerRigidbody.linearVelocity = new Vector3(horizVelocity.x, playerRigidbody.linearVelocity.y, horizVelocity.z);
//            }

//            playerRigidbody.linearDamping = moveDrag;
//        }
//        else
//        {
//            if (horizVelocity.sqrMagnitude > 1e-6f)
//            {
//                playerRigidbody.AddForce(-horizVelocity.normalized * brakingAcceleration, ForceMode.Acceleration);
//            }
//            playerRigidbody.linearDamping = idleDrag;
//        }
//    }

//    [ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Unreliable)]
//    private void SubmitInputServerRpc(InputState input)
//    {
//        lastInput = input;
//        lastInputServerTime = NetworkManager.ServerTime.Time;
//    }
//}




using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class ThirdPersonControllerOLD : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float turnSpeed = 600f;   // deg/sec
    [SerializeField] private float acceleration = 30f; // m/s^2
    [SerializeField] private float maxVelocity = 6f;   // m/s

    [Header("Stability")]
    [SerializeField] private float brakingAcceleration = 35f; // m/s^2 when no input
    [SerializeField] private float moveDrag = 0f;
    [SerializeField] private float idleDrag = 4f;

    [Header("Networking")]
    [SerializeField] private float inputTimeoutSeconds = 0.35f;
    [SerializeField] private int inputSendRateHz = 60;

    private struct InputState : INetworkSerializable
    {
        public float turn;     // A/D
        public float moveZ;    // W/S
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref turn);
            s.SerializeValue(ref moveZ);
        }
    }

    private InputState lastInput;
    private double lastInputServerTime;

    private Rigidbody rb;
    private float sendInterval, sendAccum;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // smoother contacts
    }

    public override void OnNetworkSpawn()
    {
        rb.isKinematic = !IsServer; // server-only physics
        sendInterval = 1f / Mathf.Max(1, inputSendRateHz);
        sendAccum = 0f;
    }

    private void Update()
    {
        if (!IsOwner) return;

        float inputX = Input.GetAxisRaw("Horizontal"); // A/D
        float inputZ = Input.GetAxisRaw("Vertical");   // W/S

        const float dead = 0.15f;
        float turn = Mathf.Abs(inputX) < dead ? 0f : Mathf.Clamp(inputX, -1f, 1f);
        float move = Mathf.Abs(inputZ) < dead ? 0f : Mathf.Clamp(inputZ, -1f, 1f);

        sendAccum += Time.unscaledDeltaTime;
        if (sendAccum >= sendInterval)
        {
            sendAccum = 0f;
            var input = new InputState { turn = turn, moveZ = move };

            if (IsServer)
            {
                lastInput = input;
                lastInputServerTime = NetworkManager.ServerTime.Time;
            }
            else
            {
                SubmitInputServerRpc(input);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        double now = NetworkManager.ServerTime.Time;
        if (now - lastInputServerTime > inputTimeoutSeconds)
        {
            lastInput.turn = 0f;
            lastInput.moveZ = 0f;
        }

        float dt = Time.fixedDeltaTime;

        // 1) Rotate (A/D)
        if (Mathf.Abs(lastInput.turn) > 1e-4f)
        {
            float deltaYaw = lastInput.turn * turnSpeed * dt;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, deltaYaw, 0f));
        }

        // 2) Move (W/S) using forces along local forward/back
        Vector3 vel = rb.linearVelocity;
        Vector3 horiz = new Vector3(vel.x, 0f, vel.z);

        if (Mathf.Abs(lastInput.moveZ) > 1e-4f)
        {
            // accelerate along facing
            Vector3 dir = rb.rotation * Vector3.forward * Mathf.Sign(lastInput.moveZ);
            rb.AddForce(dir * (acceleration * Mathf.Abs(lastInput.moveZ)), ForceMode.Acceleration);

            // re-sample AFTER force, then clamp planar speed if needed
            horiz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horiz.magnitude > maxVelocity)
            {
                horiz = horiz.normalized * maxVelocity;
                rb.linearVelocity = new Vector3(horiz.x, rb.linearVelocity.y, horiz.z);
            }

            rb.linearDamping = moveDrag;
        }
        else
        {
            // gentle braking when no input (don’t hard zero velocity)
            if (horiz.sqrMagnitude > 1e-6f)
            {
                rb.AddForce(-horiz.normalized * brakingAcceleration, ForceMode.Acceleration);
            }
            rb.linearDamping = idleDrag;
        }
    }

    [ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Unreliable)]
    private void SubmitInputServerRpc(InputState input)
    {
        lastInput = input;
        lastInputServerTime = NetworkManager.ServerTime.Time;
    }
}
