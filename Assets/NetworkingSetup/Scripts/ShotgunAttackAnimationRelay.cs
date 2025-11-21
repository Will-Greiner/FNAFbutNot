using UnityEngine;

public class ShotgunAttackAnimationRelay : MonoBehaviour
{
    [Tooltip("Attack script to forward events to. If left null, will auto-find in parents.")]
    [SerializeField] private Shotgun attack;

    private void Awake()
    {
        if (attack == null)
        {
            attack = GetComponentInParent<Shotgun>();
            if (attack == null)
                Debug.LogWarning("ShotgunAttackAnimationRelay: Shotgun not found in parents.");
        }
    }

    public void AnimationAttackHit()
    {
        if (attack != null)
            attack.AnimationAttackHit();
    }

    public void AnimationAttackEnd()
    {
        if (attack != null)
            attack.AnimationAttackEnd();
    }

    public void PlayFireSound()
    {
        if (attack != null)
            attack.PlayFireSound();
    }

    public void PlayReloadSound()
    {
        if (attack != null)
            attack.PlayReloadSound();
    }
}