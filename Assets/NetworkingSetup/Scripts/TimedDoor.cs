using UnityEngine;

/// <summary>
/// Runs a timer, then lerps its target (parent by default) by a set local offset.
/// All instances share a global time reduction that can be modified at runtime.
/// </summary>
public class TimedDoor : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Base time in seconds before the door starts moving.")]
    [SerializeField] private float baseDelay = 10f;

    [Tooltip("Minimum possible delay after global reductions.")]
    [SerializeField] private float minimumDelay = 1f;

    [Tooltip("How long the door takes to fully move once triggered.")]
    [SerializeField] private float openDuration = 2f;

    [Header("Movement")]
    [Tooltip("Local offset to apply when opening (e.g. (0,2,0) to move up).")]
    [SerializeField] private Vector3 localMoveOffset = new Vector3(0f, -2f, 0f);

    [Tooltip("Use smooth step instead of linear interpolation.")]
    [SerializeField] private bool smooth = true;

    // ---- static global modifier ----
    public static float GlobalTimeIncrease { get; private set; } = 0f;

    [Tooltip("Maximum total reduction that can be applied globally.")]
    [SerializeField] private float maxTotalIncrease = 30f;

    /// <summary>
    /// Call this from the upgrade station to reduce the delay on ALL TimedDoor instances.
    /// </summary>
    public static void AddGlobalTimeReduction(float amount)
    {
        GlobalTimeIncrease = Mathf.Max(0f, GlobalTimeIncrease + amount);
    }

    // ---- instance fields ----
    private Transform _target;        // door to move
    private Vector3 _closedPos;
    private Vector3 _openPos;

    private float _startTime;
    private bool _opening;
    private bool _opened;

    private void Awake()
    {
        // If this object has a parent, use the parent as the door; otherwise use self.
        _target = transform.parent != null ? transform.parent : transform;

        _closedPos = _target.localPosition;
        _openPos = _closedPos + localMoveOffset;

        _startTime = Time.time;
    }

    private void OnEnable()
    {
        // restart timer when enabled
        _startTime = Time.time;
        _opening = false;
        _opened = false;
        _target.localPosition = _closedPos;
    }

    private void Update()
    {
        if (_opened) return;

        float effectiveDelay = Mathf.Clamp(
            baseDelay + Mathf.Min(GlobalTimeIncrease, maxTotalIncrease),
            minimumDelay,
            baseDelay + maxTotalIncrease);

        if (!_opening)
        {
            float elapsed = Time.time - _startTime;
            if (elapsed >= effectiveDelay)
            {
                // start opening
                _opening = true;
                _startTime = Time.time;
            }
        }
        else
        {
            float t = (Time.time - _startTime) / openDuration;
            t = Mathf.Clamp01(t);

            if (smooth)
                t = Mathf.SmoothStep(0f, 1f, t);

            _target.localPosition = Vector3.Lerp(_closedPos, _openPos, t);

            if (t >= 1f)
            {
                _opened = true;
            }
        }
    }
}
