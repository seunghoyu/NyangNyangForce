using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LoadingSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Loading.unity";

    [MenuItem("Tools/Cramming Hamster/Build Loading Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        new GameObject("LoadingScreenController").AddComponent<LoadingScreenController>();
        EditorSceneManager.SaveScene(scene, ScenePath);

        string[] order = { "Assets/Scenes/Title.unity", ScenePath, "Assets/Scenes/Prologue.unity", "Assets/Scenes/Tutorial.unity", "Assets/Scenes/World Map.unity", "Assets/Scenes/Stage 1.unity" };
        Dictionary<string, EditorBuildSettingsScene> existing = EditorBuildSettings.scenes.ToDictionary(x => x.path, x => x);
        List<EditorBuildSettingsScene> scenes = order.Select(path => new EditorBuildSettingsScene(path, true)).ToList();
        scenes.AddRange(existing.Values.Where(x => !order.Contains(x.path)));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Built Loading scene and updated Build Settings.");
    }
}
