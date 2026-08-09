using UnityEngine;

// Stage 1과 Stage 2가 함께 사용하는 픽셀 HUD 렌더러.
public static class HudPixelGauges
{
    private const float HeartSize = 36f * 1.3f;
    private const float HeartGap = 3f;
    private const int SegmentCount = 24;

    // boss1_hpbar.png의 위쪽 기준 픽셀 좌표.
    private static readonly Rect EmptyBarSource = new Rect(15f, 50f, 336f, 36f);
    private static readonly Rect StandardFillSource = new Rect(53f, 93f, 10f, 20f);
    private static readonly Rect LastFillSource = new Rect(67f, 93f, 12f, 20f);
    private const float BossBarScale = 1.12f;
    private const float BossBarVerticalOffset = 4f;
    private const float FirstSegmentX = 38f;
    private const float SegmentY = 8f;
    private const float SegmentPitch = 12f;

    private static Texture2D heartTexture;
    private static Texture2D bossPurificationBarTexture;

    private static Texture2D HeartTexture =>
        heartTexture ??= Resources.Load<Texture2D>("UI/HUD/Player/player_heart_full");

    private static Texture2D BossPurificationBarTexture =>
        bossPurificationBarTexture ??= Resources.Load<Texture2D>("UI/Boss/boss1_hpbar");

    public static void DrawPlayerHearts(float margin, int currentHealth, int maxHealth, bool bottomAligned = false)
    {
        Texture2D heart = HeartTexture;
        if (heart == null) return;

        int visibleHearts = Mathf.Clamp(currentHealth, 0, maxHealth);
        float top = bottomAligned
            ? Screen.height - margin - HeartSize
            : margin + BossBarVerticalOffset;
        GUI.color = Color.white;
        for (int i = 0; i < visibleHearts; i++)
        {
            float x = margin + i * (HeartSize + HeartGap);
            GUI.DrawTexture(new Rect(x, top, HeartSize, HeartSize), heart, ScaleMode.ScaleToFit, true);
        }
        GUI.color = Color.white;
    }

    public static void DrawBossPurificationMeter(
        float screenWidth,
        float margin,
        int currentHealth,
        int maxHealth,
        GUIStyle labelStyle,
        string stageLabel,
        float visualScale = 1f,
        GUIStyle stageLabelStyle = null)
    {
        Texture2D sheet = BossPurificationBarTexture;
        if (sheet == null || maxHealth <= 0) return;

        // 남은 보스 체력을 24칸에 비례시킨다.
        // 체력이 1 이상이면 최소 한 칸을 유지하고, 0일 때 모든 칸이 사라진다.
        float healthRatio = Mathf.Clamp01(currentHealth / (float)maxHealth);
        int filledSegments = currentHealth <= 0
            ? 0
            : Mathf.CeilToInt(healthRatio * SegmentCount);
        int percent = Mathf.RoundToInt(healthRatio * 100f);

        float uiMultiplier = Mathf.Max(1f, visualScale);
        float percentWidth = 90f * uiMultiplier;
        float groupGap = 8f * uiMultiplier;
        float scale = BossBarScale * uiMultiplier;
        float maximumBarWidth = Mathf.Max(100f, screenWidth - percentWidth - groupGap - 16f);
        scale = Mathf.Min(scale, maximumBarWidth / EmptyBarSource.width);
        float barWidth = EmptyBarSource.width * scale;
        float barHeight = EmptyBarSource.height * scale;
        float left = (screenWidth - barWidth) * 0.5f;
        float top = margin + BossBarVerticalOffset;
        Rect barRect = new Rect(left, top, barWidth, barHeight);

        GUI.color = Color.white;
        DrawSheetRegion(barRect, sheet, EmptyBarSource);

        // 남은 체력의 1~23번째 칸은 일반 파란색 칸 이미지를 반복해서 사용한다.
        int standardSegments = Mathf.Min(filledSegments, SegmentCount - 1);
        for (int i = 0; i < standardSegments; i++)
        {
            Rect segmentRect = new Rect(
                left + (FirstSegmentX + i * SegmentPitch) * scale,
                top + SegmentY * scale,
                StandardFillSource.width * scale,
                StandardFillSource.height * scale);
            DrawSheetRegion(segmentRect, sheet, StandardFillSource);
        }

        // 체력이 가득 찬 24칸 상태에서는 우측 끝 전용 이미지를 사용한다.
        if (filledSegments == SegmentCount)
        {
            Rect lastSegmentRect = new Rect(
                left + (FirstSegmentX + (SegmentCount - 1) * SegmentPitch) * scale,
                top + SegmentY * scale,
                LastFillSource.width * scale,
                LastFillSource.height * scale);
            DrawSheetRegion(lastSegmentRect, sheet, LastFillSource);
        }

        if (labelStyle != null)
        {
            DrawOutlinedLabel(new Rect(left + barWidth + groupGap, top, percentWidth, barHeight), percent + "%", labelStyle, 2f);
            GUIStyle resolvedStageStyle = stageLabelStyle ?? labelStyle;
            float stageLabelGap = 7f * uiMultiplier;
            DrawOutlinedLabel(
                new Rect(left, top + barHeight + stageLabelGap, barWidth, 22f * uiMultiplier),
                stageLabel,
                resolvedStageStyle,
                2f);
        }

        GUI.color = Color.white;
    }

    private static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, float thickness)
    {
        Color originalColor = style.normal.textColor;
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x - thickness, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + thickness, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y - thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y + thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x - thickness, rect.y - thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + thickness, rect.y - thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x - thickness, rect.y + thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + thickness, rect.y + thickness, rect.width, rect.height), text, style);
        style.normal.textColor = originalColor;
        GUI.Label(rect, text, style);
    }

    private static void DrawSheetRegion(Rect destination, Texture2D sheet, Rect sourcePixels)
    {
        Rect uv = new Rect(
            sourcePixels.x / sheet.width,
            1f - (sourcePixels.y + sourcePixels.height) / sheet.height,
            sourcePixels.width / sheet.width,
            sourcePixels.height / sheet.height);
        GUI.DrawTextureWithTexCoords(destination, sheet, uv, true);
    }
}
