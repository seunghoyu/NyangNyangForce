using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LoadingScreenController : MonoBehaviour
{
    private const float LoadingBarFrameHeight = 36f;
    private const float LoadingBarDisplayWidth = 339f;
    private const float RunnerNativeSize = 90f;
    private const float MinimumVisibleSeconds = 2.8f;
    private const string BarResourcePath = "UI/Loading/loadingbar";
    private const string GaugeResourcePath = "UI/Loading/loadingbar_effect";
    private static readonly string[] RunnerResourcePaths =
    {
        "UI/Loading/player_run",
        "UI/Loading/npc_rabbit_walk",
        "UI/Loading/boss1_npc_walk"
    };
    private static readonly int[] RunnerFrameCounts = { 8, 4, 4 };
    private static readonly bool[] RunnerFlipHorizontally = { false, false, true };
    private static readonly string[] LoadingTips =
    {
        "수철햄은 김냥이와 같은 동아리의 선배예요.",
        "수철햄은 쳇바퀴홍보학과 3학년이에요.",
        "한토롱은 당근유통경제학과 2학년이에요.",
        "김고양과 한토롱은 동물 다큐멘터리 촬영 현장에서 아르바이트하다 친해졌어요."
    };
    private static int previousTipIndex = -1;
    private static int previousRunnerIndex = -1;

    private float displayedProgress;
    private GUIStyle loadingStyle;
    private GUIStyle percentStyle;
    private GUIStyle tipStyle;
    private Texture2D barTexture;
    private Texture2D gaugeTexture;
    private Sprite[] gaugeFrames;
    private Texture2D runnerTexture;
    private int runnerFrameCount;
    private bool runnerFlipHorizontally;
    private string selectedTip;

    private void Awake()
    {
        barTexture = Resources.Load<Texture2D>(BarResourcePath);
        gaugeTexture = Resources.Load<Texture2D>(GaugeResourcePath);
        gaugeFrames = Resources.LoadAll<Sprite>(GaugeResourcePath);
        System.Array.Sort(gaugeFrames, (left, right) => string.CompareOrdinal(left.name, right.name));
        int runnerIndex = previousRunnerIndex < 0
            ? Random.Range(0, RunnerResourcePaths.Length)
            : (previousRunnerIndex + Random.Range(1, RunnerResourcePaths.Length)) % RunnerResourcePaths.Length;
        previousRunnerIndex = runnerIndex;
        runnerTexture = Resources.Load<Texture2D>(RunnerResourcePaths[runnerIndex]);
        runnerFrameCount = RunnerFrameCounts[runnerIndex];
        runnerFlipHorizontally = RunnerFlipHorizontally[runnerIndex];
        if (barTexture != null) barTexture.filterMode = FilterMode.Point;
        if (gaugeTexture != null) gaugeTexture.filterMode = FilterMode.Point;
        if (runnerTexture != null) runnerTexture.filterMode = FilterMode.Point;
        int tipIndex;
        if (LoadingTips.Length <= 1)
        {
            tipIndex = 0;
        }
        else if (previousTipIndex < 0)
        {
            tipIndex = Random.Range(0, LoadingTips.Length);
        }
        else
        {
            tipIndex = (previousTipIndex + Random.Range(1, LoadingTips.Length)) % LoadingTips.Length;
        }
        previousTipIndex = tipIndex;
        selectedTip = LoadingTips[tipIndex];
    }

    private void Start()
    {
        StartCoroutine(LoadTargetRoutine());
    }

    private IEnumerator LoadTargetRoutine()
    {
        float startedAt = Time.realtimeSinceStartup;
        string target = SceneTransition.TargetSceneName;
        if (string.IsNullOrWhiteSpace(target)) target = "Title";

        if (!Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogError($"Loading target scene '{target}' is not registered in the active Build Profile/shared scene list.");
            yield break;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(target);
        if (operation == null)
        {
            Debug.LogError($"Failed to create an async loading operation for scene '{target}'.");
            yield break;
        }
        operation.allowSceneActivation = false;
        while (operation.progress < 0.9f || Time.realtimeSinceStartup - startedAt < MinimumVisibleSeconds)
        {
            float asyncProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01((Time.realtimeSinceStartup - startedAt) / MinimumVisibleSeconds);
            // Always show visible movement, but reserve the last 10% until loading is actually ready.
            float targetProgress = operation.progress < 0.9f
                ? Mathf.Min(0.9f, Mathf.Max(asyncProgress, timeProgress * 0.9f))
                : timeProgress;
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * 0.65f);
            yield return null;
        }

        while (displayedProgress < 1f)
        {
            displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, Time.unscaledDeltaTime * 1.5f);
            yield return null;
        }
        yield return new WaitForEndOfFrame();
        operation.allowSceneActivation = true;
    }

    private void EnsureStyles()
    {
        if (loadingStyle != null) return;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loadingStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        percentStyle = new GUIStyle(loadingStyle)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleLeft
        };
        tipStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 24,
            alignment = TextAnchor.UpperCenter,
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = new Color(0.72f, 0.88f, 1f) }
        };
        GameTypography.ApplyDialogueFont(loadingStyle, percentStyle, tipStyle);
    }

    private void OnGUI()
    {
        GameTypography.ApplyToCurrentSkin();
        EnsureStyles();
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float scale = Mathf.Clamp(Screen.height / 720f, 0.75f, 1.5f);
        float width = Mathf.Min(LoadingBarDisplayWidth * scale, Screen.width * 0.78f);
        float barHeight = LoadingBarFrameHeight * scale;
        float left = (Screen.width - width) * 0.5f;
        float top = Screen.height * 0.5f;
        float runnerSize = RunnerNativeSize * scale;
        float runnerTop = top - runnerSize - 8f * scale;
        GUI.Label(new Rect(left, runnerTop - 38f * scale, width, 30f * scale), "LOADING", loadingStyle);

        Rect barRect = new Rect(left, top, width, barHeight);
        Rect innerRect = new Rect(
            barRect.x + barRect.width * (9f / LoadingBarDisplayWidth),
            barRect.y + barRect.height * (8f / LoadingBarFrameHeight),
            barRect.width * (321f / LoadingBarDisplayWidth),
            barRect.height * (20f / LoadingBarFrameHeight));
        if (barTexture != null)
            GUI.DrawTexture(barRect, barTexture, ScaleMode.StretchToFill, true);
        else
            DrawPixelBorder(barRect, 2f * scale);
        Rect filledRect = new Rect(innerRect.x, innerRect.y, innerRect.width * displayedProgress, innerRect.height);
        if (gaugeTexture != null && filledRect.width > 0f)
            DrawTiledGauge(filledRect);

        DrawRunningPlayer(innerRect, runnerSize, runnerTop);
        GUI.Label(new Rect(barRect.xMax + 8f * scale, barRect.y, Screen.width - barRect.xMax - 8f * scale, barRect.height),
            Mathf.RoundToInt(displayedProgress * 100f) + "%", percentStyle);
        string tipText = "TIP : " + selectedTip;
        float tipWidth = Mathf.Min(Screen.width - 32f, Mathf.Max(720f, 980f * scale));
        int originalTipSize = tipStyle.fontSize;
        tipStyle.fontSize = Mathf.RoundToInt(24f * scale);
        while (tipStyle.fontSize > 12 && tipStyle.CalcSize(new GUIContent(tipText)).x > tipWidth)
            tipStyle.fontSize--;
        GUI.Label(new Rect((Screen.width - tipWidth) * 0.5f, barRect.yMax + 31f * scale, tipWidth, 34f * scale), tipText, tipStyle);
        tipStyle.fontSize = originalTipSize;
    }

    private void DrawTiledGauge(Rect rect)
    {
        if (gaugeFrames != null && gaugeFrames.Length >= 3)
        {
            DrawSlicedGauge(rect);
            return;
        }
        float tileWidth = rect.height * gaugeTexture.width / gaugeTexture.height;
        GUI.BeginGroup(rect);
        for (float x = 0f; x < rect.width; x += tileWidth)
            GUI.DrawTexture(new Rect(x, 0f, tileWidth, rect.height), gaugeTexture, ScaleMode.StretchToFill, true);
        GUI.EndGroup();
    }

    private void DrawSlicedGauge(Rect rect)
    {
        float pixelScale = rect.height / 20f;
        float leftWidth = 4f * pixelScale;
        float middleWidth = 3f * pixelScale;
        float rightWidth = 4f * pixelScale;
        GUI.BeginGroup(rect);
        DrawSpriteRegion(new Rect(0f, 0f, leftWidth, rect.height), gaugeFrames[0]);
        float rightX = Mathf.Max(leftWidth, rect.width - rightWidth);
        for (float x = leftWidth; x < rightX; x += middleWidth)
        {
            float width = Mathf.Min(middleWidth, rightX - x);
            DrawSpriteRegion(new Rect(x, 0f, width, rect.height), gaugeFrames[1]);
        }
        if (rect.width > leftWidth)
            DrawSpriteRegion(new Rect(rightX, 0f, Mathf.Min(rightWidth, rect.width - rightX), rect.height), gaugeFrames[2]);
        GUI.EndGroup();
    }

    private static void DrawSpriteRegion(Rect destination, Sprite sprite)
    {
        Rect source = sprite.textureRect;
        Rect uv = new Rect(
            source.x / sprite.texture.width,
            source.y / sprite.texture.height,
            source.width / sprite.texture.width,
            source.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(destination, sprite.texture, uv, true);
    }

    private void DrawRunningPlayer(Rect gaugeRect, float size, float top)
    {
        if (runnerTexture == null || runnerFrameCount <= 0) return;
        int frame = Mathf.FloorToInt(Time.unscaledTime * 10f) % runnerFrameCount;
        float x = Mathf.Clamp(gaugeRect.x + gaugeRect.width * displayedProgress - size * 0.5f,
            gaugeRect.x - size * 0.15f,
            gaugeRect.xMax - size * 0.85f);
        Rect playerRect = new Rect(x, top, size, size);
        Rect frameUv = runnerFlipHorizontally
            ? new Rect((frame + 1f) / runnerFrameCount, 0f, -1f / runnerFrameCount, 1f)
            : new Rect(frame / (float)runnerFrameCount, 0f, 1f / runnerFrameCount, 1f);
        GUI.DrawTextureWithTexCoords(playerRect, runnerTexture, frameUv, true);
    }

    private static void DrawPixelBorder(Rect rect, float thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }
}
