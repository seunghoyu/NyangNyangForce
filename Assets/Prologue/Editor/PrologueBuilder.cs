using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrologueBuilder
{
    private const string ScenePath = "Assets/Scenes/Prologue.unity";

    [MenuItem("Tools/Cramming Hamster/Build Prologue Scene")]
    public static void Build()
    {
        ConfigureTexture("Assets/Prologue/Art/prologue_background.png");
        ConfigureTexture("Assets/Prologue/Art/player_prologue_stand.png");
        ConfigureTexture("Assets/Prologue/Art/player_walk.png");
        ConfigureTexture("Assets/Player/Art/player_idle.png", true);
        ConfigureTexture("Assets/Player/Art/player_run.png", true);
        ConfigureTexture("Assets/Player/Art/player_jump.png", true);
        ConfigureTexture("Assets/Player/Art/powerfall_1.png", true);
        ConfigureTexture("Assets/Player/Art/powerfall_2.png", true);
        ConfigureTexture("Assets/Player/Art/powerfall_3.png", true);
        ConfigureTexture("Assets/Prologue/Art/npc_rabbit.png");
        ConfigureTexture("Assets/Prologue/Art/npc_rabbit_walk.png");
        ConfigureTexture("Assets/Prologue/Art/item_gun.png");
        ConfigureTexture("Assets/Resources/Stage1/UI/Dialogue/PlayerMugshot/player_mugshot1.png");
        ConfigureTexture("Assets/Prologue/Art/player_mugshot2.png");
        ConfigureTexture("Assets/Resources/Stage1/UI/Dialogue/PlayerMugshot/player_mugshotbox.png");
        ConfigureTexture("Assets/Prologue/Art/npc_rabbit_mugshot.png");
        ConfigureTexture("Assets/Prologue/Art/npc_mugshot.png");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        GameObject controllerObject = new GameObject("PrologueController");
        PrologueController controller = controllerObject.AddComponent<PrologueController>();
        controller.backgroundTexture = LoadTexture("Assets/Prologue/Art/prologue_background.png");
        controller.playerPrologueStandingSheet = LoadTexture("Assets/Prologue/Art/player_prologue_stand.png");
        controller.playerWalkSheet = LoadTexture("Assets/Prologue/Art/player_walk.png");
        controller.playerArmedStandingSheet = LoadTexture("Assets/Player/Art/player_idle.png");
        controller.playerRunSheet = LoadTexture("Assets/Player/Art/player_run.png");
        controller.playerJumpSheet = LoadTexture("Assets/Player/Art/player_jump.png");
        controller.playerPowerfallWindupSheet = LoadTexture("Assets/Player/Art/powerfall_1.png");
        controller.playerPowerfallFallSheet = LoadTexture("Assets/Player/Art/powerfall_2.png");
        controller.playerPowerfallLandSheet = LoadTexture("Assets/Player/Art/powerfall_3.png");
        controller.rabbitStandingSheet = LoadTexture("Assets/Prologue/Art/npc_rabbit.png");
        controller.rabbitWalkSheet = LoadTexture("Assets/Prologue/Art/npc_rabbit_walk.png");
        controller.gunTexture = LoadTexture("Assets/Prologue/Art/item_gun.png");
        controller.playerMugshotSheet = LoadTexture("Assets/Resources/Stage1/UI/Dialogue/PlayerMugshot/player_mugshot1.png");
        controller.playerMugshotSheet2 = LoadTexture("Assets/Prologue/Art/player_mugshot2.png");
        controller.playerMugshotFrame = LoadTexture("Assets/Resources/Stage1/UI/Dialogue/PlayerMugshot/player_mugshotbox.png");
        controller.rabbitMugshotSheet = LoadTexture("Assets/Prologue/Art/npc_rabbit_mugshot.png");
        controller.rabbitMugshotFrame = LoadTexture("Assets/Prologue/Art/npc_mugshot.png");
        controller.hardModeSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/hardmode_sound.wav");
        controller.playerVoice = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/Dialogue/dialogue_voice_player.wav");
        controller.rabbitVoice = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/Dialogue/dialogue_voice_rabbit.wav");
        controller.slamImpactSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/player_jumpcrash_sound.wav");
        controller.slamImpactEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Stage1/Prefabs/SlamImpactEffect.prefab");

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddScenesToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Built Prologue scene and updated Build Settings.");
    }

    private static Texture2D LoadTexture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

    private static void ConfigureTexture(string path, bool preserveSpriteSlices = false)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (!preserveSpriteSlices)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
        }
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.SaveAndReimport();
    }

    private static void AddScenesToBuildSettings()
    {
        string[] desiredOrder =
        {
            "Assets/Scenes/Title.unity",
            "Assets/Scenes/Loading.unity",
            ScenePath,
            "Assets/Scenes/Tutorial.unity",
            "Assets/Scenes/World Map.unity",
            "Assets/Scenes/Stage 1.unity"
        };
        Dictionary<string, EditorBuildSettingsScene> existing = EditorBuildSettings.scenes
            .ToDictionary(scene => scene.path, scene => scene);
        List<EditorBuildSettingsScene> ordered = new List<EditorBuildSettingsScene>();
        foreach (string path in desiredOrder)
            ordered.Add(existing.TryGetValue(path, out EditorBuildSettingsScene entry)
                ? new EditorBuildSettingsScene(entry.path, true)
                : new EditorBuildSettingsScene(path, true));
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            if (!desiredOrder.Contains(entry.path)) ordered.Add(entry);
        EditorBuildSettings.scenes = ordered.ToArray();
    }
}
