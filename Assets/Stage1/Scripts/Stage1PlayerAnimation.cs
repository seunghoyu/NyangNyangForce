using UnityEngine;

public sealed class Stage1PlayerAnimation : MonoBehaviour
{
    public Sprite[] idleFrames;
    public Sprite[] runFrames;
    public Sprite[] shootIdleFrames;
    public Sprite[] shootRunFrames;
    public Sprite[] jumpFrames;
    public Sprite[] fallFrames;
    public Sprite[] dashFrames;

    [Header("위/아래 조준 사격 (없으면 정면 사격 모션으로 대체)")]
    public Sprite[] shootIdleUpFrames;
    public Sprite[] shootIdleDownFrames;
    public Sprite[] shootRunUpFrames;
    public Sprite[] shootRunDownFrames;
    public Sprite[] shootJumpUpFrames;
    public Sprite[] shootJumpDownFrames;
    public Sprite[] shootJumpSideFrames;

    [Header("앉기 (Idle 상태에서 C키)")]
    public Sprite[] crouchFrames;

    [Header("내려찍기 (공중에서 ↓+Shift)")]
    public Sprite[] slamWindupFrames;
    public Sprite[] slamFallFrames;
    public Sprite[] slamLandFrames;

    public float idleFramesPerSecond = 8f;
    public float runFramesPerSecond = 10f;
    public float shootFramesPerSecond = 12f;
    public float shootRunPoseDuration = 0.18f;
    public float fallFramesPerSecond = 8f;
    public float dashFramesPerSecond = 12f;
    public float slamWindupFramesPerSecond = 24f;
    public float slamFallFramesPerSecond = 12f;
    public float slamLandFramesPerSecond = 20f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Stage1Player player;
    private DashAfterimage afterimage;
    private int idleFrameIndex;
    private int runFrameIndex;
    private int shootFrameIndex;
    private int fallFrameIndex;
    private int dashFrameIndex;
    private int slamWindupFrameIndex;
    private int slamFallFrameIndex;
    private int slamLandFrameIndex;
    private float idleFrameTimer;
    private float runFrameTimer;
    private float shootFrameTimer;
    private float fallFrameTimer;
    private float dashFrameTimer;
    private float slamWindupFrameTimer;
    private float slamFallFrameTimer;
    private float slamLandFrameTimer;
    private float shootRunUntil;
    private float shootJumpUntil;
    private int jumpShotFrameIndex;
    private float jumpShotFrameTimer;
    private bool idleShooting;
    private bool wasRunning;
    private bool wasFalling;
    private bool wasDashing;
    private bool wasSlamFalling;
    private bool wasSlamLanding;

    // PlayShoot()이 조준 방향에 따라 채워주는, 지금 재생 중인 사격 모션 프레임 집합
    private Sprite[] activeIdleShootFrames;
    private Sprite[] activeRunShootFrames;
    private Sprite[] activeJumpShootFrames;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetComponent<Stage1Player>();
        afterimage = GetComponent<DashAfterimage>() ?? gameObject.AddComponent<DashAfterimage>();
    }

    public void ForceIdlePose()
    {
        idleFrameIndex = 0;
        runFrameIndex = 0;
        shootFrameIndex = 0;
        fallFrameIndex = 0;
        dashFrameIndex = 0;
        slamWindupFrameIndex = 0;
        slamFallFrameIndex = 0;
        slamLandFrameIndex = 0;
        idleFrameTimer = 0f;
        runFrameTimer = 0f;
        shootFrameTimer = 0f;
        fallFrameTimer = 0f;
        dashFrameTimer = 0f;
        slamWindupFrameTimer = 0f;
        slamFallFrameTimer = 0f;
        slamLandFrameTimer = 0f;
        shootRunUntil = 0f;
        shootJumpUntil = 0f;
        jumpShotFrameIndex = 0;
        jumpShotFrameTimer = 0f;
        idleShooting = false;
        wasRunning = false;
        wasFalling = false;
        wasDashing = false;
        wasSlamFalling = false;
        wasSlamLanding = false;
        if (afterimage != null)
        {
            afterimage.SetDashing(false);
            afterimage.SetTrailActive(false);
        }
        ShowFrame(idleFrames, 0, null);
    }

    private void Update()
    {
        // 점프 정점 근처에서는 수직 속도가 거의 0이 되므로, 속도 기준이 아니라 실제 접지 여부로
        // 공중 상태를 판정한다 (속도 기준이면 정점에서 한두 프레임 동안 지면 포즈로 잘못 전환되어,
        // 점프 중 사격 시 다른 모션이 순간적으로 끼어드는 것처럼 보였음)
        bool cutsceneWalking = player.IsCutsceneWalking;
        bool airborne = !cutsceneWalking && !player.IsGrounded;
        bool falling = airborne && body.linearVelocity.y < 0f;
        bool running = !player.MovementActionActive &&
                       !airborne &&
                       (cutsceneWalking || Mathf.Abs(body.linearVelocity.x) >= 0.05f);

        if (player.IsDashing)
        {
            if (!wasDashing)
            {
                dashFrameIndex = 0;
                dashFrameTimer = 0f;
                afterimage.SetDashing(true);
            }
            wasDashing = true;
            wasFalling = false;
            idleShooting = false;
            wasRunning = false;
            AdvanceLoop(ref dashFrameIndex, ref dashFrameTimer, dashFramesPerSecond, dashFrames);
            ShowFrame(dashFrames, dashFrameIndex, idleFrames);
            return;
        }

        if (wasDashing)
            afterimage.SetDashing(false);
        wasDashing = false;

        if (player.IsSlamWindup)
        {
            wasFalling = false;
            idleShooting = false;
            wasRunning = false;
            if (wasSlamFalling) afterimage.SetTrailActive(false);
            wasSlamFalling = false;

            if (HasFrames(slamWindupFrames))
            {
                AdvanceLoop(ref slamWindupFrameIndex, ref slamWindupFrameTimer, slamWindupFramesPerSecond, slamWindupFrames);
                ShowFrame(slamWindupFrames, slamWindupFrameIndex, jumpFrames);
            }
            return;
        }

        if (player.IsSlamFalling)
        {
            wasFalling = false;
            idleShooting = false;
            wasRunning = false;
            wasSlamLanding = false;

            if (!wasSlamFalling)
            {
                slamFallFrameIndex = 0;
                slamFallFrameTimer = 0f;
                afterimage.SetTrailActive(true);
            }
            wasSlamFalling = true;

            AdvanceLoop(ref slamFallFrameIndex, ref slamFallFrameTimer, slamFallFramesPerSecond, slamFallFrames);
            ShowFrame(slamFallFrames, slamFallFrameIndex, jumpFrames);
            return;
        }

        if (wasSlamFalling)
            afterimage.SetTrailActive(false);
        wasSlamFalling = false;

        if (player.IsDroppingThroughPlatform && HasFrames(fallFrames))
        {
            idleShooting = false;
            wasRunning = false;
            if (!wasFalling)
            {
                fallFrameIndex = 0;
                fallFrameTimer = 0f;
            }
            wasFalling = true;
            AdvanceClamped(ref fallFrameIndex, ref fallFrameTimer, fallFramesPerSecond, fallFrames.Length);
            ShowFrame(fallFrames, fallFrameIndex, jumpFrames);
            return;
        }

        if (player.IsSlamLanding && HasFrames(slamLandFrames))
        {
            idleShooting = false;
            wasRunning = false;

            if (!wasSlamLanding)
            {
                slamLandFrameIndex = 0;
                slamLandFrameTimer = 0f;
            }
            wasSlamLanding = true;

            AdvanceClamped(ref slamLandFrameIndex, ref slamLandFrameTimer, slamLandFramesPerSecond, slamLandFrames.Length);
            ShowFrame(slamLandFrames, slamLandFrameIndex, idleFrames);
            return;
        }
        wasSlamLanding = false;

        if (airborne)
        {
            idleShooting = false;
            wasRunning = false;

            if (Time.time < shootJumpUntil && HasFrames(activeJumpShootFrames))
            {
                AdvanceClamped(ref jumpShotFrameIndex, ref jumpShotFrameTimer, shootFramesPerSecond, activeJumpShootFrames.Length);
                ShowFrame(activeJumpShootFrames, jumpShotFrameIndex, jumpFrames);
                return;
            }

            if (falling && HasFrames(fallFrames))
            {
                if (!wasFalling) { fallFrameIndex = 0; fallFrameTimer = 0f; }
                wasFalling = true;
                AdvanceClamped(ref fallFrameIndex, ref fallFrameTimer, fallFramesPerSecond, fallFrames.Length);
                ShowFrame(fallFrames, fallFrameIndex, jumpFrames);
            }
            else
            {
                wasFalling = false;
                ShowFrame(jumpFrames, 0, idleFrames);
            }
            return;
        }
        wasFalling = false;

        if (running)
        {
            idleShooting = false;
            if (!wasRunning)
            {
                runFrameIndex = 0;
                runFrameTimer = 0f;
            }

            wasRunning = true;
            AdvanceLoop(ref runFrameIndex, ref runFrameTimer, runFramesPerSecond, runFrames);
            Sprite[] frames = Time.time < shootRunUntil && HasFrames(activeRunShootFrames)
                ? activeRunShootFrames
                : runFrames;
            ShowFrame(frames, runFrameIndex, idleFrames);
            return;
        }

        wasRunning = false;

        if (player.IsCrouching && HasFrames(crouchFrames))
        {
            idleShooting = false;
            ShowFrame(crouchFrames, 0, idleFrames);
            return;
        }

        if (idleShooting)
        {
            if (AdvanceIdleShoot()) return;
            idleShooting = false;
        }

        AdvanceLoop(ref idleFrameIndex, ref idleFrameTimer, idleFramesPerSecond, idleFrames);
        ShowFrame(idleFrames, idleFrameIndex, null);
    }

    // aimVertical: 1 = 위로 조준, -1 = 아래로 조준, 0 = 정면 조준
    public void PlayShoot(int aimVertical = 0)
    {
        bool airborne = !player.IsGrounded;
        if (airborne)
        {
            Sprite[] jumpShotFrames = aimVertical > 0 ? shootJumpUpFrames
                : aimVertical < 0 ? shootJumpDownFrames
                : shootJumpSideFrames;
            if (!HasFrames(jumpShotFrames)) return;
            activeJumpShootFrames = jumpShotFrames;
            shootJumpUntil = Time.time + shootRunPoseDuration;
            jumpShotFrameIndex = 0;
            jumpShotFrameTimer = 0f;
            ShowFrame(jumpShotFrames, 0, jumpFrames);
            return;
        }

        bool running = !player.MovementActionActive && Mathf.Abs(body.linearVelocity.x) >= 0.05f;

        // 정지 상태에서 쏜 직후 곧바로 이동을 시작해도 자세가 끊기지 않도록, 이동 중 사격 포즈를
        // 가만히 서서 쏠 때도 항상 미리 준비해 둔다 (이동 시작 시점에 평범한 정지 프레임이 끼는 것을 방지)
        Sprite[] runShotFrames = SelectVerticalFrames(aimVertical, shootRunUpFrames, shootRunDownFrames);
        if (!HasFrames(runShotFrames)) runShotFrames = shootRunFrames;
        if (HasFrames(runShotFrames))
        {
            activeRunShootFrames = runShotFrames;
            shootRunUntil = Time.time + shootRunPoseDuration;
        }

        if (running && HasFrames(runShotFrames))
        {
            ShowFrame(runShotFrames, runFrameIndex, runFrames);
            return;
        }

        Sprite[] idleShotFrames = aimVertical > 0 && HasFrames(shootIdleUpFrames) ? shootIdleUpFrames
            : aimVertical < 0 && HasFrames(shootIdleDownFrames) ? shootIdleDownFrames
            : shootIdleFrames;
        if (!HasFrames(idleShotFrames)) return;
        activeIdleShootFrames = idleShotFrames;
        idleShooting = true;
        shootFrameIndex = 0;
        shootFrameTimer = 0f;
        ShowFrame(idleShotFrames, 0, idleFrames);
    }

    private static Sprite[] SelectVerticalFrames(int aimVertical, Sprite[] upFrames, Sprite[] downFrames)
    {
        if (aimVertical > 0) return upFrames;
        if (aimVertical < 0) return downFrames;
        return null;
    }

    private bool AdvanceIdleShoot()
    {
        if (!HasFrames(activeIdleShootFrames)) return false;
        shootFrameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(shootFramesPerSecond, 0.01f);
        while (shootFrameTimer >= frameDuration)
        {
            shootFrameTimer -= frameDuration;
            shootFrameIndex++;
            if (shootFrameIndex >= activeIdleShootFrames.Length) return false;
        }

        ShowFrame(activeIdleShootFrames, shootFrameIndex, idleFrames);
        return true;
    }

    private static void AdvanceLoop(ref int index, ref float timer, float framesPerSecond, Sprite[] frames)
    {
        if (!HasFrames(frames)) return;
        timer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(framesPerSecond, 0.01f);
        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            index = (index + 1) % frames.Length;
        }
    }

    // 루프하지 않고 마지막 프레임에서 멈추는 진행 (짧은 사격 모션 등에 사용)
    private static void AdvanceClamped(ref int index, ref float timer, float framesPerSecond, int frameCount)
    {
        timer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(framesPerSecond, 0.01f);
        while (timer >= frameDuration && index < frameCount - 1)
        {
            timer -= frameDuration;
            index++;
        }
    }

    private void ShowFrame(Sprite[] frames, int index, Sprite[] fallback)
    {
        Sprite[] selected = HasFrames(frames) ? frames : fallback;
        if (!HasFrames(selected)) return;
        spriteRenderer.sprite = selected[Mathf.Abs(index) % selected.Length];
    }

    private static bool HasFrames(Sprite[] frames)
    {
        return frames != null && frames.Length > 0;
    }
}
