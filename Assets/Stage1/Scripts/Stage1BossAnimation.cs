using System.Collections;
using UnityEngine;

public sealed class Stage1BossAnimation : MonoBehaviour
{
    private const string StoryResourceRoot = "Stage1/Boss/Story/";
    private const string NpcResourcePath = "Stage1/Boss/NPC/boss1_npc";
    private const float StoryTrembleInterval = 0.045f;
    private const float StoryTremblePixelSize = 1f / 30f;
    private const float NpcTrembleDuration = 0.3f;
    private const float NpcTrembleInterval = 0.05f;
    private const float NpcSqueezeDuration = 0.12f;
    private const float NpcPopDuration = 0.22f;
    private const float NpcGroundLocalY = -45f / 30f;

    public Sprite[] standFrames;
    public Sprite[] hardFrames;
    public Sprite[] attackReadyFrames;
    public Sprite[] attackFrames;
    public Sprite[] bookDropReadyFrames;
    public Sprite[] bookDropAttackFrames;
    public Sprite[] deathFrames;
    public Sprite npcFrame;
    public Sprite[] storyIdleFrames;
    public Sprite[] storyOutburstFrames;
    public Sprite[] storyThrowFrames;
    public float framesPerSecond = 4f;
    public float attackReadyFramesPerSecond = 8f;
    public float attackFramesPerSecond = 10f;
    public float deathFramesPerSecond = 8f;
    public float storyIdleFramesPerSecond = 4f;
    public float storyThrowFramesPerSecond = 8f;

    private Stage1Boss boss;
    private SpriteRenderer spriteRenderer;
    private Sprite[] activeBaseFrames;
    private int frameIndex;
    private float frameTimer;
    private bool attackAnimationPlaying;
    private bool attackLoopPlaying;
    private int attackFrameIndex;
    private float attackFrameTimer;
    private bool deathAnimationPlaying;
    private bool storyAnimationActive;
    private bool storyIdleLoopPlaying;
    private int storyFrameIndex;
    private float storyFrameTimer;
    private Vector3 storyBaseLocalPosition;
    private float storyTrembleTimer;
    private int storyTrembleStep;
    private bool storyTremblePlaying;
    private SpriteRenderer npcRenderer;
    private Vector3 baseLocalScale;

    public bool HasDirectAttackAnimation =>
        attackReadyFrames != null && attackReadyFrames.Length > 0 &&
        attackFrames != null && attackFrames.Length > 0;

    public bool HasBookDropAnimation =>
        bookDropReadyFrames != null && bookDropReadyFrames.Length > 0 &&
        bookDropAttackFrames != null && bookDropAttackFrames.Length > 0;

    public bool HasDeathAnimation => deathFrames != null && deathFrames.Length > 0;

    private void Awake()
    {
        boss = GetComponent<Stage1Boss>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        storyBaseLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;
        LoadStoryFrames();
        LoadNpcFrame();
        RefreshBaseAnimation(true);
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        if (storyAnimationActive)
        {
            if (storyIdleLoopPlaying)
                TickStoryIdleLoop();
            if (storyTremblePlaying)
                TickStoryTremble();
            return;
        }

        if (attackAnimationPlaying)
        {
            if (attackLoopPlaying)
                TickAttackLoop();
            return;
        }

        RefreshBaseAnimation(false);
        if (activeBaseFrames == null || activeBaseFrames.Length <= 1) return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(framesPerSecond, 0.01f);
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % activeBaseFrames.Length;
            ShowFrame(activeBaseFrames, frameIndex);
        }
    }

    public IEnumerator PlayDirectAttackReady()
    {
        if (!HasDirectAttackAnimation || spriteRenderer == null)
            yield break;

        attackAnimationPlaying = true;
        attackLoopPlaying = false;
        yield return PlayFramesOnce(attackReadyFrames, attackReadyFramesPerSecond);

        attackFrameIndex = 0;
        attackFrameTimer = 0f;
        attackLoopPlaying = true;
        ShowFrame(attackFrames, attackFrameIndex);
    }

    public IEnumerator PlayBookDropAttackReady()
    {
        if (!HasBookDropAnimation || spriteRenderer == null)
            yield break;

        attackAnimationPlaying = true;
        attackLoopPlaying = false;
        yield return PlayFramesOnce(bookDropReadyFrames, attackReadyFramesPerSecond);
        ShowFrame(bookDropReadyFrames, bookDropReadyFrames.Length - 1);
    }

    public IEnumerator PlayBookDropAttackOnce()
    {
        if (!attackAnimationPlaying || spriteRenderer == null)
            yield break;

        attackLoopPlaying = false;
        yield return PlayFramesOnce(bookDropAttackFrames, attackFramesPerSecond);
        CancelDirectAttack();
    }

    public IEnumerator PlayDirectAttackOutro()
    {
        if (!attackAnimationPlaying || spriteRenderer == null) yield break;

        attackLoopPlaying = false;
        yield return PlayFramesReverse(
            attackFrames,
            attackFramesPerSecond,
            Mathf.Max(0, attackFrameIndex - 1));
        yield return PlayFramesReverse(
            attackReadyFrames,
            attackReadyFramesPerSecond,
            attackReadyFrames.Length - 1);

        CancelDirectAttack();
    }

    public IEnumerator PlayDeath()
    {
        if (!HasDeathAnimation || spriteRenderer == null) yield break;

        StopAllCoroutines();
        deathAnimationPlaying = true;
        attackAnimationPlaying = true;
        attackLoopPlaying = false;
        yield return PlayFramesOnce(deathFrames, deathFramesPerSecond);
        ShowFrame(deathFrames, deathFrames.Length - 1);
    }

    public IEnumerator PlayNpcRecovery()
    {
        if (spriteRenderer == null || npcFrame == null) yield break;

        float elapsed = 0f;
        while (elapsed < NpcTrembleDuration)
        {
            int step = Mathf.FloorToInt(elapsed / NpcTrembleInterval);
            float x = (step & 1) == 0 ? -StoryTremblePixelSize : StoryTremblePixelSize;
            transform.localPosition = storyBaseLocalPosition + new Vector3(x, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = storyBaseLocalPosition;

        Vector3 squeezedScale = new Vector3(
            baseLocalScale.x * 1.1f,
            baseLocalScale.y * 0.82f,
            baseLocalScale.z);
        elapsed = 0f;
        while (elapsed < NpcSqueezeDuration)
        {
            float ratio = Mathf.Clamp01(elapsed / NpcSqueezeDuration);
            transform.localScale = Vector3.Lerp(baseLocalScale, squeezedScale, ratio);
            elapsed += Time.deltaTime;
            yield return null;
        }

        CreateNpcRenderer();
        spriteRenderer.enabled = false;
        transform.localScale = baseLocalScale;

        Vector3 popStartScale = new Vector3(1.15f, 0.75f, 1f);
        Vector3 popOvershootScale = new Vector3(0.95f, 1.08f, 1f);
        npcRenderer.transform.localScale = popStartScale;
        elapsed = 0f;
        while (elapsed < NpcPopDuration)
        {
            float ratio = Mathf.Clamp01(elapsed / NpcPopDuration);
            npcRenderer.transform.localScale = ratio < 0.55f
                ? Vector3.Lerp(popStartScale, popOvershootScale, ratio / 0.55f)
                : Vector3.Lerp(popOvershootScale, Vector3.one, (ratio - 0.55f) / 0.45f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        npcRenderer.transform.localScale = Vector3.one;
    }

    public void BeginStoryIdle()
    {
        if (storyIdleFrames == null || storyIdleFrames.Length == 0) return;

        ResetStoryTremble();
        storyAnimationActive = true;
        storyIdleLoopPlaying = true;
        storyFrameIndex = 0;
        storyFrameTimer = 0f;
        ShowFrame(storyIdleFrames, 0);
    }

    public void ShowStoryOutburst()
    {
        if (storyOutburstFrames == null || storyOutburstFrames.Length == 0) return;

        storyAnimationActive = true;
        storyIdleLoopPlaying = false;
        storyTremblePlaying = true;
        storyTrembleTimer = 0f;
        storyTrembleStep = 0;
        storyFrameIndex = 0;
        storyFrameTimer = 0f;
        ShowFrame(storyOutburstFrames, 0);
    }

    public IEnumerator PlayStoryThrow()
    {
        if (storyThrowFrames == null || storyThrowFrames.Length == 0) yield break;

        ResetStoryTremble();
        storyAnimationActive = true;
        storyIdleLoopPlaying = false;
        yield return PlayFramesOnce(storyThrowFrames, storyThrowFramesPerSecond);
    }

    public void ReturnToStoryIdle()
    {
        BeginStoryIdle();
    }

    public void EndStorySequence()
    {
        ResetStoryTremble();
        storyAnimationActive = false;
        storyIdleLoopPlaying = false;
        storyFrameIndex = 0;
        storyFrameTimer = 0f;
        RefreshBaseAnimation(true);
    }

    public void CancelDirectAttack()
    {
        if (deathAnimationPlaying) return;
        if (!attackAnimationPlaying) return;
        attackAnimationPlaying = false;
        attackLoopPlaying = false;
        attackFrameTimer = 0f;
        RefreshBaseAnimation(true);
    }

    private void TickAttackLoop()
    {
        if (attackFrames == null || attackFrames.Length == 0) return;

        attackFrameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(attackFramesPerSecond, 0.01f);
        while (attackFrameTimer >= frameDuration)
        {
            attackFrameTimer -= frameDuration;
            attackFrameIndex = (attackFrameIndex + 1) % attackFrames.Length;
            ShowFrame(attackFrames, attackFrameIndex);
        }
    }

    private void TickStoryIdleLoop()
    {
        if (storyIdleFrames == null || storyIdleFrames.Length == 0) return;

        storyFrameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(storyIdleFramesPerSecond, 0.01f);
        while (storyFrameTimer >= frameDuration)
        {
            storyFrameTimer -= frameDuration;
            storyFrameIndex = (storyFrameIndex + 1) % storyIdleFrames.Length;
            ShowFrame(storyIdleFrames, storyFrameIndex);
        }
    }

    private void TickStoryTremble()
    {
        storyTrembleTimer += Time.deltaTime;
        while (storyTrembleTimer >= StoryTrembleInterval)
        {
            storyTrembleTimer -= StoryTrembleInterval;
            storyTrembleStep++;
        }

        float x = (storyTrembleStep & 1) == 0
            ? -StoryTremblePixelSize
            : StoryTremblePixelSize;
        // Keep the book/feet baseline locked while the angry pose trembles.
        transform.localPosition = storyBaseLocalPosition + new Vector3(x, 0f, 0f);
    }

    private void ResetStoryTremble()
    {
        storyTremblePlaying = false;
        storyTrembleTimer = 0f;
        storyTrembleStep = 0;
        transform.localPosition = storyBaseLocalPosition;
    }

    private void LoadStoryFrames()
    {
        if (storyIdleFrames == null || storyIdleFrames.Length == 0)
            storyIdleFrames = CreateStoryFrames("boss1_story_1", 4, 90, 105, new Vector2(0.5f, 60f / 105f));
        if (storyOutburstFrames == null || storyOutburstFrames.Length == 0)
            storyOutburstFrames = CreateStoryFrames("boss1_story_2", 1, 90, 100, new Vector2(0.5f, 55f / 100f));
        if (storyThrowFrames == null || storyThrowFrames.Length == 0)
            storyThrowFrames = CreateStoryFrames("boss1_story_3", 5, 90, 145, new Vector2(0.5f, 55f / 145f));
    }

    private void LoadNpcFrame()
    {
        if (npcFrame != null) return;

        Texture2D texture = Resources.Load<Texture2D>(NpcResourcePath);
        if (texture == null) return;
        texture.filterMode = FilterMode.Point;
        npcFrame = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(16f / 30f, 0f),
            30f,
            0u,
            SpriteMeshType.FullRect);
        npcFrame.name = "boss1_npc";
    }

    private void CreateNpcRenderer()
    {
        if (npcRenderer != null) return;

        GameObject npcObject = new GameObject("BossNpcVisual");
        npcObject.transform.SetParent(transform, false);
        npcObject.transform.localPosition = new Vector3(0f, NpcGroundLocalY, 0f);
        npcRenderer = npcObject.AddComponent<SpriteRenderer>();
        npcRenderer.sprite = npcFrame;
        npcRenderer.color = Color.white;
        npcRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        npcRenderer.sortingOrder = spriteRenderer.sortingOrder;
        npcRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
    }

    private static Sprite[] CreateStoryFrames(
        string resourceName,
        int frameCount,
        int frameWidth,
        int frameHeight,
        Vector2 pivot)
    {
        Texture2D texture = Resources.Load<Texture2D>(StoryResourceRoot + resourceName);
        if (texture == null) return null;

        texture.filterMode = FilterMode.Point;
        Sprite[] frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            frames[i] = Sprite.Create(
                texture,
                new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                pivot,
                30f,
                0u,
                SpriteMeshType.FullRect);
            frames[i].name = resourceName + "_" + i;
        }
        return frames;
    }

    private IEnumerator PlayFramesOnce(Sprite[] frames, float animationFramesPerSecond)
    {
        float frameDuration = 1f / Mathf.Max(animationFramesPerSecond, 0.01f);
        for (int i = 0; i < frames.Length; i++)
        {
            ShowFrame(frames, i);
            yield return new WaitForSeconds(frameDuration);
        }
    }

    private IEnumerator PlayFramesReverse(
        Sprite[] frames,
        float animationFramesPerSecond,
        int startIndex)
    {
        if (frames == null || frames.Length == 0) yield break;

        float frameDuration = 1f / Mathf.Max(animationFramesPerSecond, 0.01f);
        for (int i = Mathf.Min(startIndex, frames.Length - 1); i >= 0; i--)
        {
            ShowFrame(frames, i);
            yield return new WaitForSeconds(frameDuration);
        }
    }

    private void RefreshBaseAnimation(bool force)
    {
        bool useHard = boss != null && boss.State == Stage1BossState.Overload &&
                       hardFrames != null && hardFrames.Length > 0;
        Sprite[] desiredFrames = useHard ? hardFrames : standFrames;
        if (!force && ReferenceEquals(activeBaseFrames, desiredFrames)) return;

        activeBaseFrames = desiredFrames;
        frameIndex = 0;
        frameTimer = 0f;
        ShowFrame(activeBaseFrames, frameIndex);
    }

    private void ShowFrame(Sprite[] frames, int index)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;
        spriteRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }
}
