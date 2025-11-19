using UnityEngine;

public class SpectatorUIRoot : MonoBehaviour
{
    [SerializeField] private GameObject rootPanel;

    private void Awake()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    public void SetSpectating(bool active)
    {
        if (rootPanel != null)
            rootPanel.SetActive(active);
    }
}
