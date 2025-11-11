using UnityEngine;

public class OrbitCameraOLD : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                  // set at runtime for the local player
    public Vector3 targetOffset = new Vector3(0f, 1.7f, 0f);

    [Header("Orbit")]
    public float yaw = 0f;                    // degrees
    public float pitch = 15f;                 // degrees
    public float minPitch = -30f;
    public float maxPitch = 70f;
    public float mouseXSens = 250f;
    public float mouseYSens = 250f;

    [Header("Zoom")]
    public float distance = 4.5f;
    public float minDistance = 2.0f;
    public float maxDistance = 7.0f;
    public float zoomSpeed = 4.0f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float sphereRadius = 0.2f;

    [Header("Quality")]
    public float followLerp = 20f;            // camera smoothing

    void LateUpdate()
    {
        if (!target) return;

        // Mouse input (always-on; or gate behind RMB if you prefer)
        float mx = Input.GetAxis("Mouse X") * mouseXSens * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseYSens * Time.deltaTime;
        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 1e-4f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        // Desired camera pose
        Vector3 pivot = target.position + targetOffset;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = pivot - (rot * Vector3.forward) * distance;

        // Collision: push camera forward if obstructed
        Vector3 dir = (desiredPos - pivot);
        float maxDist = dir.magnitude;
        if (maxDist > 1e-3f)
        {
            dir /= maxDist;
            if (Physics.SphereCast(pivot, sphereRadius, dir, out RaycastHit hit, maxDist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                desiredPos = pivot + dir * (hit.distance - 0.05f);
            }
        }

        // Smoothly move & rotate
        transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
        transform.rotation = rot;
    }
}
