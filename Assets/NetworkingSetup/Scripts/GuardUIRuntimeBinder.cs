using UnityEngine;

public class GuardUIRuntimeBinder : MonoBehaviour
{
    [Tooltip("Root of the normal guard UI (pre end-game).")]
    [SerializeField] private GameObject normalUIParent;

    [Tooltip("Root of the end-game guard UI (post Stage 3).")]
    [SerializeField] private GameObject endGameUIParent;

    private void Start()
    {
        // This prefab might exist on every client, so we just register
        // with the local GuardPrefabSwapper instance.
        if (GuardPrefabSwapper.Instance != null)
        {
            GuardPrefabSwapper.Instance.RegisterGuardUIParents(normalUIParent, endGameUIParent);
        }
        else
        {
            Debug.LogWarning("GuardUIRuntimeBinder: GuardPrefabSwapper.Instance is null.");
        }
    }
}
