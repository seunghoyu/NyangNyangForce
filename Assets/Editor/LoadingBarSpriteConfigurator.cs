using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class LoadingBarSpriteConfigurator
{
    private const string BarAssetPath = "Assets/Resources/UI/Loading/loadingbar.png";
    private const string EffectAssetPath = "Assets/Resources/UI/Loading/loadingbar_effect.png";

    [MenuItem("Tools/Cramming Hamster/Configure Loading Bar Sprites")]
    public static void Configure()
    {
        RestoreLoadingBarTexture();
        AssetDatabase.ImportAsset(EffectAssetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(EffectAssetPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 30f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        Rect[] frameRects =
        {
            new Rect(0f, 0f, 4f, 20f),
            new Rect(7f, 0f, 3f, 20f),
            new Rect(13f, 0f, 4f, 20f)
        };
        SpriteRect[] sprites = new SpriteRect[frameRects.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i] = new SpriteRect
            {
                name = "LoadingBarEffect_" + i,
                rect = frameRects[i],
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            };
        }
        provider.SetSpriteRects(sprites);
        provider.Apply();
        AssetDatabase.ImportAsset(EffectAssetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        Debug.Log("Restored loadingbar.png and configured loadingbar_effect.png as three gauge sprites.");
    }

    private static void RestoreLoadingBarTexture()
    {
        AssetDatabase.ImportAsset(BarAssetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(BarAssetPath) as TextureImporter;
        if (importer == null) return;
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (provider != null)
        {
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(new SpriteRect[0]);
            provider.Apply();
        }
        importer.textureType = TextureImporterType.Default;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }
}
