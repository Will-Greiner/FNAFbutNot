using UnityEngine;

public class BotAttackAnimationRelay : MonoBehaviour
{
    [Tooltip("Attack script to forward events to. If left null, will auto-find in parents.")]
    [SerializeField] private BotAttack attack;

    private void Awake()
    {
        if (attack == null)
        {
            attack = GetComponentInParent<BotAttack>();
            if (attack == null)
                Debug.LogWarning("BotAttackAnimationRelay: BotAttack not found in parents.");
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
}
