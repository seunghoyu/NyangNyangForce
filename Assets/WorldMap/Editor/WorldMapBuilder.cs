using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WorldMapBuilder
{
    private const string Root = "Assets/WorldMap";
    private const string ScenePath = "Assets/Scenes/World Map.unity";
    private const string PlayerIdlePath = "Assets/Player/Art/player_idle.png";
    private const string PlayerRunPath = "Assets/Player/Art/player_run.png";
    private static readonly Vector2[] NodePositions =
    {
        new Vector2(-16f, -109.5f),
        new Vector2(-416f, 45.5f),
        new Vector2(-16f, 111.5f),
        new Vector2(384f, 30.5f),
        new Vector2(-416f, -249.5f),
        new Vector2(-16f, -354.5f),
        new Vector2(384f, -239.5f)
    };

    [MenuItem("Tools/Cramming Hamster/Build World Map Scene")]
    public static void Build()
    {
        ConfigureTexture(Root + "/worldmap_demo1.png", false);
        ConfigureTexture(Root + "/platformroad_direction.png", true);
        for (int i = 0; i <= 6; i++) ConfigureTexture(Root + "/stage" + i + "_platform.png", true);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        CreateEventSystem();
        Canvas canvas = CreateCanvas();
        RectTransform mapRoot = CreateImage("WorldMapDemo", canvas.transform, LoadSprite(Root + "/worldmap_demo1.png"), Vector2.zero, new Vector2(1672f, 941f), false).rectTransform;

        string lockedMaterialPath = Root + "/WorldMapLockedStage.mat";
        AssetDatabase.DeleteAsset(lockedMaterialPath);
        Material lockedMaterial = new Material(Shader.Find("UI/CrammingHamsterGrayscale"));
        lockedMaterial.name = "WorldMapLockedStage";
        AssetDatabase.CreateAsset(lockedMaterial, lockedMaterialPath);

        Sprite roadSprite = LoadSprite(Root + "/platformroad_direction.png");
        Image[] roadDots = CreateRoadDots(mapRoot, roadSprite, lockedMaterial);
        Image homeNode = CreateImage("Stage0Start", mapRoot, LoadSprite(Root + "/stage0_platform.png"), NodePositions[0], new Vector2(80f, 80f), true);

        GameObject controllerObject = new GameObject("WorldMapController");
        controllerObject.transform.SetParent(canvas.transform, false);
        WorldMapScreen screen = controllerObject.AddComponent<WorldMapScreen>();
        WorldMapStageHotspot homeHotspot = homeNode.gameObject.AddComponent<WorldMapStageHotspot>();
        homeHotspot.Configure(screen, 0);
        Image[] nodes = new Image[6];
        for (int stage = 1; stage <= 6; stage++)
        {
            Image node = CreateImage("Stage" + stage + "Node", mapRoot, LoadSprite(Root + "/stage" + stage + "_platform.png"), NodePositions[stage], new Vector2(80f, 80f), true);
            node.material = lockedMaterial;
            WorldMapStageHotspot hotspot = node.gameObject.AddComponent<WorldMapStageHotspot>();
            hotspot.Configure(screen, stage);
            nodes[stage - 1] = node;
        }

        Sprite[] playerFrames = AssetDatabase.LoadAllAssetsAtPath(PlayerIdlePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();
        Sprite[] playerRunFrames = AssetDatabase.LoadAllAssetsAtPath(PlayerRunPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();
        Image playerMarker = CreateImage(
            "WorldMapPlayer",
            mapRoot,
            playerFrames.Length > 0 ? playerFrames[0] : null,
            NodePositions[0],
            new Vector2(54f, 54f),
            false);

        Image fade = CreateImage("FadeOverlay", canvas.transform, null, Vector2.zero, Vector2.zero, false);
        Stretch(fade.rectTransform);
        fade.color = Color.black;
        SerializedObject serialized = new SerializedObject(screen);
        serialized.FindProperty("fadeImage").objectReferenceValue = fade;
        serialized.FindProperty("mapRoot").objectReferenceValue = mapRoot;
        serialized.FindProperty("homeNode").objectReferenceValue = homeNode;
        serialized.FindProperty("playerMarker").objectReferenceValue = playerMarker;
        serialized.FindProperty("lockedStageMaterial").objectReferenceValue = lockedMaterial;
        SerializedProperty roads = serialized.FindProperty("roadDots");
        roads.arraySize = roadDots.Length;
        for (int i = 0; i < roadDots.Length; i++) roads.GetArrayElementAtIndex(i).objectReferenceValue = roadDots[i];
        SerializedProperty array = serialized.FindProperty("stageNodes");
        array.arraySize = nodes.Length;
        for (int i = 0; i < nodes.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
        SerializedProperty frames = serialized.FindProperty("playerIdleFrames");
        frames.arraySize = playerFrames.Length;
        for (int i = 0; i < playerFrames.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = playerFrames[i];
        SerializedProperty runFrames = serialized.FindProperty("playerRunFrames");
        runFrames.arraySize = playerRunFrames.Length;
        for (int i = 0; i < playerRunFrames.Length; i++) runFrames.GetArrayElementAtIndex(i).objectReferenceValue = playerRunFrames[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Built reference-based World Map scene.");
    }

    private static Image[] CreateRoadDots(Transform parent, Sprite sprite, Material lockedMaterial)
    {
        Vector2[] referencePixelCenters =
        {
            new Vector2(732f, 354f), new Vector2(671f, 358f), new Vector2(619f, 375f),
            new Vector2(581f, 401f), new Vector2(496f, 427f), new Vector2(548f, 427f),
            new Vector2(571f, 457f), new Vector2(596f, 487f), new Vector2(603f, 527f),
            new Vector2(604f, 571f), new Vector2(758f, 599f), new Vector2(716f, 608f),
            new Vector2(666f, 614f), new Vector2(619f, 615f), new Vector2(601f, 663f),
            new Vector2(571f, 702f), new Vector2(510f, 714f), new Vector2(821f, 652f),
            new Vector2(821f, 698f), new Vector2(821f, 736f), new Vector2(910f, 354f),
            new Vector2(971f, 358f), new Vector2(1023f, 375f), new Vector2(1061f, 401f),
            new Vector2(1094f, 427f), new Vector2(1146f, 427f), new Vector2(1071f, 457f),
            new Vector2(1046f, 487f), new Vector2(1039f, 527f), new Vector2(1038f, 571f),
            new Vector2(884f, 599f), new Vector2(926f, 608f), new Vector2(976f, 614f),
            new Vector2(1023f, 615f), new Vector2(1041f, 663f), new Vector2(1071f, 702f),
            new Vector2(1132f, 714f)
        };
        Image[] dots = new Image[referencePixelCenters.Length];
        for (int i = 0; i < referencePixelCenters.Length; i++)
        {
            Vector2 pixel = referencePixelCenters[i];
            Vector2 anchored = new Vector2(pixel.x - 836f, 470.5f - pixel.y);
            Image dot = CreateImage("RoadDot_" + i, parent, sprite, anchored, new Vector2(15f, 15f), false);
            dot.material = lockedMaterial;
            dots[i] = dot;
        }
        return dots;
    }

    private static Canvas CreateCanvas()
    {
        GameObject root = new GameObject("WorldMapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672f, 941f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateCamera()
    {
        GameObject root = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        root.tag = "MainCamera";
        Camera camera = root.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        root.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        GameObject root = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, bool raycast)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(parent, false);
        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = raycast;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    private static void ConfigureTexture(string path, bool alpha)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = alpha;
        importer.spritePixelsPerUnit = 100f;
        importer.SaveAndReimport();
    }
}
