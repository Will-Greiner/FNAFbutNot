using UnityEngine;
using UnityEngine.UI;

public class GuardSwapButton : MonoBehaviour
{
    [SerializeField] private GuardPrefabSwapper manager;
    [SerializeField] private int spawnIndex = 0;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        manager = FindFirstObjectByType<GuardPrefabSwapper>();
    }
    private void OnEnable()
    {
        RefreshInteractable();
    }

    private void Update()
    {
        RefreshInteractable();
    }

    public void ChangeIndex(int index)
    {
        spawnIndex = index;
        RefreshInteractable();
    }

    public void OnClick()
    {
        if (!manager) { Debug.LogWarning("HostSwapButton: manager not assigned."); return; }
        if (!manager.IsSpawnAvailable(spawnIndex))
        {
            Debug.Log("GuardSwapButton: spawn index " + spawnIndex + " is no longer available.");
            RefreshInteractable();
            return;
        }
        manager.HostSwapTo(spawnIndex);
    }
    private void RefreshInteractable()
    {
        if (!button) return;

        // If we don't have a manager yet, keep button disabled.
        if (!manager)
        {
            button.interactable = false;
            return;
        }

        bool available = manager.IsSpawnAvailable(spawnIndex);
        button.interactable = available;
    }
}
