using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class Stage1AutoBuild
{
    static Stage1AutoBuild()
    {
        EditorApplication.delayCall += BuildIfOutdated;
    }

    private static void BuildIfOutdated()
    {
        const string scenePath = "Assets/Scenes/Stage 1.unity";
        const string builderPath = "Assets/Stage1/Editor/Stage1Builder.cs";
        if (File.Exists(scenePath) && File.GetLastWriteTimeUtc(scenePath) >= File.GetLastWriteTimeUtc(builderPath)) return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += BuildIfOutdated;
            return;
        }

        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += BuildIfOutdated;
            return;
        }

        Stage1Builder.Build();
    }
}
