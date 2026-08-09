using UnityEngine;

// 실제 Player 프리팹을 구동하는 TutorialPlayerPreview의 출력을 공통 픽셀 UI에 표시한다.
public sealed class TutorialKeyGuide : MonoBehaviour
{
    // 기존 Tutorial 씬의 직렬화 데이터 호환용. 미리보기 렌더링에는 사용하지 않는다.
    [HideInInspector] public Texture2D[] previewSheets;

    private enum PreviewKind
    {
        Move,
        DoubleJump,
        Dash,
        DropThrough,
        Slam,
        Shooting,
        Item,
        Crouch
    }

    private readonly struct KeyHint
    {
        public readonly string Key;
        public readonly string Label;
        public readonly PreviewKind Kind;

        public KeyHint(string key, string label, PreviewKind kind)
        {
            Key = key;
            Label = label;
            Kind = kind;
        }
    }

    private static readonly KeyHint[] Hints =
    {
        new KeyHint("LEFT / RIGHT", "이동", PreviewKind.Move),
        new KeyHint("SPACE", "2단 점프", PreviewKind.DoubleJump),
        new KeyHint("SHIFT", "대시", PreviewKind.Dash),
        new KeyHint("DOWN + SPACE", "발판 아래로 내려가기", PreviewKind.DropThrough),
        new KeyHint("DOWN + SHIFT", "공중 하강 슬램", PreviewKind.Slam),
        new KeyHint("Z / X", "기본 사격", PreviewKind.Shooting),
        new KeyHint("ITEM", "연사 무기 아이템", PreviewKind.Item),
        new KeyHint("C", "웅크리기", PreviewKind.Crouch)
    };

    private const int Columns = 4;
    private const float PanelMargin = 12f;
    private const float PanelWidth = 1123f;
    private const float ChipHeight = 62f;
    private const float ChipGap = 10f;

    private GUIStyle keyStyle;
    private GUIStyle labelStyle;
    private GUIStyle previewLabelStyle;
    private GUIStyle shootingLabelStyle;
    private int selectedHint;
    private TutorialPlayerPreview playerPreview;

    private void Awake()
    {
        selectedHint = Mathf.Clamp(selectedHint, 0, Hints.Length - 1);
        playerPreview = GetComponent<TutorialPlayerPreview>();
        if (playerPreview == null) playerPreview = gameObject.AddComponent<TutorialPlayerPreview>();
    }

    private void EnsureStyles()
    {
        if (keyStyle != null) return;
        keyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            normal = { textColor = PixelUiTheme.Gold }
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            wordWrap = true,
            normal = { textColor = PixelUiTheme.Text }
        };
        previewLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = PixelUiTheme.Accent }
        };
        shootingLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = PixelUiTheme.Text }
        };
        GameTypography.ApplyDialogueFont(keyStyle, labelStyle, previewLabelStyle, shootingLabelStyle);
    }

    private void OnGUI()
    {
        GameTypography.ApplyToCurrentSkin();
        EnsureStyles();
        selectedHint = Mathf.Clamp(selectedHint, 0, Hints.Length - 1);

        int rows = Mathf.CeilToInt(Hints.Length / (float)Columns);
        float panelWidth = Mathf.Min(PanelWidth, Screen.width - PanelMargin * 2f);
        float chipWidth = (panelWidth - 24f - ChipGap * (Columns - 1)) / Columns;
        float previewHeight = Mathf.Clamp(Screen.height * 0.42f, 130f, 260f);
        float panelHeight = previewHeight + rows * ChipHeight + (rows - 1) * ChipGap + 34f;
        float left = (Screen.width - panelWidth) * 0.5f;
        float top = PanelMargin;

        Rect panelRect = new Rect(left, top, panelWidth, panelHeight);
        PixelUiTheme.DrawPanel(panelRect);

        Rect previewRect = new Rect(left + 12f, top + 12f, panelWidth - 24f, previewHeight);
        PixelUiTheme.DrawInset(previewRect);
        DrawSelectedPreview(previewRect);

        float gridTop = previewRect.yMax + 10f;
        for (int i = 0; i < Hints.Length; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            float x = left + 12f + col * (chipWidth + ChipGap);
            float y = gridTop + row * (ChipHeight + ChipGap);
            Rect chip = new Rect(x, y, chipWidth, ChipHeight);

            PixelUiTheme.DrawInset(chip);
            if (i == selectedHint) PixelUiTheme.DrawBorder(chip, PixelUiTheme.Accent, 2f);
            if (GUI.Button(chip, GUIContent.none, GUIStyle.none) && selectedHint != i)
            {
                selectedHint = i;
                playerPreview?.SelectAction((int)Hints[i].Kind);
            }

            GUI.Label(new Rect(x + 4f, y + 3f, chipWidth - 8f, 24f), Hints[i].Key, keyStyle);
            GUI.Label(new Rect(x + 5f, y + 26f, chipWidth - 10f, ChipHeight - 29f), Hints[i].Label, labelStyle);
        }
    }

    private void DrawSelectedPreview(Rect rect)
    {
        playerPreview?.SelectAction((int)Hints[selectedHint].Kind);
        playerPreview?.EnsureRenderSize(Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height));

        if (playerPreview != null && playerPreview.Output != null)
        {
            GUI.DrawTexture(RoundRect(rect), playerPreview.Output, ScaleMode.StretchToFill, false);
        }
        else
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.11f, 0.12f, 0.15f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        PixelUiTheme.DrawBorder(rect, new Color(PixelUiTheme.Border.r, PixelUiTheme.Border.g, PixelUiTheme.Border.b, 0.72f), 1f);
        GUI.Label(new Rect(rect.x + 8f, rect.y + 3f, rect.width - 16f, 22f), Hints[selectedHint].Label, previewLabelStyle);
        if (Hints[selectedHint].Kind == PreviewKind.Shooting)
        {
            string[] labels =
            {
                "Z / X  오른쪽 사격",
                "DOWN + Z / X  아래 사격",
                "UP + Z / X  위 사격",
                "RIGHT + Z / X  이동 사격"
            };
            Rect guideRect = new Rect(rect.x + 12f, rect.y + 28f, rect.width - 24f, 40f);
            PixelUiTheme.DrawInset(guideRect);
            float cellWidth = guideRect.width * 0.5f;
            float cellHeight = guideRect.height * 0.5f;
            int shootingMode = playerPreview != null ? playerPreview.ShootingMode : 0;
            for (int i = 0; i < labels.Length; i++)
            {
                shootingLabelStyle.normal.textColor = i == shootingMode
                    ? PixelUiTheme.Accent
                    : PixelUiTheme.Text;
                int column = i % 2;
                int row = i / 2;
                GUI.Label(
                    new Rect(guideRect.x + column * cellWidth, guideRect.y + row * cellHeight, cellWidth, cellHeight),
                    (i == shootingMode ? "> " : "  ") + labels[i],
                    shootingLabelStyle);
            }
        }
    }

    private static Rect RoundRect(Rect rect)
    {
        return new Rect(
            Mathf.Round(rect.x),
            Mathf.Round(rect.y),
            Mathf.Round(rect.width),
            Mathf.Round(rect.height));
    }
}
