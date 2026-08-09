using System.Collections;
using UnityEngine;

public sealed class PlayerDeathMotion : MonoBehaviour
{
    private const string IntroResourcePath = "UI/Player/GameOver/gameover";
    private const string LoopResourcePath = "UI/Player/GameOver/gameover_roop";
    private const int FrameWidth = 30;
    private const int FrameHeight = 30;
    private const float FramesPerSecond = 10f;

    private SpriteRenderer targetRenderer;
    private Sprite[] introFrames;
    private Sprite[] loopFrames;
    private bool playing;
    private bool looping;
    private int loopFrameIndex;
    private float loopFrameTimer;

    public IEnumerator Play()
    {
        if (playing) yield break;
        playing = true;
        targetRenderer = GetComponent<SpriteRenderer>();
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        Behaviour animation = GetComponent("Stage1PlayerAnimation") as Behaviour;
        if (animation == null) animation = GetComponent("Stage2PlayerAnimation") as Behaviour;
        if (animation != null) animation.enabled = false;
        foreach (Collider2D item in colliders) item.enabled = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        transform.rotation = Quaternion.identity;
        if (targetRenderer != null)
        {
            targetRenderer.enabled = true;
            targetRenderer.color = Color.white;
        }

        introFrames = CreateFrames(Resources.Load<Texture2D>(IntroResourcePath));
        loopFrames = CreateFrames(Resources.Load<Texture2D>(LoopResourcePath));

        // First play gameover 0 -> 4 once.
        for (int i = 0; i < introFrames.Length; i++)
            yield return ShowFrameForRealtime(introFrames[i]);

        // gameover frame 4 and loop frame 0 overlap, so bridge with loop 1 -> 2 -> 3.
        for (int i = 1; i < loopFrames.Length; i++)
            yield return ShowFrameForRealtime(loopFrames[i]);

        // The popup can now open. Update keeps this 0 -> 1 -> 2 -> 3 loop alive at timeScale 0.
        loopFrameIndex = 0;
        loopFrameTimer = 0f;
        looping = loopFrames.Length > 0;
        if (looping && targetRenderer != null) targetRenderer.sprite = loopFrames[0];
    }

    private void Update()
    {
        if (!looping || targetRenderer == null || loopFrames == null || loopFrames.Length == 0) return;
        loopFrameTimer += Time.unscaledDeltaTime;
        float frameDuration = 1f / FramesPerSecond;
        while (loopFrameTimer >= frameDuration)
        {
            loopFrameTimer -= frameDuration;
            loopFrameIndex = (loopFrameIndex + 1) % loopFrames.Length;
            targetRenderer.sprite = loopFrames[loopFrameIndex];
        }
    }

    private IEnumerator ShowFrameForRealtime(Sprite frame)
    {
        if (targetRenderer != null) targetRenderer.sprite = frame;
        yield return new WaitForSecondsRealtime(1f / FramesPerSecond);
    }

    private static Sprite[] CreateFrames(Texture2D sheet)
    {
        if (sheet == null || sheet.width < FrameWidth || sheet.height < FrameHeight)
            return new Sprite[0];
        sheet.filterMode = FilterMode.Point;
        int count = sheet.width / FrameWidth;
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(
                sheet,
                new Rect(i * FrameWidth, 0f, FrameWidth, FrameHeight),
                new Vector2(0.5f, 0.5f),
                30f);
        }
        return frames;
    }
}
