using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransition
{
    private const string LoadingSceneName = "Loading";
    private static string targetSceneName;

    public static string TargetSceneName => targetSceneName;

    public static void Load(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || sceneName == LoadingSceneName) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Target scene '{sceneName}' is not registered in the active Build Profile/shared scene list.");
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            Debug.LogError("Loading scene is not registered in the active Build Profile/shared scene list.");
            return;
        }
        targetSceneName = sceneName;
        Time.timeScale = 1f;
        SceneManager.LoadScene(LoadingSceneName);
    }
}
