using UnityEngine;

// 내려찍기(슬램) 착지 충격 이펙트. 정해진 프레임을 한 번 재생한 뒤 스스로 사라진다.
// Stage1/Stage2 공통으로 사용.
public sealed class SlamImpactEffect : MonoBehaviour
{
    private const string CrashDustResourcePath = "Player/FX/crashdust_effect_1";
    private const int ForegroundSortingOrder = 6;
    private const float ForegroundZ = -0.1f;
    private static readonly Rect[] CrashDustFrameRects =
    {
        new Rect(0f, 0f, 46f, 45f),
        new Rect(46f, 0f, 55f, 45f),
        new Rect(101f, 0f, 103f, 45f),
        new Rect(204f, 0f, 103f, 45f),
        new Rect(307f, 0f, 83f, 45f)
    };

    public Sprite[] frames;
    public float framesPerSecond = 20f;

    private SpriteRenderer spriteRenderer;
    private int frameIndex;
    private float frameTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = ForegroundSortingOrder;
        Vector3 position = transform.position;
        transform.position = new Vector3(position.x, position.y, ForegroundZ);
        Sprite[] crashDustFrames = LoadCrashDustFrames();
        if (crashDustFrames != null && crashDustFrames.Length > 0)
            frames = crashDustFrames;
    }

    private void Start()
    {
        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }
        spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(framesPerSecond, 0.01f);
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
            if (frameIndex >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }
        }

        spriteRenderer.sprite = frames[frameIndex];
    }

    private static Sprite[] LoadCrashDustFrames()
    {
        Texture2D texture = Resources.Load<Texture2D>(CrashDustResourcePath);
        if (texture == null) return null;

        texture.filterMode = FilterMode.Point;
        Sprite[] loadedFrames = new Sprite[CrashDustFrameRects.Length];
        Vector2 groundPivot = new Vector2(0.5f, 5f / 45f);
        for (int i = 0; i < loadedFrames.Length; i++)
        {
            loadedFrames[i] = Sprite.Create(
                texture,
                CrashDustFrameRects[i],
                groundPivot,
                30f,
                0u,
                SpriteMeshType.FullRect);
            loadedFrames[i].name = "CrashDust_" + i;
        }
        return loadedFrames;
    }
}
