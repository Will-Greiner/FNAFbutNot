using UnityEngine;

public class GuardSwapButton : MonoBehaviour
{
    [SerializeField] private GuardPrefabSwapper manager;
    [SerializeField] private int spawnIndex = 0;

    private void Awake()
    {
        manager = FindFirstObjectByType<GuardPrefabSwapper>();
    }

    public void ChangeIndex(int index)
    {
        spawnIndex = index;
    }

    public void OnClick()
    {
        if (!manager) { Debug.LogWarning("HostSwapButton: manager not assigned."); return; }
        manager.HostSwapTo(spawnIndex);
    }
}
