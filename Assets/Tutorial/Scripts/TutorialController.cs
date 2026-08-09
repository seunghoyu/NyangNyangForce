using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TutorialController : MonoBehaviour
{
    private const string MusicPath = "Audio/BGM/Pixel Tutor";
    private AudioSource musicSource;
    private GUIStyle exitStyle;
    private bool leaving;

    private void Awake()
    {
        EnsureAirPlatform();
    }

    private void Start()
    {
        AudioClip clip = Resources.Load<AudioClip>(MusicPath);
        if (clip != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.5f * GameSettingsService.Data.musicVolume;
            musicSource.Play();
        }
    }

    private void Update()
    {
        if (leaving || Keyboard.current == null || !Keyboard.current.enterKey.wasPressedThisFrame) return;
        leaving = true;
        SceneTransition.Load("World Map");
    }

    private void OnGUI()
    {
        GameTypography.ApplyToCurrentSkin();
        if (exitStyle == null)
        {
            exitStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
        float width = Mathf.Min(476f, Screen.width - 24f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 51f, width, 37f);
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(rect, "ENTER : 튜토리얼 종료", exitStyle);
    }

    private static void EnsureAirPlatform()
    {
        if (GameObject.Find("TutorialAirPlatform") != null) return;
        Texture2D texture = Resources.Load<Texture2D>("Tutorial/library_platform_left");
        if (texture == null) return;
        texture.filterMode = FilterMode.Point;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        GameObject platform = new GameObject("TutorialAirPlatform", typeof(SpriteRenderer));
        platform.transform.position = new Vector3(-3.8f, -0.7f, 0.3f);
        SpriteRenderer renderer = platform.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;

        GameObject surface = new GameObject("TopSurfaceCollider", typeof(BoxCollider2D), typeof(PlatformEffector2D));
        surface.transform.SetParent(platform.transform, false);
        surface.transform.position = new Vector3(-3.8f, -0.415f, 0f);
        BoxCollider2D collider = surface.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(3.78f, 0.12f);
        collider.usedByEffector = true;
        surface.GetComponent<PlatformEffector2D>().useOneWay = true;
    }
}
