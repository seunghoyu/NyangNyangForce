using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum Stage1BossState
{
    Idle,
    Attack,
    Overload,
    Dead
}

public sealed class Stage1Game : MonoBehaviour
{
    private const float PickupDropRayStartY = 4f;
    private const float PickupDropRayDistance = 10f;
    private const float PickupSurfaceFallbackY = -1.08f;

    public Stage1Player player;
    public Stage1Boss boss;
    public Stage1Projectile basicPlayerShotPrefab;
    public Stage1Projectile machineGunShotPrefab;
    public Stage1Projectile hazardPrefab;
    public Stage1MachineGunPickup machineGunPickupPrefab;
    public AudioClip introMusic;
    public AudioClip battleMusic;

    // 튜토리얼 등 자유 연습용 씬에서 사용: 보스 공격 루틴/승패 판정을 멈추고 보스를 가만히 있는 연습용 더미로 둠
    public bool freeRoamMode = false;

    public bool BattleEnded { get; private set; }
    public bool ControlsLocked { get; private set; }
    public bool IsCutsceneMovingPlayer { get; private set; }
    public float ArenaLeft => -7.75f;
    public float ArenaRight => 7.75f;

    private static readonly Stage1DialogueLine[] IntroQuestionDialogue =
    {
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "(우리 학교에 저렇게 큰 햄스터가 있었나...?)"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "(엄청 크네...!)"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "저기요, 혹시 제 가방 보셨어요?"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "..."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "못 보...셨어요?"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "........"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "저기요!"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "................")
    };

    private static readonly Stage1DialogueLine[] IntroOutburstDialogue =
    {
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "조용히 햄!!!!!")
    };

    private static readonly Stage1DialogueLine[] IntroClosingDialogue =
    {
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "가방 위치를 물어봤는데 형광펜 위치만 잔뜩 알려주네…"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "조용히 하랬다 햄!!"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "일단 진정부터 시켜야겠다."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "말로는 안 될 것 같으니까… 아주 확실하게.")
    };

    private static readonly Stage1DialogueLine[] EpilogueOpeningDialogue =
    {
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "고맙다 햄… 덕분에 정신이 돌아왔다 햄."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "돌아와서 다행이긴 한데요."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "형광펜으로 사람 잡을 뻔한 건 알고 있죠?"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "공부하고 있었는데 갑자기 벼락 같은 게 찌릿! 하고 떨어졌다 햄."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "그 뒤로는 아무것도 기억나지 않는다 햄…"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "혹시 갈색 가방은 못 보셨어요?"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "꼭 찾아야 하는 게 들어 있어요. 진짜로 중요한 거예요."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "가방은 못 봤다 햄."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "방금 실습수업이었으니까… 컴퓨터실에 가봐야겠다.")
    };

    private static readonly Stage1DialogueLine[] EpilogueRewardDialogue =
    {
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "잠깐! 그냥 가면 내가 은혜도 모르는 햄스터가 된다 햄!"),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "내 비상식량을 나눠주겠다 햄."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "무려 한 알이다 햄. 소중히 다뤄라 햄.")
    };

    private static readonly Stage1DialogueLine[] EpilogueClosingDialogue =
    {
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "고마워요. 생각보다 엄청 진지하게 주시네요."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Boss, "내일 먹을 거였다 햄."),
        new Stage1DialogueLine(Stage1DialogueSpeaker.Player, "…더 소중해졌네요.")
    };

    private const float IntroCharacterInterval = 0.045f;
    private const float IntroLineHoldSeconds = 1.4f;
    private const float StageFadeInSeconds = 1f;
    private const string IntroMusicResourcePath = "Audio/Stage1/Hamster Ledger Chase";
    private const string BattleMusicResourcePath = "Audio/Stage1/Whimsical Library Battle (Fade In)";
    private const float MusicVolume = 0.55f;
    private const float IntroMusicFadeInSeconds = 1.2f;
    private const float MusicTransitionFadeOutSeconds = 1.0f;
    private const float BattleMusicFadeInSeconds = 1.4f;
    private const float PlayerEntranceStartX = -10.25f;
    private const float PlayerEntranceRunSpeed = 2.2f;
    private const float PlayerEntrancePauseSeconds = 1f;
    private const float CutsceneWalkSpeed = 3.4f;
    private const float IntroHighlighterSequenceInterval = 0.12f;
    private const float EpilogueHamsterHalfWidth = 0.4f;
    private const float EpilogueBossGap = 0.35f;
    private const float EpilogueExitWalkDistance = 0.45f;
    private const float RewardPopupAutoCloseSeconds = 3f;
    private const string RewardIconResourcePath = "Stage1/Rewards/stage1_sunflower_seed";
    private const string PlayerMugshotResourcePath = "Stage1/UI/Dialogue/PlayerMugshot/player_mugshot1";
    private const string PlayerMugshotBoxResourcePath = "Stage1/UI/Dialogue/PlayerMugshot/player_mugshotbox";
    private const string BossNpcMugshotResourcePath = "Stage1/UI/Dialogue/BossNpcMugshot/boss1npc_mugshot";
    private const string BossNpcMugshotBoxResourcePath = "Stage1/UI/Dialogue/BossNpcMugshot/boss1npc_mugshotbox";
    private const string PlayerMugshotSecondExpressionLine = "저기요!";
    private const string PlayerMugshotThirdExpressionLine = "가방 위치를 물어봤는데 형광펜 위치만 잔뜩 알려주네…";
    private const float PixelPerfectReferenceWidth = 480f;
    private const float PixelPerfectReferenceHeight = 270f;
    private const float PlayerMugshotBubbleGapPixels = 12f;
    private const float PlayerMugshotSizeRatio = 0.9f;
    private const float OverloadHealthRatio = 0.4f;
    private const float OverloadDurationSeconds = 10f;
    private const float OverloadShakeDuration = 1.15f;
    private const float OverloadShakeMagnitude = 0.09f;
    private const float OverloadShakeFrequency = 24f;

    private string resultTitle;
    private string notice;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle noticeStyle;
    private GUIStyle hudLabelStyle;
    private GUIStyle bossStageLabelStyle;
    private GUIStyle dialogueStyle;
    private GUIStyle dialogueShadowStyle;
    private GUIStyle dialogueHighlightStyle;
    private GUIStyle dialogueHintStyle;
    private GUIStyle dialogueHintShadowStyle;
    private GUIStyle rewardNameStyle;
    private AudioSource musicSource;
    private GameMenuController gameMenu;
    private DialogueVoiceController dialogueVoice;
    private Coroutine musicFadeRoutine;
    private readonly Stage1DialogueSequence dialogue = new Stage1DialogueSequence();
    private bool introCutsceneActive;
    private bool stageClearCutsceneActive;
    private bool stageClearSequenceStarted;
    private bool rewardPopupActive;
    private float rewardPopupClosesAt;
    private Texture2D rewardIcon;
    private Texture2D playerMugshotTexture;
    private Texture2D playerMugshotBoxTexture;
    private Texture2D bossNpcMugshotTexture;
    private Texture2D bossNpcMugshotBoxTexture;
    private bool battleMusicTransitionStarted;
    private bool bossHealthBarVisible;
    private float stageFadeAlpha;

    private void Start()
    {
        Time.timeScale = 1f;
        if (!freeRoamMode)
        {
            gameMenu = gameObject.GetComponent<GameMenuController>();
            if (gameMenu == null) gameMenu = gameObject.AddComponent<GameMenuController>();
        }
        dialogueVoice = gameObject.GetComponent<DialogueVoiceController>();
        if (dialogueVoice == null) dialogueVoice = gameObject.AddComponent<DialogueVoiceController>();
        dialogue.CharacterRevealed += HandleDialogueCharacterRevealed;
        gameMenu?.Initialize("Stage 1");
        player.Initialize(this);
        if (boss != null) boss.Initialize(this);
        bossHealthBarVisible = freeRoamMode && boss != null;
        LoadStageMusic();
        if (!freeRoamMode)
        {
            StartCoroutine(StageIntroRoutine());
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard.spaceKey.wasPressedThisFrame) return;

        if (dialogue.IsActive)
        {
            dialogue.Advance(Time.time);
            return;
        }

        if (rewardPopupActive)
            rewardPopupActive = false;
    }

    private IEnumerator StageIntroRoutine()
    {
        introCutsceneActive = true;
        ControlsLocked = true;
        SpriteRenderer entranceRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
        if (entranceRenderer != null) entranceRenderer.enabled = false;
        yield return StageFadeInRoutine();

        PlayMusic(introMusic);
        Stage1BossAnimation storyAnimation = boss != null
            ? boss.GetComponent<Stage1BossAnimation>()
            : null;
        storyAnimation?.BeginStoryIdle();
        yield return PlayerEntranceRoutine();

        yield return RunDialogue(IntroQuestionDialogue);
        storyAnimation?.ShowStoryOutburst();
        yield return RunDialogue(IntroOutburstDialogue);
        if (storyAnimation != null)
            yield return storyAnimation.PlayStoryThrow();
        storyAnimation?.EndStorySequence();
        yield return PrologueHighlighterRainRoutine();
        yield return RunDialogue(IntroClosingDialogue);

        bossHealthBarVisible = !BattleEnded;
        introCutsceneActive = false;
        ControlsLocked = false;
        StartBattleMusicTransition();

        if (!BattleEnded)
        {
            StartCoroutine(BattleRoutine());
            StartCoroutine(SpawnMachineGunRoutine());
        }
    }

    private IEnumerator StageFadeInRoutine()
    {
        stageFadeAlpha = 1f;
        yield return null;

        float elapsed = 0f;
        while (elapsed < StageFadeInSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            stageFadeAlpha = 1f - Mathf.Clamp01(elapsed / StageFadeInSeconds);
            yield return null;
        }

        stageFadeAlpha = 0f;
    }

    private IEnumerator PlayerEntranceRoutine()
    {
        if (player == null) yield break;

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        Vector3 targetPosition = player.transform.position;
        Vector3 startPosition = new Vector3(PlayerEntranceStartX, targetPosition.y, targetPosition.z);
        if (playerRenderer != null) playerRenderer.flipX = false;

        RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;
        float originalGravityScale = 0f;
        if (playerBody != null)
        {
            originalBodyType = playerBody.bodyType;
            originalGravityScale = playerBody.gravityScale;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            playerBody.bodyType = RigidbodyType2D.Kinematic;
            playerBody.gravityScale = 0f;
            playerBody.position = startPosition;
        }
        else
        {
            player.transform.position = startPosition;
        }

        IsCutsceneMovingPlayer = true;
        if (playerRenderer != null) playerRenderer.enabled = true;
        float entranceDeadline = Time.realtimeSinceStartup + 5f;
        WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
        while (!BattleEnded && player.transform.position.x < targetPosition.x - 0.02f)
        {
            if (Time.realtimeSinceStartup >= entranceDeadline)
            {
                Debug.LogWarning("Stage 1 player entrance exceeded five seconds. Snapping to the dialogue position.");
                break;
            }

            float step = PlayerEntranceRunSpeed * Time.fixedDeltaTime;
            float nextX = Mathf.Min(player.transform.position.x + step, targetPosition.x);
            if (playerBody != null)
                playerBody.MovePosition(new Vector2(nextX, targetPosition.y));
            else
                player.transform.position = new Vector3(nextX, targetPosition.y, targetPosition.z);
            yield return waitForFixedUpdate;
        }

        if (playerBody != null)
        {
            playerBody.position = targetPosition;
            playerBody.bodyType = originalBodyType;
            playerBody.gravityScale = originalGravityScale;
            playerBody.linearVelocity = Vector2.zero;
        }
        else
        {
            player.transform.position = targetPosition;
        }
        IsCutsceneMovingPlayer = false;
        player.ForceIdlePose();

        yield return new WaitForSeconds(PlayerEntrancePauseSeconds);
    }

    private IEnumerator RunDialogue(Stage1DialogueLine[] lines)
    {
        dialogue.Begin(lines, IntroCharacterInterval, IntroLineHoldSeconds);
        while (dialogue.IsActive)
        {
            dialogue.Tick(Time.deltaTime, Time.time);
            yield return null;
        }
    }

    private void OnDestroy()
    {
        dialogue.CharacterRevealed -= HandleDialogueCharacterRevealed;
    }

    private void HandleDialogueCharacterRevealed(Stage1DialogueLine line, char character)
    {
        if (dialogueVoice == null) return;

        DialogueVoiceProfile profile;
        if (line.Speaker == Stage1DialogueSpeaker.Player)
            profile = DialogueVoiceProfile.Player;
        else if (boss != null && boss.State == Stage1BossState.Dead)
            profile = DialogueVoiceProfile.BossNpc;
        else
            profile = DialogueVoiceProfile.Boss;

        dialogueVoice.PlayCharacter(character, profile);
    }

    private IEnumerator PrologueHighlighterRainRoutine()
    {
        if (player == null || boss == null || hazardPrefab == null) yield break;

        yield return SpawnHighlighterWave(new[] { -6.4f, -2.5f, 1.1f, 5.2f });
        yield return MovePlayerToX(3.1f, CutsceneWalkSpeed);
        yield return new WaitForSeconds(0.5f);

        yield return SpawnHighlighterWave(new[] { -5.5f, -1.2f, 3.8f });
        yield return MovePlayerToX(1.25f, CutsceneWalkSpeed);
        yield return new WaitForSeconds(0.55f);

        yield return SpawnHighlighterWave(new[] { -3.1f, 4.6f });
        yield return MovePlayerToX(2.45f, CutsceneWalkSpeed);
        yield return new WaitForSeconds(0.55f);

        // 마지막 한 자루는 현재 플레이어 위치를 정확히 노리고, 기존 대시 모션으로 피한다.
        SpawnCutsceneHighlighter(player.transform.position.x);
        yield return new WaitForSeconds(0.95f);

        float leftSpace = player.transform.position.x - ArenaLeft;
        float rightSpace = ArenaRight - player.transform.position.x;
        float dashDirection = rightSpace >= leftSpace ? 1f : -1f;
        IsCutsceneMovingPlayer = true;
        yield return player.PlayCutsceneDash(dashDirection, 1.8f);
        IsCutsceneMovingPlayer = false;
        player.ForceIdlePose();
        yield return new WaitForSeconds(0.65f);
    }

    private IEnumerator SpawnHighlighterWave(float[] xPositions)
    {
        for (int i = 0; i < xPositions.Length; i++)
        {
            SpawnCutsceneHighlighter(xPositions[i] + Random.Range(-0.08f, 0.08f));
            if (i < xPositions.Length - 1)
                yield return new WaitForSeconds(IntroHighlighterSequenceInterval);
        }
    }

    private Stage1Projectile SpawnCutsceneHighlighter(float x)
    {
        Stage1Projectile highlighter = SpawnHazard(new Vector2(x, 0f), Vector2.zero, 0f, 4f);
        highlighter.ConfigureCutsceneHazard(false);
        boss.ConfigureHighlighterPattern(highlighter);
        return highlighter;
    }

    private IEnumerator MovePlayerToX(float targetX, float speed)
    {
        if (player == null) yield break;

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        targetX = Mathf.Clamp(targetX, ArenaLeft + 0.45f, ArenaRight - 0.45f);
        float direction = Mathf.Sign(targetX - player.transform.position.x);
        player.SetCutsceneFacing(direction);
        IsCutsceneMovingPlayer = true;

        while (Mathf.Abs(player.transform.position.x - targetX) > 0.02f)
        {
            float nextX = Mathf.MoveTowards(player.transform.position.x, targetX, speed * Time.deltaTime);
            Vector3 position = player.transform.position;
            player.transform.position = new Vector3(nextX, position.y, position.z);
            if (playerBody != null)
                playerBody.linearVelocity = new Vector2(direction * speed, playerBody.linearVelocity.y);
            yield return null;
        }

        Vector3 finalPosition = player.transform.position;
        player.transform.position = new Vector3(targetX, finalPosition.y, finalPosition.z);
        if (playerBody != null) playerBody.linearVelocity = Vector2.zero;
        IsCutsceneMovingPlayer = false;
        player.ForceIdlePose();
    }

    private IEnumerator BattleRoutine()
    {
        yield return new WaitForSeconds(1f);

        boss.BeginAttack(1, false);

        int overloadHealth = Mathf.CeilToInt(Stage1Boss.MaxHealth * OverloadHealthRatio);
        while (!BattleEnded && boss.CurrentHealth > overloadHealth)
            yield return null;

        if (BattleEnded || boss.CurrentHealth <= 0) yield break;

        // Health threshold trigger -> one-shot overload event.
        boss.BeginAttack(1, true);
        // General SFX uses a 0.3 bus while the nominal BGM mix is 0.5.
        // 5.0 * 0.3 = 1.5, which is 3 times the nominal BGM level.
        GameSfx.Play(GameSfxId.HardMode, 5f);
        StartCoroutine(OverloadCameraShakeRoutine());
        yield return WaitBattleSeconds(OverloadDurationSeconds);
        if (BattleEnded) yield break;

        // Resume attacking immediately when overload ends; no groggy phase.
        boss.BeginAttack(2, false);
    }

    private IEnumerator OverloadCameraShakeRoutine()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;

        Transform cameraTransform = mainCamera.transform;
        Vector3 origin = cameraTransform.localPosition;
        float seed = Random.value * 100f;
        float elapsed = 0f;

        while (elapsed < OverloadShakeDuration)
        {
            float progress = Mathf.Clamp01(elapsed / OverloadShakeDuration);
            float strength = OverloadShakeMagnitude * (1f - progress) * (1f - progress);
            float sampleTime = Time.unscaledTime * OverloadShakeFrequency;
            float x = (Mathf.PerlinNoise(seed, sampleTime) - 0.5f) * 2f * strength;
            float y = (Mathf.PerlinNoise(seed + 19f, sampleTime) - 0.5f) * 2f * strength;
            cameraTransform.localPosition = origin + new Vector3(x, y, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (cameraTransform != null)
            cameraTransform.localPosition = origin;
    }

    private IEnumerator SpawnMachineGunRoutine()
    {
        yield return WaitBattleSeconds(30f);
        if (BattleEnded || machineGunPickupPrefab == null) yield break;

        float x = Random.value < 0.5f ? -4.4f : 4.4f;
        float surfaceY = FindHighestPickupSurfaceY(x);
        float pickupCenterY = machineGunPickupPrefab.GetCenterYForSurface(surfaceY);
        Stage1MachineGunPickup pickup = Instantiate(machineGunPickupPrefab, new Vector2(x, pickupCenterY), Quaternion.identity);
        pickup.Initialize(this);
    }

    private static float FindHighestPickupSurfaceY(float x)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            new Vector2(x, PickupDropRayStartY),
            Vector2.down,
            PickupDropRayDistance);

        float highestSurfaceY = float.NegativeInfinity;
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D hitCollider = hit.collider;
            if (hitCollider == null || hitCollider.isTrigger) continue;

            bool isPlatformSurface = hitCollider.GetComponent<PlatformEffector2D>() != null;
            bool isFloor = hitCollider.gameObject.name == "Floor";
            if (!isPlatformSurface && !isFloor) continue;

            highestSurfaceY = Mathf.Max(highestSurfaceY, hit.point.y);
        }

        return float.IsNegativeInfinity(highestSurfaceY)
            ? PickupSurfaceFallbackY
            : highestSurfaceY;
    }

    private IEnumerator WaitBattleSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds && !BattleEnded)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void StartBattleMusicTransition()
    {
        if (battleMusicTransitionStarted) return;
        battleMusicTransitionStarted = true;
        PlayMusic(battleMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0f;
        }

        if (musicSource.clip == clip) return;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        bool hasCurrentMusic = musicSource.clip != null && musicSource.isPlaying;
        float fadeOutSeconds = hasCurrentMusic ? MusicTransitionFadeOutSeconds : 0f;
        float fadeInSeconds = clip == introMusic ? IntroMusicFadeInSeconds : BattleMusicFadeInSeconds;
        musicFadeRoutine = StartCoroutine(FadeToMusic(clip, fadeOutSeconds, fadeInSeconds));
    }

    private void LoadStageMusic()
    {
        if (introMusic == null)
            introMusic = Resources.Load<AudioClip>(IntroMusicResourcePath);
        if (battleMusic == null)
            battleMusic = Resources.Load<AudioClip>(BattleMusicResourcePath);
        if (rewardIcon == null)
            rewardIcon = Resources.Load<Texture2D>(RewardIconResourcePath);
        if (playerMugshotTexture == null)
            playerMugshotTexture = Resources.Load<Texture2D>(PlayerMugshotResourcePath);
        if (playerMugshotBoxTexture == null)
            playerMugshotBoxTexture = Resources.Load<Texture2D>(PlayerMugshotBoxResourcePath);
        if (bossNpcMugshotTexture == null)
            bossNpcMugshotTexture = Resources.Load<Texture2D>(BossNpcMugshotResourcePath);
        if (bossNpcMugshotBoxTexture == null)
            bossNpcMugshotBoxTexture = Resources.Load<Texture2D>(BossNpcMugshotBoxResourcePath);
        if (playerMugshotTexture != null)
            playerMugshotTexture.filterMode = FilterMode.Point;
        if (playerMugshotBoxTexture != null)
            playerMugshotBoxTexture.filterMode = FilterMode.Point;
        if (bossNpcMugshotTexture != null)
            bossNpcMugshotTexture.filterMode = FilterMode.Point;
        if (bossNpcMugshotBoxTexture != null)
            bossNpcMugshotBoxTexture.filterMode = FilterMode.Point;
    }

    private IEnumerator FadeToMusic(AudioClip clip, float fadeOutSeconds, float fadeInSeconds)
    {
        if (fadeOutSeconds > 0f)
            yield return FadeMusicVolume(musicSource.volume, 0f, fadeOutSeconds);

        musicSource.Stop();
        musicSource.clip = clip;

        if (clip == null)
        {
            musicFadeRoutine = null;
            yield break;
        }

        musicSource.volume = 0f;
        musicSource.Play();
        yield return FadeMusicVolume(0f, MusicVolume * GameSettingsService.Data.musicVolume, fadeInSeconds);
        musicFadeRoutine = null;
    }

    private IEnumerator FadeMusicVolume(float from, float to, float seconds)
    {
        if (seconds <= 0f)
        {
            musicSource.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            musicSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        musicSource.volume = to;
    }

    public Stage1Projectile SpawnBasicPlayerShot(Vector2 position, Vector2 direction, float speed)
    {
        Stage1Projectile shot = Instantiate(basicPlayerShotPrefab, position, Quaternion.identity);
        shot.Initialize(this, direction, speed, true, 2, 4f);
        return shot;
    }

    public Stage1Projectile SpawnMachineGunShot(Vector2 position, Vector2 direction, float speed, int visualVariantIndex)
    {
        Stage1Projectile shot = Instantiate(machineGunShotPrefab, position, Quaternion.identity);
        shot.Initialize(this, direction, speed, true, 2, 4f);
        shot.SetVisualVariant(visualVariantIndex);
        return shot;
    }

    public Stage1Projectile SpawnHazard(Vector2 position, Vector2 direction, float speed, float lifetime)
    {
        Stage1Projectile hazard = Instantiate(hazardPrefab, position, Quaternion.identity);
        hazard.Initialize(this, direction, speed, false, 1, lifetime);
        return hazard;
    }

    public void GameOver()
    {
        if (BattleEnded || freeRoamMode) return;
        BattleEnded = true;
        resultTitle = "GAME OVER";
        boss.StopBattle();
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        if (player != null)
        {
            PlayerDeathMotion motion = player.GetComponent<PlayerDeathMotion>();
            if (motion == null) motion = player.gameObject.AddComponent<PlayerDeathMotion>();
            yield return motion.Play();
        }
        Time.timeScale = 0f;
        gameMenu?.ShowGameOver();
    }

    public void StageClear()
    {
        if (freeRoamMode || resultTitle == "STAGE CLEAR") return;
        BattleEnded = true;
        stageClearCutsceneActive = false;
        ControlsLocked = true;
        resultTitle = "STAGE CLEAR";
        boss.StopBattle();
        Time.timeScale = 0f;
        gameMenu?.ShowStageClear("수철햄을 정화했습니다!", "반짝 해바라기씨 × 1", "World Map");
    }

    public void BeginStageClearAnimation()
    {
        if (BattleEnded || freeRoamMode) return;
        BattleEnded = true;
        ControlsLocked = true;
        bossHealthBarVisible = false;
        resultTitle = null;
    }

    public void BeginStageClearSequence()
    {
        if (freeRoamMode || stageClearSequenceStarted) return;
        stageClearSequenceStarted = true;
        StartCoroutine(StageClearSequenceRoutine());
    }

    private IEnumerator StageClearSequenceRoutine()
    {
        stageClearCutsceneActive = true;
        ControlsLocked = true;

        if (player == null || boss == null)
        {
            StageClear();
            yield break;
        }

        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        float playerHalfWidth = playerRenderer != null ? playerRenderer.bounds.extents.x : 0.35f;
        // 사망 시트 마지막 프레임은 투명 여백이 크므로 Sprite.bounds가 아니라
        // 실제 작은 햄스터의 시각 폭을 기준으로 바로 왼쪽 위치를 계산한다.
        float talkPositionX = boss.transform.position.x
                              - EpilogueHamsterHalfWidth
                              - playerHalfWidth
                              - EpilogueBossGap;

        yield return MovePlayerToX(talkPositionX, CutsceneWalkSpeed);
        player.SetCutsceneFacing(1f);
        yield return RunDialogue(EpilogueOpeningDialogue);

        float exitTargetX = Mathf.Min(ArenaRight - 0.45f, player.transform.position.x + EpilogueExitWalkDistance);
        yield return MovePlayerToX(exitTargetX, CutsceneWalkSpeed * 0.85f);
        yield return new WaitForSeconds(0.18f);
        player.SetCutsceneFacing(1f);
        yield return RunDialogue(EpilogueRewardDialogue);

        rewardPopupActive = true;
        rewardPopupClosesAt = Time.time + RewardPopupAutoCloseSeconds;
        while (rewardPopupActive && Time.time < rewardPopupClosesAt)
            yield return null;
        rewardPopupActive = false;

        yield return RunDialogue(EpilogueClosingDialogue);
        StageClear();
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        noticeStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            normal = { textColor = Color.white }
        };
        hudLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        bossStageLabelStyle = new GUIStyle(hudLabelStyle) { fontSize = 26 };
        dialogueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 30,
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            padding = new RectOffset(18, 18, 14, 12),
            normal = { textColor = Color.white }
        };
        dialogueShadowStyle = new GUIStyle(dialogueStyle)
        {
            normal = { textColor = Color.black }
        };
        dialogueHighlightStyle = new GUIStyle(dialogueStyle)
        {
            normal = { textColor = new Color(0.58f, 0.82f, 1f, 0.75f) }
        };
        dialogueHintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.86f, 0.91f, 1f) }
        };
        dialogueHintShadowStyle = new GUIStyle(dialogueHintStyle)
        {
            normal = { textColor = Color.black }
        };
        rewardNameStyle = new GUIStyle(dialogueStyle)
        {
            fontSize = 19,
            normal = { textColor = new Color(1f, 0.88f, 0.38f) }
        };
        GameTypography.ApplyDialogueFont(
            dialogueStyle,
            dialogueShadowStyle,
            dialogueHighlightStyle,
            dialogueHintStyle,
            dialogueHintShadowStyle,
            rewardNameStyle,
            hudLabelStyle,
            bossStageLabelStyle);
    }

    private void OnGUI()
    {
        GameTypography.ApplyToCurrentSkin();
        EnsureStyles();
        bool cutsceneActive = introCutsceneActive || stageClearCutsceneActive;
        if (!cutsceneActive)
            DrawHealth();

        if (dialogue.IsActive)
            DrawDialogue();

        if (rewardPopupActive)
            DrawRewardPopup();

        if (stageFadeAlpha > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, stageFadeAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

    }

    private void DrawDialogue()
    {
        if (!dialogue.IsActive) return;

        float width = Mathf.Min(Screen.width - 36f, 560f);
        float height = 150f;
        bool isPlayerLine = dialogue.CurrentLine.Speaker == Stage1DialogueSpeaker.Player;
        bool showBossNpcMugshot = dialogue.CurrentLine.Speaker == Stage1DialogueSpeaker.Boss &&
                                  boss != null && boss.State == Stage1BossState.Dead;
        float mugshotPixelScale = isPlayerLine || showBossNpcMugshot ? GetPixelPerfectUiScale() : 1f;
        Vector2 anchor = GetSpeechScreenAnchor(dialogue.CurrentLine.Speaker);
        float left = Mathf.Clamp(anchor.x - width * 0.5f, 18f, Screen.width - width - 18f);
        float maximumTop = Screen.height - height - 18f;
        float requestedMinimumTop = isPlayerLine || showBossNpcMugshot
            ? 18f + (48f * PlayerMugshotSizeRatio + PlayerMugshotBubbleGapPixels) * mugshotPixelScale
            : 18f;
        float minimumTop = Mathf.Min(requestedMinimumTop, maximumTop);
        float lowerOffset = showBossNpcMugshot ? 38f * mugshotPixelScale : 0f;
        float top = Mathf.Clamp(anchor.y - height - 24f + lowerOffset, minimumTop, maximumTop);
        Rect bubbleRect = new Rect(left, top, width, height);
        Rect shadowRect = new Rect(left + 4f, top + 4f, width, height);

        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(shadowRect, Texture2D.whiteTexture);

        GUI.color = new Color(0.03f, 0.035f, 0.045f, 0.96f);
        GUI.DrawTexture(bubbleRect, Texture2D.whiteTexture);
        GUI.color = new Color(0.84f, 0.9f, 1f, 1f);
        DrawPixelBorder(bubbleRect, 4f);
        DrawPixelTail(anchor.x, top + height);

        GUI.color = Color.white;
        if (isPlayerLine)
            DrawPlayerMugshot(left, top, mugshotPixelScale);
        else if (showBossNpcMugshot)
            DrawBossNpcMugshot(left, top, mugshotPixelScale);

        string displayName = GetDialogueDisplayName(dialogue.CurrentLine.Speaker);
        string displayedText = string.IsNullOrEmpty(displayName)
            ? dialogue.VisibleText
            : displayName + " : " + dialogue.VisibleText;
        DrawPixelText(
            new Rect(left + 4f, top + 4f, width - 8f, height - 8f),
            displayedText,
            dialogueStyle,
            dialogueShadowStyle,
            dialogueHighlightStyle);
        DrawPixelText(new Rect(left + width - 124f, top + height - 26f, 108f, 18f), "SPACE", dialogueHintStyle, dialogueHintShadowStyle, null);
    }

    private string GetDialogueDisplayName(Stage1DialogueSpeaker speaker)
    {
        if (speaker == Stage1DialogueSpeaker.Player) return "김냥이";
        return boss != null && boss.State == Stage1BossState.Dead ? "수철햄" : string.Empty;
    }

    private void DrawPlayerMugshot(float bubbleLeft, float bubbleTop, float pixelScale)
    {
        if (playerMugshotTexture == null || playerMugshotBoxTexture == null) return;

        // One source pixel maps to the same integer screen-pixel scale used by
        // the 480x270 Pixel Perfect Camera. Native sizes remain 48x48 / 45x30.
        float portraitScale = pixelScale * PlayerMugshotSizeRatio;
        float boxSize = 48f * portraitScale;
        float bubbleGap = PlayerMugshotBubbleGapPixels * pixelScale;
        Rect boxRect = new Rect(bubbleLeft, bubbleTop - bubbleGap - boxSize, boxSize, boxSize);
        Rect mugshotRect = new Rect(
            boxRect.x + 1.5f * portraitScale,
            boxRect.y + 9f * portraitScale,
            45f * portraitScale,
            30f * portraitScale);

        GUI.color = Color.white;
        GUI.DrawTexture(boxRect, playerMugshotBoxTexture, ScaleMode.StretchToFill, true);

        int frameIndex = GetPlayerMugshotFrameIndex(dialogue.CurrentLine.Text);
        Rect textureCoordinates = new Rect(frameIndex / 3f, 0f, 1f / 3f, 1f);
        GUI.DrawTextureWithTexCoords(mugshotRect, playerMugshotTexture, textureCoordinates, true);
    }

    private void DrawBossNpcMugshot(float bubbleLeft, float bubbleTop, float pixelScale)
    {
        if (bossNpcMugshotTexture == null || bossNpcMugshotBoxTexture == null) return;
        float portraitScale = pixelScale * PlayerMugshotSizeRatio;
        float boxSize = 48f * portraitScale;
        float bubbleGap = PlayerMugshotBubbleGapPixels * pixelScale;
        Rect boxRect = new Rect(bubbleLeft, bubbleTop - bubbleGap - boxSize, boxSize, boxSize);
        float faceSize = 31f * portraitScale;
        Rect faceRect = new Rect(
            boxRect.center.x - faceSize * 0.5f,
            boxRect.center.y - faceSize * 0.5f,
            faceSize,
            faceSize);
        GUI.color = Color.white;
        GUI.DrawTexture(boxRect, bossNpcMugshotBoxTexture, ScaleMode.ScaleToFit, true);
        GUI.DrawTexture(faceRect, bossNpcMugshotTexture, ScaleMode.ScaleToFit, true);
    }

    private static float GetPixelPerfectUiScale()
    {
        float widthScale = Screen.width / PixelPerfectReferenceWidth;
        float heightScale = Screen.height / PixelPerfectReferenceHeight;
        return Mathf.Max(1f, Mathf.Floor(Mathf.Min(widthScale, heightScale)));
    }

    private static int GetPlayerMugshotFrameIndex(string line)
    {
        if (line == PlayerMugshotSecondExpressionLine) return 1;
        if (line == PlayerMugshotThirdExpressionLine) return 2;
        return 0;
    }

    private Vector2 GetSpeechScreenAnchor(Stage1DialogueSpeaker speaker)
    {
        Camera mainCamera = Camera.main;
        Transform speakerTransform = speaker == Stage1DialogueSpeaker.Player
            ? player != null ? player.transform : null
            : boss != null ? boss.transform : null;
        if (speakerTransform == null || mainCamera == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);

        float verticalOffset = speaker == Stage1DialogueSpeaker.Player ? 1.15f : 1.45f;
        Vector3 worldPosition = speakerTransform.position + new Vector3(0f, verticalOffset, 0f);
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
    }

    private void DrawRewardPopup()
    {
        Matrix4x4 backdropMatrix = PixelUiTheme.BeginReferenceCanvas();
        PixelUiTheme.DrawBackdrop();
        PixelUiTheme.EndReferenceCanvas(backdropMatrix);
        Matrix4x4 previous = PixelUiTheme.BeginReferenceCanvas(0.5f);
        Rect panel = new Rect(52f, 38f, 376f, 194f);
        PixelUiTheme.DrawPanel(panel, PixelUiTheme.Gold);
        PixelUiTheme.Title(new Rect(panel.x, panel.y + 10f, panel.width, 30f), "아이템을 획득했다!", PixelUiTheme.Gold);
        Rect itemArea = new Rect(panel.x + 22f, panel.y + 48f, panel.width - 44f, 104f);
        PixelUiTheme.DrawInset(itemArea);
        if (rewardIcon != null)
            GUI.DrawTexture(new Rect(itemArea.x + 12f, itemArea.y + 19f, 66f, 66f), rewardIcon, ScaleMode.ScaleToFit, true);
        PixelUiTheme.Label(new Rect(itemArea.x + 91f, itemArea.y + 12f, itemArea.width - 103f, 27f), "반짝 해바라기씨 × 1", TextAnchor.MiddleLeft, PixelUiTheme.Gold);
        PixelUiTheme.Label(new Rect(itemArea.x + 91f, itemArea.y + 39f, itemArea.width - 103f, 52f), "수철햄이 건네준 비상식량.\n무려 한 톨이다!", TextAnchor.UpperLeft, PixelUiTheme.Text, true);
        PixelUiTheme.Hint(new Rect(panel.x + 18f, panel.yMax - 27f, panel.width - 36f, 16f), "SPACE 닫기");
        PixelUiTheme.EndReferenceCanvas(previous);
    }

    private static void DrawPixelText(Rect rect, string text, GUIStyle style, GUIStyle shadowStyle, GUIStyle highlightStyle)
    {
        if (highlightStyle != null)
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, highlightStyle);

        GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width, rect.height), text, shadowStyle);
        GUI.Label(new Rect(rect.x, rect.y + 2f, rect.width, rect.height), text, shadowStyle);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, shadowStyle);
        GUI.Label(rect, text, style);
    }

    private static void DrawPixelBorder(Rect rect, float thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }

    private static void DrawPixelTail(float x, float y)
    {
        float step = 4f;
        float left = x - step * 3f;
        GUI.DrawTexture(new Rect(left, y, step * 7f, step), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(left + step, y + step, step * 5f, step), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(left + step * 2f, y + step * 2f, step * 3f, step), Texture2D.whiteTexture);

        GUI.color = new Color(0.03f, 0.035f, 0.045f, 0.96f);
        GUI.DrawTexture(new Rect(left + step, y, step * 5f, step), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(left + step * 2f, y + step, step * 3f, step), Texture2D.whiteTexture);
        GUI.color = new Color(0.84f, 0.9f, 1f, 1f);
    }

    private void DrawHealth()
    {
        if (!bossHealthBarVisible) return;

        HudPixelGauges.DrawPlayerHearts(margin: 12f, currentHealth: player.DisplayedHealth, maxHealth: 3, bottomAligned: true);
        HudPixelGauges.DrawBossPurificationMeter(
            screenWidth: Screen.width,
            margin: 12f,
            currentHealth: boss.CurrentHealth,
            maxHealth: Stage1Boss.MaxHealth,
            labelStyle: hudLabelStyle,
            stageLabel: "STAGE 1 : 수철햄 [쳇바퀴홍보학과]",
            visualScale: 1.6f,
            stageLabelStyle: bossStageLabelStyle);
        GUI.color = Color.white;
    }
}
