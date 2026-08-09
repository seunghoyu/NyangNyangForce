using UnityEngine;

/// <summary>
/// Shared font entry point for runtime UI. Screens depend on this provider,
/// while the concrete font asset and Resources path stay centralized here.
/// </summary>
public static class GameTypography
{
    private const string DialogueFontResourcePath =
        "Common/UI/Typography/NeoDunggeunmo/neodgm";

    private static Font dialogueFont;

    public static Font DialogueFont
    {
        get
        {
            if (dialogueFont == null)
                dialogueFont = Resources.Load<Font>(DialogueFontResourcePath);
            return dialogueFont;
        }
    }

    public static void ApplyDialogueFont(params GUIStyle[] styles)
    {
        Font font = DialogueFont;
        if (font == null || styles == null) return;

        foreach (GUIStyle style in styles)
        {
            if (style != null)
                style.font = font;
        }
    }

    public static void ApplyToCurrentSkin()
    {
        Font font = DialogueFont;
        if (font == null || GUI.skin == null) return;
        GUI.skin.font = font;
        GUI.skin.label.font = font;
        GUI.skin.button.font = font;
        GUI.skin.toggle.font = font;
        GUI.skin.textField.font = font;
        GUI.skin.textArea.font = font;
        GUI.skin.box.font = font;
    }
}
