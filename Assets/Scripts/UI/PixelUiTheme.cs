using UnityEngine;

public static class PixelUiTheme
{
    public static readonly Color Panel = new Color(0.025f, 0.045f, 0.10f, 0.96f);
    public static readonly Color PanelSoft = new Color(0.035f, 0.065f, 0.14f, 0.96f);
    public static readonly Color Border = new Color(0.64f, 0.86f, 1f, 1f);
    public static readonly Color Text = Color.white;
    public static readonly Color Accent = new Color(0.48f, 0.86f, 1f, 1f);
    public static readonly Color Gold = new Color(1f, 0.82f, 0.22f, 1f);
    public static readonly Color Danger = new Color(0.68f, 0.26f, 0.28f, 1f);

    private static GUIStyle titleStyle;
    private static GUIStyle bodyStyle;
    private static GUIStyle centerStyle;
    private static GUIStyle smallCenterStyle;
    private static GUIStyle hintStyle;

    public static Matrix4x4 BeginReferenceCanvas(float contentScale = 1f)
    {
        GameTypography.ApplyToCurrentSkin();
        EnsureStyles();
        Matrix4x4 previous = GUI.matrix;
        float scale = Mathf.Max(1f, Mathf.Floor(Mathf.Min(Screen.width / 480f, Screen.height / 270f))) * Mathf.Clamp(contentScale, 0.5f, 1f);
        float left = (Screen.width - 480f * scale) * 0.5f;
        float top = (Screen.height - 270f * scale) * 0.5f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(left, top, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        return previous;
    }

    public static void EndReferenceCanvas(Matrix4x4 previous) => GUI.matrix = previous;

    public static void DrawBackdrop()
    {
        Fill(new Rect(0f, 0f, 480f, 270f), new Color(0f, 0f, 0f, 0.72f));
    }

    public static void DrawPanel(Rect rect, Color? border = null)
    {
        Fill(new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.78f));
        Fill(rect, Panel);
        DrawBorder(rect, border ?? Border, 2f);
    }

    public static void DrawInset(Rect rect)
    {
        Fill(rect, PanelSoft);
        DrawBorder(rect, new Color(Border.r, Border.g, Border.b, 0.72f), 1f);
    }

    public static void Title(Rect rect, string text, Color? color = null)
    {
        Color previous = titleStyle.normal.textColor;
        titleStyle.normal.textColor = color ?? Text;
        GUI.Label(rect, text, titleStyle);
        titleStyle.normal.textColor = previous;
    }

    public static void Label(Rect rect, string text, TextAnchor alignment = TextAnchor.MiddleLeft, Color? color = null, bool wrap = false)
    {
        bodyStyle.alignment = alignment;
        bodyStyle.wordWrap = wrap;
        bodyStyle.normal.textColor = color ?? Text;
        GUI.Label(rect, text, bodyStyle);
    }

    public static bool Button(Rect rect, string text, bool selected, Color? selectedColor = null)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        bool active = selected || hover;
        if (active)
        {
            Fill(rect, new Color(0.08f, 0.20f, 0.34f, 0.95f));
            DrawBorder(rect, selectedColor ?? Accent, 1f);
        }
        else
        {
            Fill(rect, new Color(0.02f, 0.04f, 0.09f, 0.72f));
        }
        centerStyle.normal.textColor = active ? selectedColor ?? Accent : Text;
        GUI.Label(rect, (selected ? "▶ " : string.Empty) + text, centerStyle);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    public static bool SmallButton(Rect rect, string text, bool selected)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        bool active = selected || hover;
        Fill(rect, active ? new Color(0.08f, 0.20f, 0.34f, 0.95f) : new Color(0.02f, 0.04f, 0.09f, 0.72f));
        if (active) DrawBorder(rect, Accent, 1f);
        smallCenterStyle.normal.textColor = active ? Accent : Text;
        GUI.Label(rect, text, smallCenterStyle);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    public static bool Tab(Rect rect, string text, bool active)
    {
        Fill(rect, active ? new Color(0.08f, 0.22f, 0.36f, 1f) : new Color(0.02f, 0.04f, 0.09f, 0.86f));
        DrawBorder(rect, active ? Accent : new Color(Border.r, Border.g, Border.b, 0.45f), 1f);
        centerStyle.normal.textColor = active ? Accent : Text;
        GUI.Label(rect, text, centerStyle);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    public static float Slider(Rect rect, float value, bool selected)
    {
        Rect track = new Rect(rect.x, rect.center.y - 2f, rect.width, 4f);
        Fill(track, new Color(0f, 0f, 0f, 0.8f));
        Fill(new Rect(track.x, track.y, track.width * Mathf.Clamp01(value), track.height), Accent);
        float knobX = Mathf.Lerp(rect.x, rect.xMax, Mathf.Clamp01(value));
        Rect knob = new Rect(knobX - 3f, rect.center.y - 6f, 6f, 12f);
        Fill(knob, selected ? Color.white : Border);
        DrawBorder(knob, Color.black, 1f);
        Event current = Event.current;
        if (rect.Contains(current.mousePosition) && (current.type == EventType.MouseDown || current.type == EventType.MouseDrag))
        {
            value = Mathf.Clamp01((current.mousePosition.x - rect.x) / rect.width);
            current.Use();
        }
        return value;
    }

    public static bool Toggle(Rect rect, string label, bool value, bool selected)
    {
        Rect box = new Rect(rect.x, rect.center.y - 6f, 12f, 12f);
        Fill(box, value ? Accent : new Color(0f, 0f, 0f, 0.82f));
        DrawBorder(box, selected ? Color.white : Border, 1f);
        Label(new Rect(rect.x + 18f, rect.y, rect.width - 18f, rect.height), label, TextAnchor.MiddleLeft, selected ? Accent : Text);
        if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) value = !value;
        return value;
    }

    public static void Hint(Rect rect, string text)
    {
        hintStyle.normal.textColor = Border;
        GUI.Label(rect, text, hintStyle);
    }

    public static void DrawBorder(Rect rect, Color color, float thickness)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
        Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
        bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 11, fontStyle = FontStyle.Normal };
        centerStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
        smallCenterStyle = new GUIStyle(centerStyle) { fontSize = 8, wordWrap = false, clipping = TextClipping.Clip };
        hintStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleRight, fontSize = 9, fontStyle = FontStyle.Bold };
        GameTypography.ApplyDialogueFont(titleStyle, bodyStyle, centerStyle, smallCenterStyle, hintStyle);
    }
}
