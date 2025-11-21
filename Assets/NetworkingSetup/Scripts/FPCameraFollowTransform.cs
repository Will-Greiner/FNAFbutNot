using UnityEngine;

public class FPCameraFollowTransform : MonoBehaviour
{
    [Tooltip("Head bone of the character rig.")]
    public Transform followTransform;

    [Tooltip("Camera rig that owns the first-person camera.")]
    public Transform cameraRig;

    [Tooltip("Local offset from head (e.g. a little behind the eyes).")]
    public Vector3 localOffset;

    [Tooltip("Movement script so we can read isEndGame.")]
    [SerializeField] private FPMovement fpMovement;

    private void Awake()
    {
        // Auto-find if not set
        if (fpMovement == null && cameraRig != null)
        {
            fpMovement = FindFirstObjectByType<FPMovement>();
        }
    }

    void LateUpdate()
    {
        if (!followTransform || !cameraRig) return;


        // Offset is 0 until end game, then use localOffset
        Vector3 offsetToUse = Vector3.zero;

        if (fpMovement != null && fpMovement.isEndGame) // <-- uses your existing flag :contentReference[oaicite:1]{index=1}
        {
            offsetToUse = localOffset;
        }

        // Match position to head, ignore head rotation
        cameraRig.position = followTransform.position + followTransform.TransformVector(offsetToUse);

        // Rotation should come from your look script, NOT the head animation,
        // so we do NOT copy followTransform.rotation here.
    }
}
