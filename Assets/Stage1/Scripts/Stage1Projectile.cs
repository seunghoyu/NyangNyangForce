using UnityEngine;

public sealed class Stage1Projectile : MonoBehaviour
{
    private const float DirectionalAnimationFrameSeconds = 0.08f;
    private const float GroundImpactAnimationFrameSeconds = 0.08f;
    private const float FallingImpactBehindPlayerZOffset = 0.2f;
    private const float GroundImpactAheadOfHighlighterZOffset = 0.1f;

    public bool IsEnemyHazard => !playerShot;
    public Sprite[] visualVariants;

    private Stage1Game game;
    private Vector2 direction;
    private float speed;
    private float lifetime;
    private bool playerShot;
    private bool consumed;
    private int damage;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Sprite[] directionalSprites;
    private bool useDirectionForSpriteIndex;
    private bool loopSpriteAnimation = true;
    private int directionalSpriteIndex = -1;
    private int directionalAnimationOffset;
    private float directionalAnimationTimer;
    private Vector2 colliderSizeMultiplier = Vector2.one;
    private Sprite[] bookImpactSprites;
    private bool useBookImpactEffect;
    private bool bookImpactStarted;
    private float bookImpactTimer;
    private int bookImpactSpriteIndex = -1;
    private Sprite[] fallingImpactSprites;
    private bool useFallingImpactEffect;
    private float fallingImpactSurfaceY;
    private float fallingImpactCenterY;
    private float fallingImpactTopFadeInSeconds;
    private float fallingImpactTopHoldSeconds;
    private float fallingImpactTopMorphSeconds;
    private float fallingImpactHoldSeconds;
    private float fallingImpactFadeSeconds;
    private float fallingImpactTimer;
    private int fallingImpactPhase;
    private int fallingImpactImpactIndex;
    private int fallingImpactGroundedStartIndex;
    private bool fallingImpactCanDamage;
    private bool fallingImpactSoundPlayed;
    private Sprite[] fallingGroundImpactSprites;
    private SpriteRenderer fallingGroundImpactRenderer;
    private float fallingGroundImpactTimer;
    private int fallingGroundImpactSpriteIndex = -1;
    private bool fallingGroundImpactPlaying;
    private bool harmless;
    private bool destroyOnSolidImpact;

    public void Initialize(Stage1Game owner, Vector2 moveDirection, float moveSpeed, bool isPlayerShot, int amount, float maxLifetime)
    {
        game = owner;
        direction = moveDirection.normalized;
        if (direction != Vector2.zero) transform.right = direction;
        speed = moveSpeed;
        playerShot = isPlayerShot;
        damage = amount;
        lifetime = maxLifetime;
        gameObject.SetActive(true);
        UpdateDirectionalSprite();
    }

    public void SetSprite(Sprite sprite)
    {
        if (sprite == null) return;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;
        FitColliderToSpritePixels();
    }

    public void SetSortingOrder(int sortingOrder)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingOrder = sortingOrder;
    }

    public void ConfigureCutsceneHazard(bool destroyWhenTouchingSolid = true)
    {
        harmless = true;
        destroyOnSolidImpact = destroyWhenTouchingSolid;
    }

    public void SetVisualVariant(int index)
    {
        if (visualVariants == null || visualVariants.Length == 0) return;
        int wrappedIndex = ((index % visualVariants.Length) + visualVariants.Length) % visualVariants.Length;
        SetSprite(visualVariants[wrappedIndex]);
    }

    public void SetSpriteFlipX(bool flipX)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        spriteRenderer.flipX = flipX;
    }

    public void SetColliderSizeMultiplier(Vector2 multiplier)
    {
        colliderSizeMultiplier = new Vector2(Mathf.Max(0.01f, multiplier.x), Mathf.Max(0.01f, multiplier.y));
        FitColliderToSpritePixels();
    }

    public void ConfigureBookImpactEffect(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return;

        bookImpactSprites = sprites;
        useBookImpactEffect = true;
        bookImpactStarted = false;
        bookImpactTimer = 0f;
        bookImpactSpriteIndex = -1;
    }

    public void SetDirectionalSprites(Sprite[] sprites)
    {
        directionalSprites = sprites;
        useDirectionForSpriteIndex = true;
        loopSpriteAnimation = true;
        directionalSpriteIndex = -1;
        directionalAnimationOffset = 0;
        directionalAnimationTimer = 0f;
        UpdateDirectionalSprite();
    }

    public void SetAnimationSprites(Sprite[] sprites, bool loop = false)
    {
        directionalSprites = sprites;
        useDirectionForSpriteIndex = false;
        loopSpriteAnimation = loop;
        directionalSpriteIndex = -1;
        directionalAnimationOffset = 0;
        directionalAnimationTimer = 0f;
        UpdateDirectionalSprite();
    }

    public void ConfigureFallingImpactEffect(Sprite[] sprites, Sprite[] groundImpactSprites, float surfaceY, float topY, float sinkDepth, float topFadeInSeconds, float topHoldSeconds, float topMorphSeconds, float holdSeconds, float fadeSeconds)
    {
        if (sprites == null || sprites.Length == 0) return;

        fallingImpactSprites = sprites;
        fallingGroundImpactSprites = groundImpactSprites;
        useFallingImpactEffect = true;
        fallingImpactSurfaceY = surfaceY;
        fallingImpactTopFadeInSeconds = topFadeInSeconds;
        fallingImpactTopMorphSeconds = topMorphSeconds;
        fallingImpactHoldSeconds = holdSeconds;
        fallingImpactFadeSeconds = fadeSeconds;
        fallingImpactTimer = 0f;
        fallingImpactPhase = 0;
        fallingImpactCanDamage = false;
        fallingImpactSoundPlayed = false;
        fallingImpactImpactIndex = FindFirstSpriteIndexContaining("_3_3");
        if (fallingImpactImpactIndex < 0) fallingImpactImpactIndex = fallingImpactSprites.Length - 1;
        fallingImpactGroundedStartIndex = FindFirstSpriteIndexContaining("boss_effect_3_2_2");
        if (fallingImpactGroundedStartIndex < 0) fallingImpactGroundedStartIndex = fallingImpactImpactIndex;
        fallingImpactTopHoldSeconds = topHoldSeconds;
        directionalSprites = null;
        direction = Vector2.zero;
        speed = 0f;
        transform.rotation = Quaternion.identity;
        transform.position = new Vector3(
            transform.position.x,
            topY,
            transform.position.z + FallingImpactBehindPlayerZOffset);
        SetSprite(fallingImpactSprites[0]);
        SetRendererAlpha(0f);

        Sprite impactSprite = fallingImpactSprites[fallingImpactImpactIndex];
        fallingImpactCenterY = GetGroundedCenterY(impactSprite);
    }

    private void Update()
    {
        if (game == null || game.BattleEnded)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (useFallingImpactEffect)
        {
            UpdateFallingImpactEffect();
        }
        else if (bookImpactStarted)
        {
            UpdateBookImpactEffect();
        }
        else
        {
            AdvanceDirectionalAnimation();
            UpdateDirectionalSprite();
        }

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f || Mathf.Abs(transform.position.x) > 10f || Mathf.Abs(transform.position.y) > 7f)
            Destroy(gameObject);
    }

    private void UpdateBookImpactEffect()
    {
        if (bookImpactSprites == null || bookImpactSprites.Length == 0) return;

        bookImpactTimer += Time.deltaTime;
        int lastIndex = Mathf.Min(2, bookImpactSprites.Length - 1);
        int index = bookImpactTimer < 0.08f
            ? 0
            : bookImpactTimer < 0.16f ? Mathf.Min(1, lastIndex) : lastIndex;
        SetBookImpactSprite(index);

        if (index == lastIndex)
        {
            float fadeRatio = Mathf.Clamp01((bookImpactTimer - 0.16f) / 0.28f);
            SetRendererAlpha(1f - fadeRatio);
            if (fadeRatio >= 1f) Destroy(gameObject);
        }
    }

    private void StartBookImpactEffect(Collider2D impactCollider = null)
    {
        if (!useBookImpactEffect || bookImpactStarted) return;

        bookImpactStarted = true;
        bookImpactTimer = 0f;
        direction = Vector2.zero;
        speed = 0f;
        damage = 0;
        directionalSprites = null;
        transform.rotation = Quaternion.identity;
        SetRendererAlpha(1f);
        SetBookImpactSprite(0);
        AlignBookImpactToColliderTop(impactCollider);
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null) boxCollider.enabled = false;
    }

    private void AlignBookImpactToColliderTop(Collider2D impactCollider)
    {
        if (impactCollider == null || bookImpactSprites == null || bookImpactSprites.Length == 0) return;

        Sprite sprite = bookImpactSprites[Mathf.Clamp(bookImpactSpriteIndex, 0, bookImpactSprites.Length - 1)];
        float visibleBottomOffset = GetVisibleBottomOffset(sprite);
        float centerY = impactCollider.bounds.max.y - visibleBottomOffset;
        transform.position = new Vector3(transform.position.x, centerY, transform.position.z);
    }

    private void SetBookImpactSprite(int index)
    {
        if (bookImpactSprites == null || bookImpactSprites.Length == 0) return;

        index = Mathf.Clamp(index, 0, bookImpactSprites.Length - 1);
        if (bookImpactSpriteIndex == index) return;

        bookImpactSpriteIndex = index;
        SetSprite(bookImpactSprites[index]);
    }

    private void UpdateFallingImpactEffect()
    {
        if (fallingImpactSprites == null || fallingImpactSprites.Length == 0) return;

        fallingImpactTimer += Time.deltaTime;
        UpdateFallingGroundImpactAnimation();

        if (fallingImpactPhase == 0)
        {
            SetFallingImpactSprite(0);
            float fadeRatio = fallingImpactTopFadeInSeconds <= 0f ? 1f : fallingImpactTimer / fallingImpactTopFadeInSeconds;
            SetRendererAlpha(Mathf.Clamp01(fadeRatio));
            if (fallingImpactTimer >= fallingImpactTopFadeInSeconds)
            {
                fallingImpactPhase = 1;
                fallingImpactTimer = 0f;
                SetRendererAlpha(1f);
            }
            return;
        }

        if (fallingImpactPhase == 1)
        {
            SetFallingImpactSprite(0);
            if (fallingImpactTimer >= fallingImpactTopHoldSeconds)
            {
                fallingImpactPhase = 2;
                fallingImpactTimer = 0f;
                SetFallingImpactSprite(Mathf.Min(1, fallingImpactSprites.Length - 1));
                fallingImpactCanDamage = true;
            }
            return;
        }

        if (fallingImpactPhase == 2)
        {
            int lastTopIndex = Mathf.Max(1, fallingImpactImpactIndex - 1);
            int topFrameCount = lastTopIndex;
            int topOffset = fallingImpactTopMorphSeconds <= 0f
                ? topFrameCount - 1
                : Mathf.FloorToInt(fallingImpactTimer / Mathf.Max(0.001f, fallingImpactTopMorphSeconds / topFrameCount));
            int spriteIndex = Mathf.Clamp(1 + topOffset, 1, lastTopIndex);
            SetFallingImpactSprite(spriteIndex);
            fallingImpactCanDamage = spriteIndex < fallingImpactGroundedStartIndex;
            if (spriteIndex >= fallingImpactGroundedStartIndex)
                SetFallingImpactGroundedPosition(spriteIndex);
            if (fallingImpactTimer >= fallingImpactTopMorphSeconds)
            {
                fallingImpactPhase = 3;
                fallingImpactTimer = 0f;
                transform.position = new Vector3(transform.position.x, fallingImpactCenterY, transform.position.z);
                SetFallingImpactSprite(fallingImpactImpactIndex);
                fallingImpactCanDamage = false;
            }
            return;
        }

        if (fallingImpactPhase == 3)
        {
            if (fallingImpactTimer >= fallingImpactHoldSeconds)
            {
                float fadeRatio = fallingImpactFadeSeconds <= 0f
                    ? 1f
                    : (fallingImpactTimer - fallingImpactHoldSeconds) / fallingImpactFadeSeconds;
                float alpha = 1f - Mathf.Clamp01(fadeRatio);
                SetRendererAlpha(alpha);
                SetFallingGroundImpactAlpha(alpha);
                if (fadeRatio >= 1f) Destroy(gameObject);
            }
            return;
        }
    }

    private void SetFallingImpactSprite(int index)
    {
        index = Mathf.Clamp(index, 0, fallingImpactSprites.Length - 1);
        if (index == directionalSpriteIndex) return;
        directionalSpriteIndex = index;
        SetSprite(fallingImpactSprites[index]);
    }

    private void SetFallingImpactGroundedPosition(int spriteIndex)
    {
        if (fallingImpactSprites == null || fallingImpactSprites.Length == 0) return;

        if (!fallingImpactSoundPlayed)
        {
            fallingImpactSoundPlayed = true;
            GameSfx.Play(GameSfxId.BossHighlighterCrush);
            StartFallingGroundImpactAnimation();
        }

        Sprite sprite = fallingImpactSprites[Mathf.Clamp(spriteIndex, 0, fallingImpactSprites.Length - 1)];
        float centerY = GetGroundedCenterY(sprite);
        transform.position = new Vector3(transform.position.x, centerY, transform.position.z);
    }

    private void StartFallingGroundImpactAnimation()
    {
        if (fallingGroundImpactPlaying || fallingGroundImpactSprites == null || fallingGroundImpactSprites.Length == 0)
            return;

        GameObject visual = new GameObject("HighlighterGroundImpact");
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one;
        fallingGroundImpactRenderer = visual.AddComponent<SpriteRenderer>();

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            fallingGroundImpactRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            // 플레이어 > 충격파 > 형광펜 순서로 보이게 충격파를 본체보다 한 단계 앞에 둔다.
            fallingGroundImpactRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        fallingGroundImpactPlaying = true;
        fallingGroundImpactTimer = 0f;
        fallingGroundImpactSpriteIndex = -1;
        SetFallingGroundImpactSprite(0);
        UpdateFallingGroundImpactPosition();
    }

    private void UpdateFallingGroundImpactAnimation()
    {
        if (!fallingGroundImpactPlaying || fallingGroundImpactRenderer == null) return;

        fallingGroundImpactTimer += Time.deltaTime;
        int index = Mathf.Min(
            Mathf.FloorToInt(fallingGroundImpactTimer / GroundImpactAnimationFrameSeconds),
            fallingGroundImpactSprites.Length - 1);
        SetFallingGroundImpactSprite(index);
        UpdateFallingGroundImpactPosition();
    }

    private void SetFallingGroundImpactSprite(int index)
    {
        if (fallingGroundImpactRenderer == null || fallingGroundImpactSprites == null || fallingGroundImpactSprites.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, fallingGroundImpactSprites.Length - 1);
        if (index == fallingGroundImpactSpriteIndex) return;
        fallingGroundImpactSpriteIndex = index;
        fallingGroundImpactRenderer.sprite = fallingGroundImpactSprites[index];
        fallingGroundImpactRenderer.color = Color.white;
    }

    private void UpdateFallingGroundImpactPosition()
    {
        if (fallingGroundImpactRenderer == null) return;
        Transform visualTransform = fallingGroundImpactRenderer.transform;
        visualTransform.position = new Vector3(
            transform.position.x,
            fallingImpactSurfaceY,
            transform.position.z - GroundImpactAheadOfHighlighterZOffset);
    }

    private void SetFallingGroundImpactAlpha(float alpha)
    {
        if (fallingGroundImpactRenderer == null) return;
        Color color = fallingGroundImpactRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        fallingGroundImpactRenderer.color = color;
    }

    private int FindFirstSpriteIndexContaining(string text)
    {
        if (fallingImpactSprites == null) return -1;
        for (int i = 0; i < fallingImpactSprites.Length; i++)
        {
            if (fallingImpactSprites[i] != null && fallingImpactSprites[i].name.Contains(text)) return i;
        }
        return -1;
    }

    private float GetGroundedCenterY(Sprite sprite)
    {
        float visibleBottomOffset = GetVisibleBottomOffset(sprite);
        return fallingImpactSurfaceY - visibleBottomOffset;
    }

    private float GetVisibleBottomOffset(Sprite sprite)
    {
        if (sprite == null) return 0f;

        Rect rect = sprite.rect;
        Texture2D texture = sprite.texture;
        int minY = Mathf.RoundToInt(rect.yMax);
        bool foundPixel = false;

        try
        {
            for (int y = Mathf.RoundToInt(rect.yMin); y < Mathf.RoundToInt(rect.yMax); y++)
            {
                for (int x = Mathf.RoundToInt(rect.xMin); x < Mathf.RoundToInt(rect.xMax); x++)
                {
                    if (texture.GetPixel(x, y).a <= 0.08f) continue;
                    minY = Mathf.Min(minY, y);
                    foundPixel = true;
                }
            }
        }
        catch (UnityException)
        {
            foundPixel = false;
        }

        if (!foundPixel) return -sprite.bounds.extents.y * Mathf.Abs(transform.lossyScale.y);

        float pixelsPerUnit = sprite.pixelsPerUnit;
        float pivotY = sprite.pivot.y + rect.y;
        return ((minY - pivotY) / pixelsPerUnit) * Mathf.Abs(transform.lossyScale.y);
    }

    private void SetRendererAlpha(float alpha)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }

    private void UpdateDirectionalSprite()
    {
        if (directionalSprites == null || directionalSprites.Length == 0) return;
        if (useDirectionForSpriteIndex && direction == Vector2.zero) return;

        int baseIndex = useDirectionForSpriteIndex ? DirectionToSpriteIndex(direction) : 0;
        int index = (baseIndex + directionalAnimationOffset) % directionalSprites.Length;
        if (index == directionalSpriteIndex) return;

        directionalSpriteIndex = index;
        SetSprite(directionalSprites[index]);
    }

    private void AdvanceDirectionalAnimation()
    {
        if (directionalSprites == null || directionalSprites.Length == 0) return;

        directionalAnimationTimer += Time.deltaTime;
        while (directionalAnimationTimer >= DirectionalAnimationFrameSeconds)
        {
            directionalAnimationTimer -= DirectionalAnimationFrameSeconds;
            if (loopSpriteAnimation)
            {
                directionalAnimationOffset = (directionalAnimationOffset + 1) % directionalSprites.Length;
            }
            else
            {
                directionalAnimationOffset = Mathf.Min(directionalAnimationOffset + 1, directionalSprites.Length - 1);
            }
        }
    }

    private static int DirectionToSpriteIndex(Vector2 moveDirection)
    {
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        return Mathf.RoundToInt(angle / 45f) % 8;
    }

    private void FitColliderToSpritePixels()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null) return;

        Sprite sprite = spriteRenderer.sprite;
        Rect rect = sprite.rect;
        Texture2D texture = sprite.texture;
        int minX = Mathf.RoundToInt(rect.xMax);
        int minY = Mathf.RoundToInt(rect.yMax);
        int maxX = Mathf.RoundToInt(rect.xMin);
        int maxY = Mathf.RoundToInt(rect.yMin);
        bool foundPixel = false;

        try
        {
            for (int y = Mathf.RoundToInt(rect.yMin); y < Mathf.RoundToInt(rect.yMax); y++)
            {
                for (int x = Mathf.RoundToInt(rect.xMin); x < Mathf.RoundToInt(rect.xMax); x++)
                {
                    if (texture.GetPixel(x, y).a <= 0.08f) continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                    foundPixel = true;
                }
            }
        }
        catch (UnityException)
        {
            foundPixel = false;
        }

        if (!foundPixel)
        {
            boxCollider.offset = Vector2.zero;
            boxCollider.size = Vector2.Scale(sprite.bounds.size, colliderSizeMultiplier);
            return;
        }

        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot + rect.position;
        Vector2 min = new Vector2((minX - pivot.x) / pixelsPerUnit, (minY - pivot.y) / pixelsPerUnit);
        Vector2 max = new Vector2((maxX + 1f - pivot.x) / pixelsPerUnit, (maxY + 1f - pivot.y) / pixelsPerUnit);
        Vector2 size = max - min;

        boxCollider.offset = (min + max) * 0.5f;
        boxCollider.size = Vector2.Scale(size * 0.9f, colliderSizeMultiplier);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (harmless)
        {
            if (destroyOnSolidImpact && other != null && !other.isTrigger &&
                other.GetComponentInParent<Stage1Player>() == null)
                Destroy(gameObject);
            return;
        }

        if (playerShot)
        {
            if (consumed) return;
            TutorialScarecrow scarecrow = other.GetComponentInParent<TutorialScarecrow>();
            if (scarecrow != null)
            {
                consumed = true;
                scarecrow.Hit(transform.position);
                Destroy(gameObject);
                return;
            }
            Stage1Boss boss = other.GetComponentInParent<Stage1Boss>();
            if (boss != null)
            {
                consumed = true;
                boss.TakeDamage(damage, transform.position, true);
                Destroy(gameObject);
            }
            return;
        }

        Stage1PlayerHurtbox hurtbox = other.GetComponent<Stage1PlayerHurtbox>();
        if (hurtbox != null)
        {
            if (!useFallingImpactEffect || fallingImpactCanDamage)
            {
                if (!useFallingImpactEffect)
                    PlayerDamageEffect.Spawn(transform.position, hurtbox);
                hurtbox.TakeDamage(damage);
            }

            if (useBookImpactEffect)
            {
                StartBookImpactEffect();
            }
            else if (!useFallingImpactEffect)
            {
                Destroy(gameObject);
            }
            return;
        }

        if (useBookImpactEffect && !other.isTrigger)
        {
            StartBookImpactEffect(other);
        }
    }

    public void DestroyBySlam()
    {
        if (IsEnemyHazard) Destroy(gameObject);
    }
}
