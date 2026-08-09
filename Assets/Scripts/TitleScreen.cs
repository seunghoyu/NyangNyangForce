using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TitleScreen : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Stage 1";
    private const string BackgroundResourcePath = "UI/Title/title";
    private const string HeadResourcePath = "UI/Title/title_head";
    private static readonly Color PromptColor = new Color(0.035f, 0.14f, 0.36f, 1f);

    private bool loading;
    private Texture2D backgroundTexture;
    private Texture2D headTexture;
    private Material titleSweepMaterial;
    private GUIStyle promptStyle;
    private GUIStyle promptOutlineStyle;

    private void Awake()
    {
        TitleWorldMapMusic.EnsurePlaying();
        backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
        headTexture = Resources.Load<Texture2D>(HeadResourcePath);
        Shader sweepShader = Resources.Load<Shader>("Shaders/TitleLightSweep");
        if (sweepShader == null) sweepShader = Shader.Find("CrammingHamster/TitleLightSweep");
        if (sweepShader != null) titleSweepMaterial = new Material(sweepShader);
        if (backgroundTexture != null) backgroundTexture.filterMode = FilterMode.Bilinear;
        if (headTexture != null) headTexture.filterMode = FilterMode.Point;
    }

    private void OnDestroy()
    {
        if (titleSweepMaterial != null) Destroy(titleSweepMaterial);
    }

    private void Update()
    {
        if (loading) return;

        if (WasEnterPressed())
        {
            loading = true;
            SceneTransition.Load(nextSceneName);
        }
    }

    private static bool WasEnterPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
    }

    private void EnsureStyle()
    {
        if (promptStyle != null) return;
        promptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(26, Mathf.RoundToInt(Screen.height * 0.04f)),
            fontStyle = FontStyle.Bold,
            normal = { textColor = PromptColor }
        };
        GameTypography.ApplyDialogueFont(promptStyle);
        promptOutlineStyle = new GUIStyle(promptStyle);
        promptOutlineStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        GameTypography.ApplyToCurrentSkin();
        EnsureStyle();

        if (backgroundTexture != null)
            DrawCoverTexture(backgroundTexture, new Rect(0f, 0f, Screen.width, Screen.height));

        if (headTexture != null)
        {
            // Render at the source image's native pixel size. Only shrink uniformly on small screens.
            float uniformScale = Mathf.Min(1f, (Screen.width * 0.94f) / headTexture.width);
            float width = headTexture.width * uniformScale;
            float height = headTexture.height * uniformScale;
            Rect headRect = new Rect(Screen.width * 0.068f, Screen.height * 0.148f, width, height);
            GUI.DrawTexture(headRect, headTexture, ScaleMode.StretchToFill, true);
            if (titleSweepMaterial != null)
            {
                const float sweepInterval = 2f;
                const float sweepDuration = 0.68f;
                float cycleTime = Mathf.Repeat(Time.unscaledTime, sweepInterval);
                float sweepPosition = cycleTime <= sweepDuration
                    ? Mathf.Lerp(-0.25f, 1.25f, Mathf.SmoothStep(0f, 1f, cycleTime / sweepDuration))
                    : -0.3f;
                titleSweepMaterial.SetFloat("_SweepPosition", sweepPosition);
                Graphics.DrawTexture(headRect, headTexture, titleSweepMaterial);
            }
        }

        // Slow, soft pulse: the prompt never disappears abruptly, but remains clearly readable.
        float pulse = 0.28f + 0.72f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 0.72f));
        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, loading ? 0f : pulse);
        float promptHeight = Mathf.Max(42f, Screen.height * 0.065f);
        Rect promptRect = new Rect(0f, Screen.height * 0.82f, Screen.width, promptHeight);
        const string prompt = "PRESS ENTER TO START";
        float outline = Mathf.Max(2f, Screen.height / 540f);
        GUI.Label(new Rect(promptRect.x - outline, promptRect.y, promptRect.width, promptRect.height), prompt, promptOutlineStyle);
        GUI.Label(new Rect(promptRect.x + outline, promptRect.y, promptRect.width, promptRect.height), prompt, promptOutlineStyle);
        GUI.Label(new Rect(promptRect.x, promptRect.y - outline, promptRect.width, promptRect.height), prompt, promptOutlineStyle);
        GUI.Label(new Rect(promptRect.x, promptRect.y + outline, promptRect.width, promptRect.height), prompt, promptOutlineStyle);
        GUI.Label(new Rect(promptRect.x - outline, promptRect.y - outline, promptRect.width, promptRect.height), prompt, promptOutlineStyle);
        GUI.Label(new Rect(promptRect.x + outline, promptRect.y - outline, promptRect.width, promptRect.height), prompt, promptOutlineStyle);
        GUI.Label(new Rect(promptRect.x - outline, promptRect.y + outline, promptRect.width, promptRect.height), prompt, promptOutlineStyle);
        GUI.Label(new Rect(promptRect.x + outline, promptRect.y + outline, promptRect.width, promptRect.height), prompt, promptOutlineStyle);
        GUI.Label(promptRect, prompt, promptStyle);
        GUI.color = previous;
    }

    private static void DrawCoverTexture(Texture texture, Rect destination)
    {
        float sourceAspect = texture.width / (float)texture.height;
        float destinationAspect = destination.width / destination.height;
        Rect uv = new Rect(0f, 0f, 1f, 1f);
        if (sourceAspect > destinationAspect)
        {
            float visible = destinationAspect / sourceAspect;
            uv.x = (1f - visible) * 0.5f;
            uv.width = visible;
        }
        else
        {
            float visible = sourceAspect / destinationAspect;
            uv.y = (1f - visible) * 0.5f;
            uv.height = visible;
        }
        GUI.DrawTextureWithTexCoords(destination, texture, uv, true);
    }
}
