using System.Collections;
using UnityEngine;

public sealed class Stage1Boss : MonoBehaviour
{
    public const int MaxHealth = 1000;

    private static readonly Color BodyColor = Color.white;
    private static readonly Color HitFlashColor = new Color(1f, 0.45f, 0.45f);
    private static readonly Vector2 VisibleHitboxSize = new Vector2(82f / 30f, 90f / 30f);
    private static readonly Vector2 VisibleHitboxOffset = new Vector2(1f / 30f, 0f);
    private const float HitFlashDuration = 0.16f;
    private const float HitFlashInterval = 0.035f;
    private const float HitBurstDuration = 0.18f;
    private const string BossEffectProjectileRoot = "Stage1/BossProjectiles/BossEffects/";
    private const string BossHighlighterGroundImpactPath =
        BossEffectProjectileRoot + "boss_effect_3/ground_impact/boss_highlighter_ground_impact";
    private static readonly string[] BossEffect4SetPaths =
    {
        BossEffectProjectileRoot + "boss_effect_4/falling_book/rotations",
        BossEffectProjectileRoot + "boss_effect_4/other_color/rotations",
        BossEffectProjectileRoot + "boss_effect_4/blue_color/rotations"
    };
    private static readonly string[] BossEffect4BookOutPaths =
    {
        BossEffectProjectileRoot + "boss_effect_4/book_out/falling_book",
        BossEffectProjectileRoot + "boss_effect_4/book_out/other_color",
        BossEffectProjectileRoot + "boss_effect_4/book_out/blue_color"
    };
    private static readonly string[] BossEffect4RotationOrder =
    {
        "north",
        "north-east",
        "east",
        "south-east",
        "south",
        "south-west",
        "west",
        "north-west"
    };
    private const float BossEffectProjectileScale = 0.5f;
    private const float BossEffect3Scale = 3f;
    private const float BossEffect3DefaultSurfaceY = -3.48f;
    private const float BossEffect3SinkDepth = 0.65f;
    private const float BossEffect3FadeSeconds = 1f;
    private const float BossEffect3FrameSeconds = 0.08f;
    private const float BossEffect3HoldSeconds = 0.35f;
    private const float BossEffect3CameraPadding = 0.05f;
    private const float OverloadProjectileSpeedMultiplier = 1.2f;
    private const float BookPatternRecoverySeconds = 1.5f;
    private const float CardAttackVoiceLeadSeconds = 0.22f;
    private const float BookAttackVoiceLeadSeconds = 0.52f;
    private const float DeathFinalHoldSeconds = 0.35f;
    private static readonly Vector2 BossEffect4ColliderMultiplier = new Vector2(0.58f, 0.58f);

    public int CurrentHealth { get; private set; } = MaxHealth;

    // 정화도: 데미지를 줄수록 0%에서 100%로 채워지는 게이지 (체력이 깎이는 게 아니라 정화가 진행되는 컨셉)
    public float PurificationRatio => Mathf.Clamp01((MaxHealth - CurrentHealth) / (float)MaxHealth);
    public int PurificationPercent => Mathf.RoundToInt(PurificationRatio * 100f);
    public int PurificationStageIndex => PurificationRatio >= 0.6f ? 2 : PurificationRatio >= 0.3f ? 1 : 0;
    public Stage1BossState State { get; private set; } = Stage1BossState.Idle;

    [Header("패턴별 도트 아트 (디자이너가 교체 가능)")]
    public Sprite cardHazardSprite;
    public Sprite bookHazardSprite;
    public Sprite laserWarningSprite;
    public Sprite laserBeamSprite;
    public Sprite hitBurstSprite;
    public Sprite[] bossEffect1Frames;
    public Sprite[] bossEffect2Frames;
    [HideInInspector]
    public Sprite[] bossEffect3Frames;
    [HideInInspector]
    public Sprite[] bossHighlighterGroundImpactFrames;
    [HideInInspector]
    public Sprite[] bossEffect4Frames;
    private Sprite[][] bossEffect4FrameSets;
    private Sprite[][] bossEffect4BookOutFrameSets;

    private Stage1Game game;
    private SpriteRenderer bodyRenderer;
    private Stage1BossAnimation bossAnimation;
    private Stage1HitShake hitShake;
    private Coroutine attackRoutine;
    private Coroutine hitFlashRoutine;
    private int phase;
    private bool overloaded;

    public void Initialize(Stage1Game owner)
    {
        game = owner;
        bodyRenderer = GetComponent<SpriteRenderer>();
        bossAnimation = GetComponent<Stage1BossAnimation>();
        hitShake = GetComponent<Stage1HitShake>();
        BoxCollider2D bodyCollider = GetComponent<BoxCollider2D>();
        if (bodyCollider == null) bodyCollider = gameObject.AddComponent<BoxCollider2D>();
        bodyCollider.isTrigger = true;
        FitBodyColliderToSprite(bodyCollider);
        SetBodyColor(BodyColor);
        LoadBossEffectFrames();
    }

    public void BeginAttack(int currentPhase, bool isOverloaded)
    {
        StopAttackRoutine();
        phase = currentPhase;
        overloaded = isOverloaded;
        State = overloaded ? Stage1BossState.Overload : Stage1BossState.Attack;
        SetBodyColor(BodyColor);
        attackRoutine = StartCoroutine(AttackLoop());
    }

    public bool TakeDamage(int damage)
    {
        return TakeDamage(damage, transform.position, false);
    }

    public bool TakeDamage(int damage, Vector2 hitPosition, bool spawnBurst)
    {
        if (State == Stage1BossState.Dead || game.BattleEnded) return false;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        PlayHitFlash();
        if (hitShake != null) hitShake.Play();
        if (spawnBurst) SpawnHitBurst(hitPosition);
        if (CurrentHealth <= 0)
        {
            State = Stage1BossState.Dead;
            StopAttackRoutine();
            if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = null;
            SetBodyColor(BodyColor);
            game.BeginStageClearAnimation();
            GameSfx.Play(GameSfxId.BossDie);
            StartCoroutine(DeathRoutine());
        }
        return true;
    }

    private IEnumerator DeathRoutine()
    {
        if (bossAnimation != null && bossAnimation.HasDeathAnimation)
            yield return bossAnimation.PlayDeath();

        yield return new WaitForSeconds(DeathFinalHoldSeconds);
        if (bossAnimation != null)
            yield return bossAnimation.PlayNpcRecovery();
        game.BeginStageClearSequence();
    }

    public void StopBattle()
    {
        StopAttackRoutine();
        StopAllCoroutines();
        hitFlashRoutine = null;
        if (bossAnimation != null) bossAnimation.CancelDirectAttack();
        SetBodyColor(BodyColor);
    }

    private void SetBodyColor(Color color)
    {
        if (bodyRenderer != null) bodyRenderer.color = color;
    }

    private void PlayHitFlash()
    {
        if (bodyRenderer == null) return;
        if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        float elapsed = 0f;
        bool showHitColor = true;
        while (elapsed < HitFlashDuration)
        {
            SetBodyColor(showHitColor ? HitFlashColor : BodyColor);
            showHitColor = !showHitColor;
            yield return new WaitForSeconds(HitFlashInterval);
            elapsed += HitFlashInterval;
        }

        SetBodyColor(BodyColor);
        hitFlashRoutine = null;
    }

    private void SpawnHitBurst(Vector2 hitPosition)
    {
        if (hitBurstSprite == null) return;
        GameObject burstObject = new GameObject("BossHitBurst");
        burstObject.transform.position = new Vector3(hitPosition.x, hitPosition.y, transform.position.z - 0.01f);
        SpriteRenderer burstRenderer = burstObject.AddComponent<SpriteRenderer>();
        burstRenderer.sprite = hitBurstSprite;
        burstRenderer.sortingOrder = bodyRenderer != null ? bodyRenderer.sortingOrder + 2 : 10;
        burstRenderer.color = Color.white;
        StartCoroutine(HitBurstRoutine(burstObject.transform, burstRenderer));
    }

    private IEnumerator HitBurstRoutine(Transform burstTransform, SpriteRenderer burstRenderer)
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.85f;
        Vector3 endScale = Vector3.one * 1.75f;
        while (elapsed < HitBurstDuration)
        {
            float ratio = elapsed / HitBurstDuration;
            if (burstTransform != null) burstTransform.localScale = Vector3.Lerp(startScale, endScale, ratio);
            if (burstRenderer != null)
            {
                Color color = Color.white;
                color.a = 1f - ratio;
                burstRenderer.color = color;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (burstTransform != null) Destroy(burstTransform.gameObject);
    }

    private void FitBodyColliderToSprite(BoxCollider2D bodyCollider)
    {
        if (bodyRenderer == null || bodyRenderer.sprite == null) return;
        bodyCollider.size = VisibleHitboxSize;
        bodyCollider.offset = VisibleHitboxOffset;
    }

    private void StopAttackRoutine()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    private IEnumerator AttackLoop()
    {
        int pattern = 0;
        while (!game.BattleEnded && (State == Stage1BossState.Attack || State == Stage1BossState.Overload))
        {
            // Once overload begins, falling books join the sequence as an
            // exclusive third pattern. Patterns never run concurrently, so
            // the boss-thrown pattern cannot fire during the falling books.
            int patternCount = overloaded ? 3 : phase == 1 ? 2 : phase == 2 ? 3 : 5;
            switch (pattern % patternCount)
            {
                case 0: yield return CardPattern(); break;
                case 1: yield return LaserPattern(); break;
                case 2: yield return BookPattern(); break;
                case 3: yield return BombPattern(); break;
                default: yield return CaffeinePattern(); break;
            }
            pattern++;
            yield return new WaitForSeconds(overloaded ? 0.45f : 0.75f);
        }
    }

    private IEnumerator CardPattern()
    {
        GameSfx.Play(GameSfxId.BossAttackVoice1);
        yield return new WaitForSeconds(CardAttackVoiceLeadSeconds);

        bool animateAttack = bossAnimation != null && bossAnimation.HasDirectAttackAnimation;
        if (animateAttack)
            yield return bossAnimation.PlayDirectAttackReady();

        try
        {
            GameSfx.Play(GameSfxId.BossPaperAttack);
            for (int wave = 0; wave < 6; wave++)
            {
                SpawnCardWave(wave);
                yield return new WaitForSeconds(overloaded ? 0.28f : 0.5f);
            }

            if (animateAttack)
                yield return bossAnimation.PlayDirectAttackOutro();
        }
        finally
        {
            if (animateAttack)
                bossAnimation.CancelDirectAttack();
        }
    }

    private void SpawnCardWave(int wave)
    {
        if (wave % 2 == 0)
        {
            int count = wave == 4 ? 4 : 5;
            for (int i = 0; i < count; i++)
            {
                float ratio = count <= 1 ? 0.5f : i / (float)(count - 1);
                float angle = Mathf.Lerp(210f, 330f, ratio) * Mathf.Deg2Rad;
                SpawnHazard(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), 3.2f, cardHazardSprite);
            }
            return;
        }

        Vector2 direction = ((Vector2)game.player.transform.position - (Vector2)transform.position).normalized;
        int aimedCount = wave == 5 ? 2 : 1;
        for (int i = 0; i < aimedCount; i++)
        {
            float angleOffset = aimedCount == 1 ? 0f : Mathf.Lerp(-6f, 6f, i) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleOffset);
            float sin = Mathf.Sin(angleOffset);
            Vector2 spreadDirection = new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos);
            SpawnHazard(spreadDirection, 4f, cardHazardSprite);
        }
    }

    private IEnumerator LaserPattern()
    {
        if (game.BattleEnded || State == Stage1BossState.Dead) yield break;

        Vector2 spawnPosition = new Vector2(game.player.transform.position.x, 0f);
        Stage1Projectile laserEffect = game.SpawnHazard(spawnPosition, Vector2.zero, 0f, 4f);
        ConfigureHighlighterPattern(laserEffect);
        yield return new WaitForSeconds(overloaded ? 0.35f : 0.55f);
    }

    public void ConfigureHighlighterPattern(Stage1Projectile projectile)
    {
        if (projectile == null) return;

        Sprite firstEffectSprite = GetBossEffect3FirstSprite();
        float targetX = ClampBossEffect3X(projectile.transform.position.x, firstEffectSprite);
        float topY = GetBossEffect3CeilingY(firstEffectSprite);
        float surfaceY = GetBossEffect3SurfaceY(new Vector2(targetX, topY));
        projectile.transform.position = new Vector3(targetX, topY, projectile.transform.position.z);
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.localScale = Vector3.one * BossEffect3Scale;
        ApplyBossEffect3Sprite(projectile, surfaceY, topY);
    }

    private IEnumerator BookPattern()
    {
        GameSfx.Play(GameSfxId.BossAttackVoice2);
        yield return new WaitForSeconds(BookAttackVoiceLeadSeconds);

        bool animateAttack = bossAnimation != null && bossAnimation.HasBookDropAnimation;
        if (animateAttack)
            yield return bossAnimation.PlayBookDropAttackReady();

        if (game.BattleEnded || State == Stage1BossState.Dead)
        {
            if (animateAttack) bossAnimation.CancelDirectAttack();
            yield break;
        }

        if (animateAttack)
        {
            GameSfx.Play(GameSfxId.BossBookAttack);
            bossAnimation.StartCoroutine(bossAnimation.PlayBookDropAttackOnce());
        }
        else
        {
            GameSfx.Play(GameSfxId.BossBookAttack);
        }

        int count = Random.Range(6, 11);
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(game.ArenaLeft + 0.5f, game.ArenaRight - 0.5f);
            float speed = 3.6f * (overloaded ? OverloadProjectileSpeedMultiplier : 1f);
            Stage1Projectile book = game.SpawnHazard(new Vector2(x, 5f), Vector2.down, speed, 3f);
            book.transform.rotation = Quaternion.identity;
            book.transform.localScale = Vector3.one * BossEffectProjectileScale;
            ApplyBossEffect4Sprite(book);
            yield return new WaitForSeconds(overloaded ? 0.08f : 0.14f);
        }

        // Leave a short recovery window before the next scheduled pattern.
        // Falling books can still remain active, preserving the intended overlap.
        yield return new WaitForSeconds(BookPatternRecoverySeconds);
    }

    private IEnumerator BombPattern()
    {
        Vector2 target = game.player.transform.position;
        Stage1Projectile markerProjectile = Instantiate(game.hazardPrefab, target, Quaternion.identity);
        markerProjectile.enabled = false;
        GameObject marker = markerProjectile.gameObject;
        marker.SetActive(true);
        marker.GetComponent<Collider2D>().enabled = false;
        marker.GetComponent<SpriteRenderer>().color = new Color(1f, 0.55f, 0.1f, 0.55f);
        marker.transform.localScale = Vector3.one * 0.75f;
        float warningDuration = overloaded ? 1.8f : 3f;
        Destroy(marker, warningDuration + 0.5f);
        yield return new WaitForSeconds(warningDuration);
        if (marker != null) Destroy(marker);
        if (game.BattleEnded || State == Stage1BossState.Dead) yield break;

        Stage1Projectile blast = Instantiate(game.hazardPrefab, target, Quaternion.identity);
        blast.transform.localScale = Vector3.one * 2.4f;
        blast.Initialize(game, Vector2.zero, 0f, false, 1, 0.35f);
    }

    private IEnumerator CaffeinePattern()
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            SpawnHazard(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), 4.2f);
        }
        yield return new WaitForSeconds(overloaded ? 0.35f : 0.6f);
    }

    private void SpawnHazard(Vector2 direction, float baseSpeed, Sprite sprite = null)
    {
        float speed = baseSpeed * (overloaded ? OverloadProjectileSpeedMultiplier : 1f);
        Stage1Projectile hazard = game.SpawnHazard(transform.position, direction, speed, 5f);
        hazard.transform.localScale = Vector3.one * BossEffectProjectileScale;
        ApplyBossEffectSprite(hazard, sprite);
    }

    private void ApplyBossEffectSprite(Stage1Projectile projectile, Sprite fallbackSprite)
    {
        if (projectile == null) return;
        Sprite[] selectedFrames = PickBossEffectFrames(projectile.transform.position);
        if (selectedFrames != null && selectedFrames.Length > 0)
        {
            projectile.SetDirectionalSprites(selectedFrames);
            return;
        }

        projectile.SetSprite(fallbackSprite);
    }

    private void ApplyBossEffect4Sprite(Stage1Projectile projectile)
    {
        if (projectile == null) return;
        int frameSetIndex;
        Sprite[] animatedFrames = PickBossEffect4AnimationFrames(out frameSetIndex);
        if (animatedFrames != null && animatedFrames.Length > 0)
        {
            projectile.SetAnimationSprites(animatedFrames, true);
            projectile.SetColliderSizeMultiplier(BossEffect4ColliderMultiplier);
            Sprite[] bookOutFrames = GetBossEffect4BookOutFrames(frameSetIndex);
            if (bookOutFrames != null && bookOutFrames.Length > 0)
                projectile.ConfigureBookImpactEffect(bookOutFrames);
            return;
        }

        if (bossEffect4Frames != null && bossEffect4Frames.Length > 0)
        {
            projectile.SetSprite(bossEffect4Frames[Random.Range(0, bossEffect4Frames.Length)]);
            projectile.SetSpriteFlipX(Random.value < 0.5f);
            projectile.SetColliderSizeMultiplier(BossEffect4ColliderMultiplier);
            return;
        }

        projectile.SetSprite(bookHazardSprite);
    }

    private void ApplyBossEffect3Sprite(Stage1Projectile projectile, float surfaceY, float topY)
    {
        if (projectile == null) return;
        SpriteRenderer playerRenderer = game != null && game.player != null
            ? game.player.GetComponent<SpriteRenderer>()
            : null;
        if (playerRenderer != null)
            projectile.SetSortingOrder(playerRenderer.sortingOrder - 2);

        if (bossEffect3Frames != null && bossEffect3Frames.Length > 0)
        {
            projectile.ConfigureFallingImpactEffect(
                bossEffect3Frames,
                bossHighlighterGroundImpactFrames,
                surfaceY,
                topY,
                BossEffect3SinkDepth,
                BossEffect3FadeSeconds,
                0f,
                BossEffect3FrameSeconds * Mathf.Max(1, bossEffect3Frames.Length - 2),
                BossEffect3HoldSeconds,
                BossEffect3FadeSeconds);
            return;
        }

        projectile.SetSprite(laserBeamSprite);
    }

    private Sprite GetBossEffect3FirstSprite()
    {
        if (bossEffect3Frames == null || bossEffect3Frames.Length == 0) return null;
        return bossEffect3Frames[0];
    }

    private static float ClampBossEffect3X(float targetX, Sprite sprite)
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic)
            return Mathf.Clamp(targetX, -7.75f + 0.45f, 7.75f - 0.45f);

        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        float effectHalfWidth = sprite != null ? sprite.bounds.extents.x * BossEffect3Scale : 0.45f;
        float left = camera.transform.position.x - halfWidth + effectHalfWidth + BossEffect3CameraPadding;
        float right = camera.transform.position.x + halfWidth - effectHalfWidth - BossEffect3CameraPadding;
        if (left > right) return camera.transform.position.x;

        return Mathf.Clamp(targetX, left, right);
    }

    private static float GetBossEffect3CeilingY(Sprite sprite)
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic) return 4.5f;

        float effectHalfHeight = sprite != null ? sprite.bounds.extents.y * BossEffect3Scale : 0f;
        float cameraTop = camera.transform.position.y + camera.orthographicSize;
        return cameraTop - effectHalfHeight - BossEffect3CameraPadding;
    }

    private static float GetBossEffect3SurfaceY(Vector2 spawnPosition)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(spawnPosition, Vector2.down, 20f, LayerMask.GetMask("Default"));
        float bestY = BossEffect3DefaultSurfaceY;
        float bestDistance = float.MaxValue;
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (!IsFloorCollider(hit.collider)) continue;
            if (hit.distance >= bestDistance) continue;

            bestDistance = hit.distance;
            bestY = hit.point.y;
        }

        return bestY;
    }

    private static bool IsFloorCollider(Collider2D collider)
    {
        return collider.gameObject.name == "Floor";
    }

    private Sprite[] PickBossEffectFrames(Vector3 spawnPosition)
    {
        if (bossEffect1Frames == null || bossEffect2Frames == null) return null;

        int timeHash = Mathf.RoundToInt(Time.time * 1000f);
        int positionHash = Mathf.RoundToInt((spawnPosition.x * 92821f) + (spawnPosition.y * 68917f));
        int randomHash = Random.Range(int.MinValue, int.MaxValue);
        int mixed = timeHash ^ positionHash ^ randomHash;
        return (mixed & 1) == 0 ? bossEffect1Frames : bossEffect2Frames;
    }

    private void LoadBossEffectFrames()
    {
        if (bossEffect1Frames == null || bossEffect1Frames.Length == 0)
            bossEffect1Frames = Resources.LoadAll<Sprite>(BossEffectProjectileRoot + "boss_effect_1");
        if (bossEffect2Frames == null || bossEffect2Frames.Length == 0)
            bossEffect2Frames = Resources.LoadAll<Sprite>(BossEffectProjectileRoot + "boss_effect_2");
        if (bossEffect3Frames == null || bossEffect3Frames.Length == 0)
            bossEffect3Frames = Resources.LoadAll<Sprite>(BossEffectProjectileRoot + "boss_effect_3");
        if (bossHighlighterGroundImpactFrames == null || bossHighlighterGroundImpactFrames.Length == 0)
            bossHighlighterGroundImpactFrames = Resources.LoadAll<Sprite>(BossHighlighterGroundImpactPath);
        if (bossEffect4Frames == null || bossEffect4Frames.Length == 0)
            bossEffect4Frames = Resources.LoadAll<Sprite>(BossEffectProjectileRoot + "boss_effect_4");
        if (bossEffect4FrameSets == null || bossEffect4FrameSets.Length == 0)
            bossEffect4FrameSets = LoadBossEffect4FrameSets();
        if (bossEffect4BookOutFrameSets == null || bossEffect4BookOutFrameSets.Length == 0)
            bossEffect4BookOutFrameSets = LoadBossEffect4BookOutFrameSets();

        SortSpritesByName(bossEffect1Frames);
        SortSpritesByName(bossEffect2Frames);
        bossEffect3Frames = FilterBossEffect3Frames(bossEffect3Frames);
        SortSpritesByName(bossEffect3Frames);
        SortSpritesByName(bossHighlighterGroundImpactFrames);
        SortSpritesByName(bossEffect4Frames);
    }

    private Sprite[] PickBossEffect4AnimationFrames(out int frameSetIndex)
    {
        frameSetIndex = -1;
        if (bossEffect4FrameSets == null || bossEffect4FrameSets.Length == 0) return null;

        frameSetIndex = Random.Range(0, bossEffect4FrameSets.Length);
        Sprite[] sourceFrames = bossEffect4FrameSets[frameSetIndex];
        if (sourceFrames == null || sourceFrames.Length == 0) return null;

        int[] rotationSequence = PickBossEffect4RotationSequence();
        Sprite[] animationFrames = new Sprite[rotationSequence.Length];
        int direction = Random.value < 0.5f ? 1 : -1;
        for (int i = 0; i < animationFrames.Length; i++)
        {
            int sequenceIndex = direction > 0 ? i : rotationSequence.Length - 1 - i;
            int index = Mathf.Clamp(rotationSequence[sequenceIndex], 0, sourceFrames.Length - 1);
            animationFrames[i] = sourceFrames[index];
        }

        return animationFrames;
    }

    private Sprite[] GetBossEffect4BookOutFrames(int frameSetIndex)
    {
        if (bossEffect4BookOutFrameSets == null) return null;
        if (frameSetIndex < 0 || frameSetIndex >= bossEffect4BookOutFrameSets.Length) return null;
        return bossEffect4BookOutFrameSets[frameSetIndex];
    }

    private static int[] PickBossEffect4RotationSequence()
    {
        switch (Random.Range(0, 4))
        {
            case 0:
                return new[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            case 1:
                return new[] { 0, 1, 2, 1, 0, 7, 6, 7 };
            case 2:
                return new[] { 2, 1, 0, 7, 6, 5, 4, 3 };
            default:
                return new[] { 4, 3, 2, 3, 4, 5, 6, 5 };
        }
    }

    private static Sprite[][] LoadBossEffect4FrameSets()
    {
        System.Collections.Generic.List<Sprite[]> frameSets = new System.Collections.Generic.List<Sprite[]>();
        foreach (string path in BossEffect4SetPaths)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(path);
            Sprite[] orderedFrames = OrderBossEffect4Frames(frames);
            if (orderedFrames.Length > 0) frameSets.Add(orderedFrames);
        }

        return frameSets.ToArray();
    }

    private static Sprite[][] LoadBossEffect4BookOutFrameSets()
    {
        System.Collections.Generic.List<Sprite[]> frameSets = new System.Collections.Generic.List<Sprite[]>();
        foreach (string path in BossEffect4BookOutPaths)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(path);
            SortSpritesByName(frames);
            frameSets.Add(frames);
        }

        return frameSets.ToArray();
    }

    private static Sprite[] OrderBossEffect4Frames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return new Sprite[0];

        System.Collections.Generic.List<Sprite> orderedFrames = new System.Collections.Generic.List<Sprite>();
        foreach (string directionName in BossEffect4RotationOrder)
        {
            Sprite sprite = FindBossEffect4DirectionSprite(frames, directionName);
            if (sprite != null) orderedFrames.Add(sprite);
        }

        return orderedFrames.ToArray();
    }

    private static Sprite FindBossEffect4DirectionSprite(Sprite[] frames, string directionName)
    {
        foreach (Sprite sprite in frames)
        {
            if (sprite == null) continue;
            if (sprite.name == directionName || sprite.name.StartsWith(directionName + "_"))
                return sprite;
        }

        return null;
    }

    private static Sprite[] FilterBossEffect3Frames(Sprite[] sprites)
    {
        if (sprites == null) return null;

        System.Collections.Generic.List<Sprite> frames = new System.Collections.Generic.List<Sprite>();
        foreach (Sprite sprite in sprites)
        {
            if (sprite == null) continue;
            if (sprite.name.StartsWith("boss_effect_3_1") ||
                IsBossEffect3SecondFrame(sprite.name) ||
                sprite.name.StartsWith("boss_effect_3_3"))
            {
                frames.Add(sprite);
            }
        }
        return frames.ToArray();
    }

    private static bool IsBossEffect3SecondFrame(string spriteName)
    {
        const string prefix = "boss_effect_3_2_";
        if (!spriteName.StartsWith(prefix)) return false;

        string suffix = spriteName.Substring(prefix.Length);
        int index;
        return int.TryParse(suffix, out index) && index >= 0 && index < 5;
    }

    private static void SortSpritesByName(Sprite[] sprites)
    {
        if (sprites == null) return;
        System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
    }
}
