using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class WorldMapScreen : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private Image[] stageNodes;
    [SerializeField] private Image[] roadDots;
    [SerializeField] private RectTransform mapRoot;
    [SerializeField] private Image homeNode;
    [SerializeField] private Image playerMarker;
    [SerializeField] private Sprite[] playerIdleFrames;
    [SerializeField] private Sprite[] playerRunFrames;
    [SerializeField] private Material lockedStageMaterial;
    [SerializeField] private float fadeDuration = 0.85f;

    private const float PlatformStandingOffset = 24f;
    private const float RoadStandingOffset = 19f;
    private static readonly int[][] HomeToStageRoutes =
    {
        new int[0],
        new[] { 10, 11, 12, 13, 9, 8, 7, 6, 5, 4 },
        new[] { 10, 11, 12, 13, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 },
        new[] { 30, 31, 32, 33, 29, 28, 27, 26, 24, 25 },
        new[] { 10, 11, 12, 13, 14, 15, 16 },
        new[] { 17, 18, 19 },
        new[] { 30, 31, 32, 33, 34, 35, 36 }
    };

    private int selectedStage;
    private bool loading;
    private bool introPlaying;
    private bool selectionMoving;
    private bool playerEnteringStage;
    private float lockedMessageUntil;
    private GUIStyle lockedStyle;
    private float[] nodeHomeY;
    private float homeNodeBaseY;
    private bool stageOneGuided;
    private int journeyFromStage = -1;
    private int journeyToStage = -1;
    private int journeyStep;
    private int[] journeyRoute;
    private bool journeyForwardIsRight;

    private void Awake()
    {
        TitleWorldMapMusic.EnsurePlaying();
        EnsureEventSystem();
        EnsureRuntimeReferences();
    }

    private void Start()
    {
        UpdateSelectionVisuals();
        if (playerMarker != null)
        {
            playerMarker.rectTransform.localScale = Vector3.zero;
            playerMarker.color = new Color(1f, 1f, 1f, 0f);
        }
        SetFadeAlpha(1f);
        StartCoroutine(IntroZoomRoutine());
    }

    private void Update()
    {
        AnimateSelection();
        AnimatePlayerMarker();
        if (loading || introPlaying || selectionMoving || Keyboard.current == null) return;
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            RequestArrowStep(false);
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            RequestArrowStep(true);
        if (Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            TryEnterSelectedStage();
    }

    public void SelectStage(int stageNumber)
    {
        if (stageNumber < 0 || stageNumber > 1) return;
        int target = stageNumber;
        if (target == selectedStage || selectionMoving || introPlaying) return;
        StartJourney(target, selectedStage == 1 && target == 0);
        RequestJourneyStep(true);
    }

    public void SelectAndEnterStage(int stageNumber)
    {
        if (stageNumber < 0 || stageNumber > 1) return;
        int target = stageNumber;
        if (target == selectedStage)
        {
            TryEnterSelectedStage();
            return;
        }
        if (!selectionMoving && !introPlaying) SelectStage(target);
    }

    private void TryEnterSelectedStage()
    {
        if (journeyRoute != null) return;
        if (selectedStage == 0)
        {
            EnterStage("Tutorial");
            return;
        }
        if (selectedStage >= 2)
        {
            lockedMessageUntil = Time.unscaledTime + 1.6f;
            GameSfx.Play(GameSfxId.Button);
            return;
        }

        EnterStage("Stage 1");
    }

    public void EnterStage(string sceneName)
    {
        if (loading || string.IsNullOrWhiteSpace(sceneName)) return;
        GameSfx.Play(GameSfxId.Button);
        loading = true;
        playerEnteringStage = true;
        StartCoroutine(EnterStageRoutine(sceneName));
    }

    private void AnimateSelection()
    {
        Image selected = selectedStage == 0
            ? homeNode
            : stageNodes != null && selectedStage <= stageNodes.Length ? stageNodes[selectedStage - 1] : null;
        if (selected == null) return;
        float bob = Mathf.Sin(Time.unscaledTime * 5f) * 4f;
        RectTransform rect = selected.rectTransform;
        Vector2 position = rect.anchoredPosition;
        float baseY = selectedStage == 0
            ? homeNodeBaseY
            : nodeHomeY != null && selectedStage - 1 < nodeHomeY.Length ? nodeHomeY[selectedStage - 1] : position.y;
        position.y = baseY + bob;
        rect.anchoredPosition = position;
        float glow = 0.88f + 0.12f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));
        selected.color = Color.Lerp(new Color(1f, 0.58f, 0.08f), new Color(1f, 0.95f, 0.38f), glow);
    }

    private void UpdateSelectionVisuals()
    {
        if (stageNodes == null) return;
        if (homeNode != null)
        {
            homeNode.rectTransform.anchoredPosition = new Vector2(homeNode.rectTransform.anchoredPosition.x, homeNodeBaseY);
            homeNode.rectTransform.localScale = selectedStage == 0 ? Vector3.one * 1.12f : Vector3.one;
            homeNode.color = selectedStage == 0 ? new Color(1f, 0.82f, 0.18f) : Color.white;
        }
        for (int i = 0; i < stageNodes.Length; i++)
        {
            if (stageNodes[i] == null) continue;
            RectTransform rect = stageNodes[i].rectTransform;
            if (nodeHomeY != null && i < nodeHomeY.Length)
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, nodeHomeY[i]);
            bool selected = i == selectedStage - 1;
            bool guidedStageOne = i == 0 && stageOneGuided;
            rect.localScale = selected ? Vector3.one * 1.12f : Vector3.one;
            stageNodes[i].material = selected || guidedStageOne ? null : lockedStageMaterial;
            stageNodes[i].color = selected || guidedStageOne ? new Color(1f, 0.82f, 0.18f) : Color.white;
        }
        MoveMarkerToSelectedNode();
    }

    private IEnumerator IntroZoomRoutine()
    {
        introPlaying = true;
        Vector2 selectedPosition = homeNode != null ? homeNode.rectTransform.anchoredPosition : Vector2.zero;
        const float startScale = 2.25f;
        const float duration = 1.1f;
        if (mapRoot != null)
        {
            mapRoot.localScale = Vector3.one * startScale;
            mapRoot.anchoredPosition = -selectedPosition * startScale;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetFadeAlpha(1f - t);
            if (mapRoot != null)
            {
                float scale = Mathf.Lerp(startScale, 1f, t);
                mapRoot.localScale = Vector3.one * scale;
                mapRoot.anchoredPosition = Vector2.Lerp(-selectedPosition * startScale, Vector2.zero, t);
            }
            yield return null;
        }
        SetFadeAlpha(0f);
        if (mapRoot != null)
        {
            mapRoot.localScale = Vector3.one;
            mapRoot.anchoredPosition = Vector2.zero;
        }
        if (playerMarker != null && homeNode != null)
            yield return DropMarkerOntoHomeRoutine();
        yield return GuideStageOneRouteRoutine();
        introPlaying = false;
    }

    private void RequestArrowStep(bool right)
    {
        if (journeyRoute == null)
        {
            int target;
            if (selectedStage == 0)
            {
                if (right) return;
                target = 1;
            }
            else if (selectedStage == 1)
            {
                if (!right) return;
                target = 0;
            }
            else return;
            if (target == selectedStage) return;
            StartJourney(target, right);
        }
        RequestJourneyStep(right == journeyForwardIsRight);
    }

    private void StartJourney(int targetStage, bool forwardIsRight)
    {
        journeyFromStage = selectedStage;
        journeyToStage = targetStage;
        journeyRoute = BuildRoadRoute(journeyFromStage, journeyToStage);
        journeyStep = 0;
        journeyForwardIsRight = forwardIsRight;
    }

    private void RequestJourneyStep(bool forward)
    {
        if (journeyRoute == null || selectionMoving) return;
        int maximumStep = journeyRoute.Length + 1;
        int nextStep = Mathf.Clamp(journeyStep + (forward ? 1 : -1), 0, maximumStep);
        if (nextStep == journeyStep) return;
        StartCoroutine(MoveJourneyStepRoutine(nextStep));
    }

    private IEnumerator MoveJourneyStepRoutine(int nextStep)
    {
        selectionMoving = true;
        GameSfx.Play(GameSfxId.Button);
        Vector2 destination;
        if (nextStep == 0)
        {
            destination = GetStageStandingPosition(journeyFromStage);
        }
        else if (nextStep == journeyRoute.Length + 1)
        {
            destination = GetStageStandingPosition(journeyToStage);
        }
        else
        {
            int roadIndex = journeyRoute[nextStep - 1];
            if (roadDots == null || roadIndex < 0 || roadIndex >= roadDots.Length || roadDots[roadIndex] == null)
            {
                selectionMoving = false;
                yield break;
            }
            destination = roadDots[roadIndex].rectTransform.anchoredPosition + Vector2.up * RoadStandingOffset;
        }

        if (playerMarker != null) yield return TeleportMarkerRoutine(destination);
        journeyStep = nextStep;
        if (journeyStep == journeyRoute.Length + 1)
        {
            selectedStage = journeyToStage;
            ClearJourney();
            UpdateSelectionVisuals();
        }
        else if (journeyStep == 0)
        {
            ClearJourney();
            UpdateSelectionVisuals();
        }
        selectionMoving = false;
    }

    private Vector2 GetStageStandingPosition(int stage)
    {
        RectTransform target = stage == 0
            ? homeNode != null ? homeNode.rectTransform : null
            : stageNodes != null && stage <= stageNodes.Length ? stageNodes[stage - 1].rectTransform : null;
        return target != null ? target.anchoredPosition + Vector2.up * PlatformStandingOffset : Vector2.zero;
    }

    private void ClearJourney()
    {
        journeyFromStage = -1;
        journeyToStage = -1;
        journeyStep = 0;
        journeyRoute = null;
    }

    private IEnumerator DropMarkerOntoHomeRoutine()
    {
        Vector2 landingPosition = new Vector2(homeNode.rectTransform.anchoredPosition.x, homeNodeBaseY + PlatformStandingOffset);
        Vector2 airPosition = landingPosition + new Vector2(0f, 95f);
        playerMarker.rectTransform.anchoredPosition = airPosition;
        yield return PopInMarkerRoutine();

        const float fallDuration = 0.58f;
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            playerMarker.rectTransform.anchoredPosition = Vector2.Lerp(airPosition, landingPosition, t * t);
            yield return null;
        }
        playerMarker.rectTransform.anchoredPosition = landingPosition;

        const float settleDuration = 0.14f;
        elapsed = 0f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / settleDuration));
            playerMarker.rectTransform.localScale = new Vector3(
                Mathf.Lerp(1.18f, 1f, t),
                Mathf.Lerp(0.76f, 1f, t),
                1f);
            yield return null;
        }
        playerMarker.rectTransform.localScale = Vector3.one;
    }

    private IEnumerator GuideStageOneRouteRoutine()
    {
        int[] route = HomeToStageRoutes[1];
        for (int i = 0; i < route.Length; i++)
        {
            int roadIndex = route[i];
            if (roadDots == null || roadIndex >= roadDots.Length || roadDots[roadIndex] == null) continue;
            Image dot = roadDots[roadIndex];
            dot.material = null;
            const float duration = 0.13f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                dot.rectTransform.localScale = Vector3.one * (1f + pulse * 0.75f);
                dot.color = Color.Lerp(Color.white, new Color(1f, 0.78f, 0.12f), pulse);
                yield return null;
            }
            dot.rectTransform.localScale = Vector3.one;
            dot.color = Color.white;
            yield return new WaitForSecondsRealtime(0.035f);
        }

        stageOneGuided = true;
        if (stageNodes == null || stageNodes.Length == 0 || stageNodes[0] == null) yield break;
        Image stageOne = stageNodes[0];
        stageOne.material = null;
        const float stageDuration = 0.32f;
        float stageElapsed = 0f;
        while (stageElapsed < stageDuration)
        {
            stageElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(stageElapsed / stageDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            stageOne.rectTransform.localScale = Vector3.one * (1f + pulse * 0.24f);
            stageOne.color = Color.Lerp(new Color(1f, 0.82f, 0.18f), Color.white, pulse);
            yield return null;
        }
        stageOne.rectTransform.localScale = Vector3.one;
        stageOne.color = new Color(1f, 0.82f, 0.18f);
    }

    private IEnumerator TeleportMarkerRoutine(Vector2 destination)
    {
        const float vanishDuration = 0.055f;
        float elapsed = 0f;
        while (elapsed < vanishDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / vanishDuration);
            playerMarker.rectTransform.localScale = new Vector3(1f + t * 0.2f, 1f - t, 1f);
            playerMarker.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        playerMarker.rectTransform.anchoredPosition = destination;
        const float appearDuration = 0.075f;
        elapsed = 0f;
        while (elapsed < appearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / appearDuration);
            playerMarker.rectTransform.localScale = new Vector3(Mathf.Lerp(1.2f, 1f, t), t, 1f);
            playerMarker.color = new Color(1f, 1f, 1f, t);
            yield return null;
        }
        playerMarker.rectTransform.localScale = Vector3.one;
        playerMarker.color = Color.white;
    }

    private static int[] BuildRoadRoute(int fromStage, int toStage)
    {
        int[] from = HomeToStageRoutes[Mathf.Clamp(fromStage, 0, HomeToStageRoutes.Length - 1)];
        int[] to = HomeToStageRoutes[Mathf.Clamp(toStage, 0, HomeToStageRoutes.Length - 1)];
        int common = 0;
        while (common < from.Length && common < to.Length && from[common] == to[common]) common++;
        var route = new System.Collections.Generic.List<int>();
        for (int i = from.Length - 1; i >= common; i--) route.Add(from[i]);
        if (common > 0 && fromStage != 0 && toStage != 0) route.Add(from[common - 1]);
        for (int i = common; i < to.Length; i++) route.Add(to[i]);
        return route.ToArray();
    }

    private IEnumerator PopOutMarkerRoutine()
    {
        const float duration = 0.14f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            playerMarker.rectTransform.localScale = new Vector3(
                Mathf.Lerp(1f, 1.28f, t),
                Mathf.Lerp(1f, 0f, t),
                1f);
            playerMarker.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        playerMarker.rectTransform.localScale = Vector3.zero;
        playerMarker.color = new Color(1f, 1f, 1f, 0f);
    }

    private IEnumerator PopInMarkerRoutine()
    {
        const float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = t < 0.72f
                ? Mathf.Lerp(0f, 1.2f, Mathf.SmoothStep(0f, 1f, t / 0.72f))
                : Mathf.Lerp(1.2f, 1f, (t - 0.72f) / 0.28f);
            playerMarker.rectTransform.localScale = new Vector3(scale, Mathf.Min(1.15f, scale * 1.08f), 1f);
            playerMarker.color = new Color(1f, 1f, 1f, t);
            yield return null;
        }
        playerMarker.rectTransform.localScale = Vector3.one;
        playerMarker.color = Color.white;
    }

    private void MoveMarkerToSelectedNode()
    {
        if (playerMarker == null) return;
        RectTransform target = selectedStage == 0
            ? homeNode != null ? homeNode.rectTransform : null
            : stageNodes != null && selectedStage <= stageNodes.Length ? stageNodes[selectedStage - 1].rectTransform : null;
        if (target != null) playerMarker.rectTransform.anchoredPosition = target.anchoredPosition + Vector2.up * PlatformStandingOffset;
    }

    private void AnimatePlayerMarker()
    {
        if (playerMarker == null) return;
        Sprite[] frames = playerEnteringStage ? playerRunFrames : playerIdleFrames;
        if (frames == null || frames.Length == 0) return;
        float speed = playerEnteringStage ? 11f : 7f;
        int frame = Mathf.FloorToInt(Time.unscaledTime * speed) % frames.Length;
        playerMarker.sprite = frames[frame];
    }

    private IEnumerator EnterStageRoutine(string sceneName)
    {
        yield return new WaitForSecondsRealtime(0.12f);
        if (fadeImage != null) yield return Fade(0f, 1f);
        SceneTransition.Load(sceneName);
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        SetFadeAlpha(from);
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(fadeDuration, 0.01f))));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
        fadeImage.raycastTarget = alpha > 0.01f;
    }

    private void OnGUI()
    {
        GameTypography.ApplyToCurrentSkin();
        if (Time.unscaledTime >= lockedMessageUntil) return;
        if (lockedStyle == null)
        {
            lockedStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
        Rect rect = new Rect(Screen.width * 0.5f - 190f, Screen.height - 76f, 380f, 42f);
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(rect, "아직 열리지 않은 스테이지입니다", lockedStyle);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
        DontDestroyOnLoad(eventSystemObject);
    }

    private void EnsureRuntimeReferences()
    {
        if (mapRoot == null && stageNodes != null && stageNodes.Length > 0 && stageNodes[0] != null)
            mapRoot = stageNodes[0].rectTransform.parent as RectTransform;
        if (mapRoot != null) mapRoot.anchoredPosition = Vector2.zero;
        if (homeNode == null && mapRoot != null)
        {
            Transform foundHome = mapRoot.Find("Stage0Start");
            if (foundHome != null) homeNode = foundHome.GetComponent<Image>();
        }
        if (homeNode != null) homeNodeBaseY = homeNode.rectTransform.anchoredPosition.y;

        if ((roadDots == null || roadDots.Length == 0) && mapRoot != null)
        {
            roadDots = new Image[37];
            for (int i = 0; i < roadDots.Length; i++)
            {
                Transform dot = mapRoot.Find("RoadDot_" + i);
                if (dot != null) roadDots[i] = dot.GetComponent<Image>();
            }
        }
        int[] guidedRoute = HomeToStageRoutes[1];
        for (int i = 0; i < guidedRoute.Length; i++)
        {
            int index = guidedRoute[i];
            if (roadDots != null && index < roadDots.Length && roadDots[index] != null)
                roadDots[index].material = lockedStageMaterial;
        }

        if (stageNodes != null)
        {
            nodeHomeY = new float[stageNodes.Length];
            for (int i = 0; i < stageNodes.Length; i++)
            {
                Image node = stageNodes[i];
                if (node == null) continue;
                nodeHomeY[i] = node.rectTransform.anchoredPosition.y;
                if (lockedStageMaterial == null && node.material != null) lockedStageMaterial = node.material;
            }
        }

        if (playerIdleFrames == null || playerIdleFrames.Length == 0)
        {
            Texture2D sheet = Resources.Load<Texture2D>("UI/WorldMap/player_idle");
            if (sheet != null)
            {
                sheet.filterMode = FilterMode.Point;
                playerIdleFrames = new Sprite[Mathf.Max(1, sheet.width / 30)];
                for (int i = 0; i < playerIdleFrames.Length; i++)
                    playerIdleFrames[i] = Sprite.Create(sheet, new Rect(i * 30f, 0f, 30f, 30f), new Vector2(0.5f, 0.5f), 30f);
            }
        }
        if (playerRunFrames == null || playerRunFrames.Length == 0)
        {
            Texture2D sheet = Resources.Load<Texture2D>("UI/WorldMap/player_run");
            if (sheet != null)
            {
                sheet.filterMode = FilterMode.Point;
                playerRunFrames = new Sprite[Mathf.Max(1, sheet.width / 30)];
                for (int i = 0; i < playerRunFrames.Length; i++)
                    playerRunFrames[i] = Sprite.Create(sheet, new Rect(i * 30f, 0f, 30f, 30f), new Vector2(0.5f, 0.5f), 30f);
            }
        }

        if (playerMarker == null && mapRoot != null && playerIdleFrames != null && playerIdleFrames.Length > 0)
        {
            GameObject markerObject = new GameObject("WorldMapPlayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            markerObject.transform.SetParent(mapRoot, false);
            playerMarker = markerObject.GetComponent<Image>();
            playerMarker.sprite = playerIdleFrames[0];
            playerMarker.preserveAspect = true;
            playerMarker.raycastTarget = false;
            playerMarker.rectTransform.anchorMin = playerMarker.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            playerMarker.rectTransform.sizeDelta = new Vector2(54f, 54f);
        }
    }
}
