using Steamworks;
using UnityEngine;

public class SteamBootstrap : MonoBehaviour
{
    private bool steamReady;
    private static SteamBootstrap instance;

    private void Awake()
    {
        // Ensure Steam is running and steam_appid.txt is present (480 for tests)
        steamReady = SteamAPI.Init();

        instance = this;

        if (!steamReady)
        {
            Debug.LogError("[Steam] SteamAPI.Init failed. Is Steam running? Do you have steam_appid.txt?");
#if UNITY_EDITOR
            //In Editor, don't kill play session; just stop
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void Update()
    {
        if (steamReady)
            SteamAPI.RunCallbacks();
    }

    private void OnDestroy()
    {
        if (steamReady)
            SteamAPI.Shutdown();
    }
}
