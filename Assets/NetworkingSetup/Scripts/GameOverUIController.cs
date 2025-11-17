using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;

public class GameOverUIController : MonoBehaviour
{
    public static GameOverUIController Instance { get; private set; }

    // Global flag everyone else can read
    public static bool IsGameOver { get; private set; }

    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        IsGameOver = false;
    }

    public void ShowGameOver()
    {
        IsGameOver = true;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        // 🔓 Enable cursor and unlock it
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnMainMenuClicked()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }
}
