using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TutorialBuilder
{
    private const string ScenePath = "Assets/Scenes/Tutorial.unity";
    private const string BackgroundPath = "Assets/Prologue/Art/prologue_background.png";
    private const string ScarecrowPath = "Assets/Tutorial/Art/tutorial_scarecrow.png";
    private const string PlatformPath = "Assets/Stage1/Art/Environment/library_platform_left.png";
    private const string HitBurstPath = "Assets/Player/Art/Projectiles/player_basic_bullet_burst.png";
    private const float FloorHeight = 1f;
    private const float FloorTransformY = -3f;
    private const float FloorSurfaceY = FloorTransformY + FloorHeight * 0.5f;
    private const float PlayerColliderBottomFromRoot = -0.42666665f;

    [MenuItem("Tools/Cramming Hamster/Build Tutorial Scene")]
    public static void Build()
    {
        ConfigureTexture(BackgroundPath, new Vector2(0.5f, 0.5f));
        ConfigureTexture(ScarecrowPath, new Vector2(0.5f, 0f));
        ConfigureTexture(PlatformPath, new Vector2(0.5f, 0.5f), 100f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        CreateBackground();
        CreateFloor();
        CreateAirPlatform();

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Stage1/Prefabs/Player.prefab");
        GameObject playerObject = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerObject.name = "Player";
        playerObject.transform.position = new Vector3(-4.2f, FloorSurfaceY - PlayerColliderBottomFromRoot, -2f);
        Stage1Player player = playerObject.GetComponent<Stage1Player>();
        CreateScarecrow();

        GameObject manager = new GameObject("TutorialGame");
        Stage1Game game = manager.AddComponent<Stage1Game>();
        game.player = player;
        game.boss = null;
        game.freeRoamMode = true;
        GameObject basicShot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Stage1/Prefabs/BasicPurificationShot.prefab");
        GameObject machineShot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Stage1/Prefabs/PurificationShot.prefab");
        GameObject hazard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Stage1/Prefabs/Hazard.prefab");
        GameObject machineGunPickup = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Stage1/Prefabs/MachineGunPickup.prefab");
        game.basicPlayerShotPrefab = basicShot.GetComponent<Stage1Projectile>();
        game.machineGunShotPrefab = machineShot.GetComponent<Stage1Projectile>();
        game.hazardPrefab = hazard.GetComponent<Stage1Projectile>();
        game.machineGunPickupPrefab = machineGunPickup.GetComponent<Stage1MachineGunPickup>();
        manager.AddComponent<TutorialController>();
        TutorialKeyGuide guide = new GameObject("TutorialKeyGuide").AddComponent<TutorialKeyGuide>();
        guide.previewSheets = new[]
        {
            LoadTexture("Assets/Player/Art/player_run.png"),
            LoadTexture("Assets/Player/Art/player_jump.png"),
            LoadTexture("Assets/Player/Art/player_dash.png"),
            LoadTexture("Assets/Player/Art/run_shot_up.png"),
            LoadTexture("Assets/Player/Art/run_shot_down.png"),
            LoadTexture("Assets/Player/Art/player_fall.png"),
            LoadTexture("Assets/Player/Art/powerfall_2.png"),
            LoadTexture("Assets/Player/Art/player_shoot_idle.png"),
            LoadTexture("Assets/Stage1/Art/Items/item_gatling_appear.png")
        };

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Built boss-free Tutorial scene with Prologue campus background.");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateBackground()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        GameObject root = new GameObject("TutorialCampusBackground", typeof(SpriteRenderer));
        root.transform.position = new Vector3(0f, 0f, 2f);
        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -10;
        float visibleHeight = 9f;
        float visibleWidth = visibleHeight * (16f / 9f);
        Vector2 size = sprite.bounds.size;
        float scale = Mathf.Max(visibleWidth / size.x, visibleHeight / size.y);
        root.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static void CreateFloor()
    {
        GameObject floor = new GameObject("TutorialFloor", typeof(BoxCollider2D));
        floor.transform.position = new Vector3(0f, FloorTransformY, 0f);
        BoxCollider2D collider = floor.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(20f, FloorHeight);
        collider.offset = Vector2.zero;
    }

    private static void CreateScarecrow()
    {
        GameObject root = new GameObject("TutorialScarecrow", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Stage1HitShake), typeof(TutorialScarecrow));
        root.transform.position = new Vector3(3.25f, FloorSurfaceY, -1f);
        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ScarecrowPath);
        renderer.sortingOrder = 2;
        BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.25f, 1.7f);
        collider.offset = new Vector2(0f, 0.83f);
        root.GetComponent<TutorialScarecrow>().hitBurstSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HitBurstPath);
    }

    private static void CreateAirPlatform()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlatformPath);
        GameObject platform = new GameObject("TutorialAirPlatform", typeof(SpriteRenderer));
        platform.transform.position = new Vector3(-3.8f, -0.7f, 0.3f);
        SpriteRenderer renderer = platform.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;

        GameObject surface = new GameObject("TopSurfaceCollider", typeof(BoxCollider2D), typeof(PlatformEffector2D));
        surface.transform.SetParent(platform.transform, false);
        surface.transform.position = new Vector3(-3.8f, -0.415f, 0f);
        BoxCollider2D collider = surface.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(3.78f, 0.12f);
        collider.usedByEffector = true;
        surface.GetComponent<PlatformEffector2D>().useOneWay = true;
    }

    private static Texture2D LoadTexture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

    private static void ConfigureTexture(string path, Vector2 pivot, float pixelsPerUnit = 30f)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.SaveAndReimport();
    }
}
