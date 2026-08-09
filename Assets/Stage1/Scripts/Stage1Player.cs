using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 이동/조준 입력을 Stage1PlayerAnimation보다 먼저 처리해서, 애니메이션이 이번 프레임에 갱신된
// velocity/aimDirection을 곧바로 읽도록 함 (실행 순서가 뒤바뀌면 전환 시 한 프레임짜리 대기 포즈가 끼어 보임)
[DefaultExecutionOrder(-10)]
public sealed class Stage1Player : MonoBehaviour
{
    private const float PistolShotSpeed = 12f;
    private const float MachineGunShotSpeed = 19f;
    private const float MachineGunInterval = 1f / 10f;
    private const float DashDuration = 0.12f;
    private const float DashCooldown = 1f;
    private const float SlamWindup = 0.2f;
    private const float SlamRadius = 1.2f;
    private const float CrouchHurtboxHeightScale = 0.55f;
    private const float SlamLandPoseDuration = 0.25f;
    private const float DamageFlashDuration = 0.15f;
    private const float InvincibilityVisualDuration = 0.85f;
    private const float InvincibilityDuration = DamageFlashDuration + InvincibilityVisualDuration;
    private const float DamageYellowFlashStrength = 0.6f;
    private const float DamageWhiteFlashStrength = 0.8f;
    private const float DamageFlashInterval = 0.04f;
    private const float InvincibilityFlashInterval = 0.07f;
    private const float InvincibilityFlashAlpha = 0.45f;
    private const float SlamImpactVisualLowerOffset = 10f / 30f;
    private const float GroundProbeDistance = 0.1f;
    private const float GroundProbeHorizontalInset = 0.02f;
    private const float MinimumGroundNormalY = 0.5f;
    private const float SlamMaximumFallDuration = 2f;
    private static readonly Color DamageFlashColor = Color.yellow;

    public float moveSpeed = 5f;
    public float jumpSpeed = 9f;
    public GameObject slamImpactEffectPrefab;
    public int CurrentHealth { get; private set; } = 3;
    public int DisplayedHealth { get; private set; } = 3;
    public bool MovementActionActive => movementActionActive;
    public bool IsDashing { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsSlamWindup { get; private set; }
    public bool IsSlamFalling { get; private set; }
    public bool IsSlamLanding { get; private set; }
    public bool IsDroppingThroughPlatform { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsCutsceneWalking => game != null && game.IsCutsceneMovingPlayer;
    public bool HasMachineGun => hasMachineGun;

    private Stage1Game game;
    private Rigidbody2D body;
    private Collider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    private Stage1PlayerAnimation playerAnimation;
    private PlayerAudioFeedback audioFeedback;
    private SpriteColorFlash damageFlash;
    private BoxCollider2D hurtboxCollider;
    private Vector2 standingHurtboxSize;
    private Vector2 standingHurtboxOffset;
    private Vector2 aimDirection = Vector2.right;
    private int jumpsUsed;
    private int machineGunShotVisualIndex = 1;
    private float invincibleUntil;
    private float machineGunTimer;
    private bool hasMachineGun;
    private bool movementInvulnerable;
    private bool movementActionActive;
    private Collider2D dropThroughPlatform;
    private float nextDashTime;
    private float defaultGravityScale = 3f;
    private bool scriptedInputEnabled;
    private float scriptedHorizontal;
    private bool scriptedAimUpHeld;
    private bool scriptedAimDownHeld;
    private bool scriptedJumpHeld;
    private bool scriptedCrouchHeld;
    private bool scriptedAttackHeld;
    private bool scriptedJumpPressed;
    private bool scriptedDashPressed;
    private bool scriptedAimDownPressed;
    private bool scriptedAttackPressed;
    private int scriptedPreviewLayer = -1;

    public void Initialize(Stage1Game owner)
    {
        game = owner;
        body = GetComponent<Rigidbody2D>();
        if (body != null) defaultGravityScale = body.gravityScale;
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerAnimation = GetComponent<Stage1PlayerAnimation>();
        audioFeedback = GetComponent<PlayerAudioFeedback>() ?? gameObject.AddComponent<PlayerAudioFeedback>();
        damageFlash = GetComponent<SpriteColorFlash>() ?? gameObject.AddComponent<SpriteColorFlash>();

        Transform hurtboxTransform = transform.Find("Hurtbox");
        hurtboxCollider = hurtboxTransform != null ? hurtboxTransform.GetComponent<BoxCollider2D>() : null;
        if (hurtboxCollider != null)
        {
            standingHurtboxSize = hurtboxCollider.size;
            standingHurtboxOffset = hurtboxCollider.offset;
        }
    }

    public void EquipMachineGun()
    {
        hasMachineGun = true;
        machineGunTimer = 0f;
        machineGunShotVisualIndex = 1;
        audioFeedback?.PlayItemObtain();
    }

    public void EnableScriptedInput(bool enabled)
    {
        scriptedInputEnabled = enabled;
        if (!enabled) ClearScriptedInput();
    }

    public void SetScriptedHeldInput(float horizontal, bool aimUp, bool aimDown, bool jump, bool crouch, bool attack)
    {
        scriptedHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        scriptedAimUpHeld = aimUp;
        scriptedAimDownHeld = aimDown;
        scriptedJumpHeld = jump;
        scriptedCrouchHeld = crouch;
        scriptedAttackHeld = attack;
    }

    public void PressScriptedJump() => scriptedJumpPressed = true;
    public void PressScriptedDash() => scriptedDashPressed = true;
    public void PressScriptedAimDown() => scriptedAimDownPressed = true;
    public void PressScriptedAttack() => scriptedAttackPressed = true;

    public bool BeginScriptedDropThrough()
    {
        if (!scriptedInputEnabled || movementActionActive) return false;
        Collider2D platform = GetGroundCollider();
        if (platform == null || platform.GetComponent<PlatformEffector2D>() == null) return false;
        body.bodyType = RigidbodyType2D.Dynamic;
        StartCoroutine(DropThroughPlatformRoutine(platform));
        return true;
    }

    public void PrepareScriptedDropThroughPreview()
    {
        if (!scriptedInputEnabled || body == null) return;
        body.linearVelocity = Vector2.zero;
        body.bodyType = RigidbodyType2D.Kinematic;
        IsGrounded = true;
        ForceIdlePose();
    }

    public void SetScriptedPreviewLayer(int layer)
    {
        scriptedPreviewLayer = layer;
        GetComponent<DashAfterimage>()?.SetRenderLayer(layer);
    }

    public void ResetScriptedPreview(Vector3 position)
    {
        StopAllCoroutines();
        if (dropThroughPlatform != null && playerCollider != null)
            Physics2D.IgnoreCollision(playerCollider, dropThroughPlatform, false);
        dropThroughPlatform = null;
        transform.position = position;
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = defaultGravityScale;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        jumpsUsed = 0;
        nextDashTime = 0f;
        aimDirection = Vector2.right;
        hasMachineGun = false;
        ClearScriptedInput();
        ForceIdlePose();
        UpdateCrouchHurtbox();
    }

    private void ClearScriptedInput()
    {
        scriptedHorizontal = 0f;
        scriptedAimUpHeld = false;
        scriptedAimDownHeld = false;
        scriptedJumpHeld = false;
        scriptedCrouchHeld = false;
        scriptedAttackHeld = false;
        scriptedJumpPressed = false;
        scriptedDashPressed = false;
        scriptedAimDownPressed = false;
        scriptedAttackPressed = false;
    }

    public void ForceIdlePose()
    {
        body.linearVelocity = Vector2.zero;
        IsDashing = false;
        IsCrouching = false;
        IsSlamWindup = false;
        IsSlamFalling = false;
        IsSlamLanding = false;
        IsDroppingThroughPlatform = false;
        movementActionActive = false;
        movementInvulnerable = false;
        audioFeedback?.StopAllLoops();
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (damageFlash != null) damageFlash.Clear();
        if (playerAnimation != null) playerAnimation.ForceIdlePose();
    }

    public void SetCutsceneFacing(float direction)
    {
        if (spriteRenderer != null && Mathf.Abs(direction) > 0.01f)
            spriteRenderer.flipX = direction < 0f;
    }

    public IEnumerator PlayCutsceneDash(float direction, float distance)
    {
        if (body == null || Mathf.Abs(direction) < 0.01f) yield break;

        direction = Mathf.Sign(direction);
        SetCutsceneFacing(direction);
        movementActionActive = true;
        movementInvulnerable = true;
        IsDashing = true;
        audioFeedback?.StopAllLoops();
        audioFeedback?.PlayDash();

        float previousGravity = body.gravityScale;
        float duration = DashDuration;
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(direction * Mathf.Abs(distance) / duration, 0f);
        yield return new WaitForSeconds(duration);

        body.linearVelocity = Vector2.zero;
        body.gravityScale = previousGravity;
        movementInvulnerable = false;
        movementActionActive = false;
        IsDashing = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (game == null || game.BattleEnded || Time.timeScale <= 0f || (!scriptedInputEnabled && keyboard == null))
        {
            audioFeedback?.StopAllLoops();
            return;
        }
        if (game.ControlsLocked)
        {
            audioFeedback?.StopAllLoops();
            if (!game.IsCutsceneMovingPlayer)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                IsGrounded = true;
                IsDashing = false;
                IsCrouching = false;
                IsSlamWindup = false;
                IsSlamFalling = false;
                IsSlamLanding = false;
                movementActionActive = false;
            }
            return;
        }

        float horizontal = scriptedInputEnabled ? scriptedHorizontal : 0f;
        if (!scriptedInputEnabled)
        {
            if (GameSettingsService.Held(GameSettingsService.Data.moveLeft)) horizontal -= 1f;
            if (GameSettingsService.Held(GameSettingsService.Data.moveRight)) horizontal += 1f;
        }

        bool downHeld = scriptedInputEnabled ? scriptedAimDownHeld : GameSettingsService.Held(GameSettingsService.Data.aimDown);
        bool spaceHeld = scriptedInputEnabled ? scriptedJumpHeld : GameSettingsService.Held(GameSettingsService.Data.jump);
        bool jumpPressed = scriptedInputEnabled ? scriptedJumpPressed : GameSettingsService.Pressed(GameSettingsService.Data.jump);
        bool dashPressed = scriptedInputEnabled ? scriptedDashPressed : GameSettingsService.Pressed(GameSettingsService.Data.dash);
        bool aimDownPressed = scriptedInputEnabled ? scriptedAimDownPressed : GameSettingsService.Pressed(GameSettingsService.Data.aimDown);
        bool attackPressed = scriptedInputEnabled ? scriptedAttackPressed : GameSettingsService.Pressed(GameSettingsService.Data.attack);
        bool attackHeld = scriptedInputEnabled ? scriptedAttackHeld : GameSettingsService.Held(GameSettingsService.Data.attack);
        scriptedJumpPressed = false;
        scriptedDashPressed = false;
        scriptedAimDownPressed = false;
        scriptedAttackPressed = false;

        Collider2D groundCollider = GetGroundCollider(dropThroughPlatform);
        bool grounded = groundCollider != null;
        IsGrounded = grounded;
        if (grounded && body.linearVelocity.y <= 0.05f) jumpsUsed = 0;

        IsCrouching = grounded && !movementActionActive &&
                       (scriptedInputEnabled ? scriptedCrouchHeld : GameSettingsService.Held(GameSettingsService.Data.crouch));
        UpdateCrouchHurtbox();

        if (!movementActionActive)
        {
            bool slamPressed = !grounded && downHeld && dashPressed;
            bool dropPressed = grounded &&
                               groundCollider.GetComponent<PlatformEffector2D>() != null &&
                               ((downHeld && jumpPressed) || (spaceHeld && aimDownPressed));
            dashPressed = !downHeld && dashPressed;

            if (slamPressed)
            {
                StartCoroutine(SlamRoutine());
            }
            else if (dropPressed)
            {
                StartCoroutine(DropThroughPlatformRoutine(groundCollider));
            }
            else if (dashPressed)
            {
                if (Time.time >= nextDashTime)
                {
                    float facingDirection = spriteRenderer.flipX ? -1f : 1f;
                    nextDashTime = Time.time + DashCooldown;
                    StartCoroutine(DashRoutine(facingDirection));
                }
            }
            else if (IsCrouching)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
            else
            {
                body.linearVelocity = new Vector2(horizontal * moveSpeed, body.linearVelocity.y);
                if (horizontal != 0f) spriteRenderer.flipX = horizontal < 0f;

                if (jumpPressed && jumpsUsed < 2)
                {
                    body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
                    jumpsUsed++;
                    audioFeedback?.PlayJump();
                }
            }
        }

        UpdateAim();
        UpdateWeapon(keyboard, attackPressed, attackHeld);
        audioFeedback?.SetRunning(
            grounded &&
            Mathf.Abs(body.linearVelocity.y) <= 0.05f &&
            horizontal != 0f &&
            !movementActionActive &&
            !IsDashing &&
            !IsCrouching);
    }

    private void UpdateCrouchHurtbox()
    {
        if (hurtboxCollider == null) return;
        if (IsCrouching)
        {
            float crouchHeight = standingHurtboxSize.y * CrouchHurtboxHeightScale;
            float heightDelta = standingHurtboxSize.y - crouchHeight;
            hurtboxCollider.size = new Vector2(standingHurtboxSize.x, crouchHeight);
            hurtboxCollider.offset = new Vector2(standingHurtboxOffset.x, standingHurtboxOffset.y - heightDelta * 0.5f);
        }
        else
        {
            hurtboxCollider.size = standingHurtboxSize;
            hurtboxCollider.offset = standingHurtboxOffset;
        }
    }

    private void UpdateAim()
    {
        Vector2 inputAim = Vector2.zero;
        if (scriptedInputEnabled)
        {
            inputAim.x = scriptedHorizontal;
            if (scriptedAimUpHeld) inputAim.y += 1f;
            if (scriptedAimDownHeld) inputAim.y -= 1f;
        }
        else
        {
            if (GameSettingsService.Held(GameSettingsService.Data.moveLeft)) inputAim.x -= 1f;
            if (GameSettingsService.Held(GameSettingsService.Data.moveRight)) inputAim.x += 1f;
            if (GameSettingsService.Held(GameSettingsService.Data.aimUp)) inputAim.y += 1f;
            if (GameSettingsService.Held(GameSettingsService.Data.aimDown)) inputAim.y -= 1f;
        }
        if (inputAim == Vector2.zero) return;

        // 위/아래 입력이 있으면 좌우 이동 중에도 위/아래로 조준이 우선됨 (이동하면서 위/아래로 사격 가능)
        aimDirection = inputAim.y != 0f
            ? new Vector2(0f, Mathf.Sign(inputAim.y))
            : new Vector2(Mathf.Sign(inputAim.x), 0f);
    }

    private void UpdateWeapon(Keyboard keyboard, bool attackPressed, bool attackHeld)
    {
        if (IsCrouching)
        {
            audioFeedback?.SetGatlingFiring(false);
            return;
        }

        Vector2 spawnPosition = (Vector2)transform.position + aimDirection * 0.6f;
        if (!hasMachineGun)
        {
            audioFeedback?.SetGatlingFiring(false);
            bool configuredAttackPressed = attackPressed;
            bool alternateAttackPressed = !scriptedInputEnabled && GameSettingsService.Data.attack != (int)Key.X && keyboard != null &&
                                          keyboard.xKey.wasPressedThisFrame;
            if (configuredAttackPressed || alternateAttackPressed)
            {
                Stage1Projectile shot = game.SpawnBasicPlayerShot(spawnPosition, aimDirection, PistolShotSpeed);
                ConfigureScriptedPreviewSpawn(shot != null ? shot.gameObject : null, 99);
                playerAnimation.PlayShoot(AimVerticalSign);
                audioFeedback?.PlayShot();
            }
            return;
        }

        bool fireHeld = attackHeld || (!scriptedInputEnabled && keyboard != null && keyboard.xKey.isPressed);
        audioFeedback?.SetGatlingFiring(fireHeld);
        if (!fireHeld)
        {
            machineGunTimer = 0f;
            return;
        }

        machineGunTimer -= Time.deltaTime;
        if (machineGunTimer > 0f) return;

        Stage1Projectile machineGunShot = game.SpawnMachineGunShot(spawnPosition, aimDirection, MachineGunShotSpeed, machineGunShotVisualIndex);
        ConfigureScriptedPreviewSpawn(machineGunShot != null ? machineGunShot.gameObject : null, 99);
        machineGunShotVisualIndex = 1 - machineGunShotVisualIndex;
        playerAnimation.PlayShoot(AimVerticalSign);
        machineGunTimer = MachineGunInterval;
    }

    // 위/아래 조준 중인지: 1=위로 조준, -1=아래로 조준, 0=정면 조준
    private int AimVerticalSign => aimDirection.y > 0.01f ? 1 : aimDirection.y < -0.01f ? -1 : 0;

    private IEnumerator DashRoutine(float direction)
    {
        movementActionActive = true;
        movementInvulnerable = true;
        IsDashing = true;
        audioFeedback?.SetRunning(false);
        audioFeedback?.SetGatlingFiring(false);
        audioFeedback?.PlayDash();
        float dashDistance = playerCollider.bounds.size.x * 2f;
        float dashSpeed = dashDistance / DashDuration;
        float previousGravity = body.gravityScale;
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(direction * dashSpeed, 0f);

        yield return new WaitForSeconds(DashDuration);

        body.linearVelocity = Vector2.zero;
        body.gravityScale = previousGravity;
        movementInvulnerable = false;
        movementActionActive = false;
        IsDashing = false;
    }

    private IEnumerator DropThroughPlatformRoutine(Collider2D platform)
    {
        movementActionActive = true;
        IsDroppingThroughPlatform = true;
        dropThroughPlatform = platform;
        Physics2D.IgnoreCollision(playerCollider, platform, true);
        body.linearVelocity = new Vector2(body.linearVelocity.x, -moveSpeed);

        while (!game.BattleEnded && playerCollider.bounds.max.y >= platform.bounds.min.y)
            yield return new WaitForFixedUpdate();

        if (platform != null)
            Physics2D.IgnoreCollision(playerCollider, platform, false);
        dropThroughPlatform = null;
        movementActionActive = false;

        // 플랫폼을 완전히 벗어난 뒤 실제 바닥에 닿을 때까지 낙하 상태를 유지한다.
        // 접지 판정이 갱신되기 전 한 프레임이 점프 포즈로 보이는 현상을 막는다.
        while (!game.BattleEnded && GetGroundCollider() == null)
            yield return new WaitForFixedUpdate();
        IsDroppingThroughPlatform = false;
    }

    private IEnumerator SlamRoutine()
    {
        movementActionActive = true;
        movementInvulnerable = true;
        IsSlamWindup = true;
        float previousGravity = body.gravityScale;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(SlamWindup);
        IsSlamWindup = false;

        float slamSpeed = playerCollider.bounds.size.x * 3f / DashDuration;
        IsSlamFalling = true;
        body.linearVelocity = new Vector2(0f, -slamSpeed);
        yield return new WaitForFixedUpdate();

        float slamFallDeadline = Time.time + SlamMaximumFallDuration;
        Collider2D landedCollider = GetGroundCollider();
        while (!game.BattleEnded && landedCollider == null && Time.time < slamFallDeadline)
        {
            body.linearVelocity = new Vector2(0f, -slamSpeed);
            yield return new WaitForFixedUpdate();
            landedCollider = GetGroundCollider();
        }
        IsSlamFalling = false;

        body.linearVelocity = Vector2.zero;
        body.gravityScale = previousGravity;
        if (landedCollider != null)
        {
            ApplySlamImpact();
            StartCoroutine(SlamLandPoseRoutine());
        }

        movementInvulnerable = false;
        movementActionActive = false;
    }

    private IEnumerator SlamLandPoseRoutine()
    {
        IsSlamLanding = true;
        yield return new WaitForSeconds(SlamLandPoseDuration);
        IsSlamLanding = false;
    }

    private void ApplySlamImpact()
    {
        Collider2D groundCollider = GetGroundCollider();
        float impactY = groundCollider != null
            ? groundCollider.bounds.max.y
            : playerCollider.bounds.min.y;
        Vector2 center = new Vector2(transform.position.x, impactY);
        audioFeedback?.PlayJumpCrash();
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, SlamRadius);
        Stage1Boss hitBoss = null;

        foreach (Collider2D hit in hits)
        {
            Stage1Boss boss = hit.GetComponentInParent<Stage1Boss>();
            if (boss != null && boss != hitBoss)
            {
                hitBoss = boss;
                boss.TakeDamage(10);
            }

            Stage1Projectile projectile = hit.GetComponent<Stage1Projectile>();
            if (projectile != null) projectile.DestroyBySlam();
        }

        if (slamImpactEffectPrefab != null)
        {
            Vector2 visualPosition = center + Vector2.down * SlamImpactVisualLowerOffset;
            GameObject impact = Instantiate(slamImpactEffectPrefab, visualPosition, Quaternion.identity);
            if (scriptedPreviewLayer >= 0) SetLayerRecursively(impact, scriptedPreviewLayer);
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void ConfigureScriptedPreviewSpawn(GameObject spawnedObject, int sortingOrder)
    {
        if (scriptedPreviewLayer < 0 || spawnedObject == null) return;
        spawnedObject.transform.SetParent(transform.parent, true);
        SetLayerRecursively(spawnedObject, scriptedPreviewLayer);
        foreach (SpriteRenderer renderer in spawnedObject.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.sortingOrder = sortingOrder;
    }

    private Collider2D GetGroundCollider(Collider2D ignoredCollider = null)
    {
        Bounds playerBounds = playerCollider.bounds;
        float horizontalOffset = Mathf.Max(0f, playerBounds.extents.x - GroundProbeHorizontalInset);
        float distance = playerBounds.extents.y + GroundProbeDistance;

        Collider2D ground = RaycastGroundAt(playerBounds.center.x, playerBounds.center.y, distance, ignoredCollider);
        if (ground != null) return ground;

        ground = RaycastGroundAt(playerBounds.center.x - horizontalOffset, playerBounds.center.y, distance, ignoredCollider);
        if (ground != null) return ground;

        return RaycastGroundAt(playerBounds.center.x + horizontalOffset, playerBounds.center.y, distance, ignoredCollider);
    }

    private Collider2D RaycastGroundAt(float x, float y, float distance, Collider2D ignoredCollider)
    {
        int groundMask = LayerMask.GetMask("Default");
        if (scriptedPreviewLayer >= 0)
            groundMask |= 1 << scriptedPreviewLayer;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            new Vector2(x, y),
            Vector2.down,
            distance,
            groundMask);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null &&
                hit.collider != playerCollider &&
                hit.collider != ignoredCollider &&
                !hit.collider.isTrigger &&
                hit.normal.y >= MinimumGroundNormalY)
                return hit.collider;
        }
        return null;
    }

    private void FixedUpdate()
    {
        if (game == null) return;
        if (game.IsCutsceneMovingPlayer) return;
        Vector2 position = body.position;
        position.x = Mathf.Clamp(position.x, game.ArenaLeft, game.ArenaRight);
        body.position = position;
    }

    public void TakeDamage(int damage)
    {
        if (game == null || game.BattleEnded || movementInvulnerable || Time.time < invincibleUntil) return;
        int healthBeforeDamage = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, damage));
        audioFeedback?.PlayDamage(CurrentHealth <= 0);
        invincibleUntil = Time.time + InvincibilityDuration;
        StartCoroutine(FlashInvincibility(healthBeforeDamage));
    }

    private IEnumerator FlashInvincibility(int healthBeforeDamage)
    {
        float flashUntil = Time.time + DamageFlashDuration;
        bool showDamagedHeart = false;
        while (Time.time < flashUntil)
        {
            showDamagedHeart = !showDamagedHeart;
            if (damageFlash != null)
            {
                Color flashColor = showDamagedHeart ? DamageFlashColor : Color.white;
                float flashStrength = showDamagedHeart
                    ? DamageYellowFlashStrength
                    : DamageWhiteFlashStrength;
                damageFlash.Show(flashColor, flashStrength);
            }
            DisplayedHealth = showDamagedHeart ? healthBeforeDamage : CurrentHealth;
            float remainingFlashTime = flashUntil - Time.time;
            yield return new WaitForSeconds(Mathf.Min(DamageFlashInterval, remainingFlashTime));
        }
        spriteRenderer.enabled = true;
        if (damageFlash != null) damageFlash.Clear();
        DisplayedHealth = CurrentHealth;

        bool showFullOpacity = true;
        while (Time.time < invincibleUntil)
        {
            showFullOpacity = !showFullOpacity;
            Color invincibilityColor = spriteRenderer.color;
            invincibilityColor.a = showFullOpacity ? 1f : InvincibilityFlashAlpha;
            spriteRenderer.color = invincibilityColor;
            float remainingInvincibilityTime = invincibleUntil - Time.time;
            yield return new WaitForSeconds(Mathf.Min(InvincibilityFlashInterval, remainingInvincibilityTime));
        }

        if (damageFlash != null) damageFlash.Clear();
        if (CurrentHealth <= 0) game.GameOver();
    }
}
