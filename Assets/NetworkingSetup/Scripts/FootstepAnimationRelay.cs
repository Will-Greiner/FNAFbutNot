using UnityEngine;

public class FootstepAnimationRelay : MonoBehaviour
{
    [Tooltip("ThirdPersonController to forward footstep events to. If left null, will auto-find in parents.")]
    [SerializeField] private ThirdPersonController controller;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<ThirdPersonController>();
            if (controller == null)
            {
                Debug.LogWarning("FootstepAnimationRelay: ThirdPersonController not found in parents.");
            }
        }
    }

    // Called by animation event on footstep frames
    public void PlayFootstep()
    {
        if (controller != null)
        {
            controller.PlayFootstep();
        }
    }
}
