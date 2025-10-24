using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class SceneSpawnPoints : MonoBehaviour
{
    public Transform[] spawnPoints;

    // Finds the SceneSpawnPoints that lives in the given scene (or active scene)
    public static Transform[] Get(string sceneName = null)
    {
        Scene scene = string.IsNullOrEmpty(sceneName)
            ? SceneManager.GetActiveScene()
            : SceneManager.GetSceneByName(sceneName);

        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"[SceneSpawnPoints] Scene '{sceneName}' not valid/loaded.");
            return null;
        }

        // Traverse only this scene’s roots; include inactive children.
        foreach (var root in scene.GetRootGameObjects())
        {
            var ssp = root.GetComponentsInChildren<SceneSpawnPoints>(true).FirstOrDefault();
            if (ssp != null) return ssp.spawnPoints;
        }

        Debug.LogWarning($"[SceneSpawnPoints] No spawn set found in scene '{scene.name}'.");
        return null;
    }
}