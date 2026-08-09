using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class PlayerAnimationRepairInstaller
{
    private const string PrefabPath = "Assets/Stage1/Prefabs/Player.prefab";

    [MenuItem("Tools/Cramming Hamster/Repair Shared Player Animation Assets")]
    public static void Install()
    {
        ConfigureSheet("Assets/Player/Art/player_idle.png", "Idle", 8);
        ConfigureSheet("Assets/Player/Art/player_run.png", "Run", 8);
        ConfigureSheet("Assets/Player/Art/powerfall_1.png", "PowerfallWindup", 4);
        ConfigureSheet("Assets/Player/Art/powerfall_2.png", "PowerfallFall", 3);
        ConfigureSheet("Assets/Player/Art/powerfall_3.png", "PowerfallLand", 5);

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            Stage1PlayerAnimation animation = root.GetComponent<Stage1PlayerAnimation>();
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            animation.idleFrames = LoadSprites("Assets/Player/Art/player_idle.png");
            animation.runFrames = LoadSprites("Assets/Player/Art/player_run.png");
            animation.slamWindupFrames = LoadSprites("Assets/Player/Art/powerfall_1.png");
            animation.slamFallFrames = LoadSprites("Assets/Player/Art/powerfall_2.png");
            animation.slamLandFrames = LoadSprites("Assets/Player/Art/powerfall_3.png");
            renderer.sprite = animation.idleFrames.Length > 0 ? animation.idleFrames[0] : null;
            EditorUtility.SetDirty(animation);
            EditorUtility.SetDirty(renderer);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Repaired shared Player animation sheets and prefab Sprite references.");
    }

    private static void ConfigureSheet(string path, string frameName, int frameCount)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 30f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = false;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        SpriteRect[] rects = new SpriteRect[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            rects[i] = new SpriteRect
            {
                name = frameName + "_" + i,
                rect = new Rect(i * 30f, 0f, 30f, 30f),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 14f / 30f),
                spriteID = GUID.Generate()
            };
        }
        provider.SetSpriteRects(rects);
        provider.Apply();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static Sprite[] LoadSprites(string path)
    {
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite sprite) sprites.Add(sprite);
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }
}
