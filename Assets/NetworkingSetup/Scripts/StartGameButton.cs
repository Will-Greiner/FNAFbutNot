using UnityEngine;
using Unity.Netcode;

public class StartGameButton : MonoBehaviour
{
    LobbyPlayerSpawner _spawner;

    void Awake()
    {
        _spawner = FindFirstObjectByType<LobbyPlayerSpawner>();
    }

    // Hook this to Button.onClick and pass your gameplay scene name
    public void LoadGameScene(string sceneName)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsServer)
        {
            nm.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else if (nm.IsClient)
        {
            if (_spawner == null) _spawner = FindFirstObjectByType<LobbyPlayerSpawner>();
            _spawner?.RequestStartGameServerRpc(sceneName);
        }
    }
}