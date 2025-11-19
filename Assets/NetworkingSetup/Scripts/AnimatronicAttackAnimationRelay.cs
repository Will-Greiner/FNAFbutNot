using UnityEngine;

/// <summary>
/// Lives on the same GameObject as the Animator.
/// Animation events call into this, and it forwards to AnimatronicAttack,
/// which may live on a parent/child.
/// </summary>
public class AnimatronicAttackAnimationRelay : MonoBehaviour
{
    [Tooltip("Attack script to forward events to. If left null, will auto-find in parents.")]
    [SerializeField] private AnimatronicAttack attack;

    private void Awake()
    {
        if (attack == null)
        {
            attack = GetComponentInParent<AnimatronicAttack>();
            if (attack == null)
                Debug.LogWarning("AnimatronicAttackAnimationRelay: AnimatronicAttack not found in parents.");
        }
    }

    // Called by animation event at hit frame
    // (Animation window → Add Event → function name: Animation_AttackHit)
    public void AnimationAttackHit()
    {
        if (attack != null)
            attack.AnimationAttackHit();
    }

    // Called by animation event at end of attack clip
    // (Animation window → Add Event → function name: Animation_AttackEnd)
    public void AnimationAttackEnd()
    {
        if (attack != null)
            attack.AnimationAttackEnd();
    }
}
