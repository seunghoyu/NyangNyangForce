using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PrologueController : MonoBehaviour
{
    private enum Speaker
    {
        Player,
        Rabbit
    }

    private readonly struct Line
    {
        public readonly Speaker Speaker;
        public readonly string Text;
        public readonly bool IsThought;
        public readonly int MugshotFrameIndex;
        public readonly bool UsePlayerMugshot2;
        public readonly bool RabbitLooksLeft;
        public readonly bool UseExplanationColor;

        public Line(
            Speaker speaker,
            string text,
            bool isThought = false,
            int mugshotFrame = 1,
            bool usePlayerMugshot2 = false,
            bool rabbitLooksLeft = false,
            bool useExplanationColor = false)
        {
            Speaker = speaker;
            Text = text;
            IsThought = isThought;
            MugshotFrameIndex = Mathf.Max(0, mugshotFrame - 1);
            UsePlayerMugshot2 = usePlayerMugshot2;
            RabbitLooksLeft = rabbitLooksLeft;
            UseExplanationColor = useExplanationColor;
        }
    }

    private const string PlayerName = "김냥이";
    private const string RabbitName = "한토롱";
    private const float CharacterInterval = 0.045f;
    private const float LineHoldSeconds = 1.4f;
    private const float ReferenceWidth = 480f;
    private const float ReferenceHeight = 270f;
    private const float MugshotSizeRatio = 0.9f;
    private const float MugshotBubbleGapPixels = 12f;
    private const float WideCameraSize = 4.5f;
    private static readonly Vector3 WideCameraPosition = new Vector3(0f, 0f, -10f);
    private static readonly Vector3 FlagCloseUpPosition = new Vector3(0f, 0.95f, -10f);
    private const float FlagCloseUpSize = 2.45f;

    [Header("Scene art")]
    public Texture2D backgroundTexture;
    public Texture2D playerPrologueStandingSheet;
    public Texture2D playerWalkSheet;
    public Texture2D playerArmedStandingSheet;
    public Texture2D playerRunSheet;
    public Texture2D playerJumpSheet;
    public Texture2D playerPowerfallWindupSheet;
    public Texture2D playerPowerfallFallSheet;
    public Texture2D playerPowerfallLandSheet;
    public Texture2D rabbitStandingSheet;
    public Texture2D rabbitWalkSheet;
    public Texture2D gunTexture;

    [Header("Dialogue art")]
    public Texture2D playerMugshotSheet;
    public Texture2D playerMugshotSheet2;
    public Texture2D playerMugshotFrame;
    public Texture2D rabbitMugshotSheet;
    public Texture2D rabbitMugshotFrame;

    [Header("Audio")]
    public AudioClip hardModeSound;
    public AudioClip playerVoice;
    public AudioClip rabbitVoice;
    public AudioClip slamImpactSound;
    public GameObject slamImpactEffectPrefab;

    private Camera mainCamera;
    private Transform backgroundTransform;
    private Vector3 backgroundWideScale;
    private Transform playerTransform;
    private Transform rabbitTransform;
    private SpriteRenderer playerRenderer;
    private SpriteRenderer rabbitRenderer;
    private SpriteRenderer gunRenderer;
    private Rigidbody2D playerBody;
    private Rigidbody2D rabbitBody;
    private Sprite[] playerRunFrames;
    private Sprite[] playerJumpFrames;
    private Sprite[] playerWalkFrames;
    private Sprite[] rabbitWalkFrames;
    private Sprite[] playerPrologueIdleFrames;
    private Sprite[] playerArmedIdleFrames;
    private Sprite[] rabbitIdleFrames;
    private Sprite[] powerfallWindupFrames;
    private Sprite[] powerfallFallFrames;
    private Sprite[] powerfallLandFrames;
    private float walkFrameTimer;
    private int walkFrameIndex;
    private bool actorsWalking;
    private float idleFrameTimer;
    private int idleFrameIndex;
    private bool playerArmed;
    private float playerRunFrameTimer;
    private int playerRunFrameIndex;
    private bool playerRunning;
    private bool playerCinematicAction;
    private bool playerStationaryAnchorSet;
    private float playerStationaryX;
    private bool rabbitStationaryAnchorSet;
    private float rabbitStationaryX;
    private AudioSource sfxSource;
    private AudioSource voiceSource;

    private Line currentLine;
    private bool dialogueActive;
    private int visibleCharacters;
    private float characterTimer;
    private float lineCompleteAt;
    private bool advanceRequested;

    private string narrationText;
    private float narrationAlpha;
    private float flashAlpha;
    private float fadeAlpha;
    private bool inputLocked = true;
    private bool previousRunInBackground;

    private GUIStyle narrationStyle;
    private GUIStyle dialogueStyle;
    private GUIStyle dialogueShadowStyle;
    private GUIStyle dialogueHighlightStyle;
    private GUIStyle dialogueExplanationStyle;
    private GUIStyle dialogueExplanationHighlightStyle;
    private GUIStyle dialogueHintStyle;
    private GUIStyle dialogueHintShadowStyle;
    private Texture2D playerMugshotFirstFrame;

    private static readonly Line[] OpeningDialogue =
    {
        new Line(Speaker.Player, "저기… 나 너한테 할 말이 있어.", mugshotFrame: 2, usePlayerMugshot2: true),
        new Line(Speaker.Rabbit, "응? 뭔데?", mugshotFrame: 3, rabbitLooksLeft: true)
    };

    private static readonly Line[] AfterImpactDialogue =
    {
        new Line(Speaker.Rabbit, "뭐, 뭐야?! 방금 그 소리!", mugshotFrame: 2),
        new Line(Speaker.Player, "학교 쪽에서 난 것 같은데… 무슨 일이지?", mugshotFrame: 1, usePlayerMugshot2: true),
        new Line(Speaker.Player, "아차, 내 가방! 학교에 두고 왔어!", mugshotFrame: 1, usePlayerMugshot2: true),
        new Line(Speaker.Player, "(안에 러브레터도 들어 있는데…!)", true, mugshotFrame: 1, usePlayerMugshot2: true),
        new Line(Speaker.Player, "나 학교에 다시 가봐야겠어.", mugshotFrame: 1),
        new Line(Speaker.Rabbit, "잠깐! 지금은 위험할지도 몰라.", mugshotFrame: 1),
        new Line(Speaker.Rabbit, "이걸 가지고 가. 없는 것보단 나을 거야.", mugshotFrame: 1)
    };

    private static readonly Line[] GunDialogueBeforePause =
    {
        new Line(Speaker.Player, "이걸 나한테 주는 거야?", mugshotFrame: 3),
        new Line(Speaker.Player, "(잠깐. 얘는 평소에 총을 가지고 다니는 건가?)", true, mugshotFrame: 3)
    };

    private static readonly Line[] GunDialogueAfterPause =
    {
        new Line(Speaker.Player, "(…왜 가지고 다니는지는 묻지 말자.\n한 자루 더 있을지도 모르니까.)", true, mugshotFrame: 3),
        new Line(Speaker.Rabbit, "이건 탄종을 가리지 않는 특제 리볼버야.\n손에 넣은 탄환이라면 뭐든 장전해서 쏠 수 있어.", mugshotFrame: 3, useExplanationColor: true),
        new Line(Speaker.Rabbit, "아무튼 조심해. 무슨 일이 생기면 바로 돌아오고!", mugshotFrame: 3),
        new Line(Speaker.Player, "…알겠어. 다녀올게!", mugshotFrame: 2, usePlayerMugshot2: true)
    };

    private static readonly Line[] FarewellDialogue =
    {
        new Line(Speaker.Rabbit, "…조심히 다녀와.", mugshotFrame: 1)
    };

    private void Awake()
    {
        Time.timeScale = 1f;
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        TitleWorldMapMusic.EnsurePlaying();
        mainCamera = Camera.main;
        if (playerJumpSheet == null)
            playerJumpSheet = Resources.Load<Texture2D>("Player/Art/player_jump");
        CreateVisuals();
        CreateAudio();
    }

    private void OnDestroy()
    {
        Application.runInBackground = previousRunInBackground;
        if (playerMugshotFirstFrame != null) Destroy(playerMugshotFirstFrame);
    }

    private void Start()
    {
        StartCoroutine(PrologueRoutine());
    }

    private void Update()
    {
        if (!actorsWalking)
        {
            idleFrameTimer += Time.deltaTime;
            if (idleFrameTimer >= 0.25f)
            {
                idleFrameTimer -= 0.25f;
                idleFrameIndex++;

                if (!playerRunning && !playerCinematicAction && playerRenderer != null)
                {
                    Sprite[] playerIdleFrames = playerArmed
                        ? playerArmedIdleFrames
                        : playerPrologueIdleFrames;
                    if (playerIdleFrames != null && playerIdleFrames.Length > 0)
                        playerRenderer.sprite = playerIdleFrames[idleFrameIndex % playerIdleFrames.Length];
                }

                if (rabbitRenderer != null && rabbitIdleFrames != null && rabbitIdleFrames.Length > 0)
                    rabbitRenderer.sprite = rabbitIdleFrames[idleFrameIndex % rabbitIdleFrames.Length];
            }
        }

        if (playerStationaryAnchorSet && !actorsWalking && !playerRunning && !playerCinematicAction && playerTransform != null)
        {
            Vector3 anchored = playerTransform.position;
            anchored.x = playerStationaryX;
            SetBodyPosition(playerBody, playerTransform, anchored);
        }
        if (rabbitStationaryAnchorSet && !actorsWalking && rabbitTransform != null)
        {
            Vector3 anchored = rabbitTransform.position;
            anchored.x = rabbitStationaryX;
            SetBodyPosition(rabbitBody, rabbitTransform, anchored);
        }

        if (actorsWalking)
        {
            walkFrameTimer += Time.deltaTime;
            if (walkFrameTimer >= 0.12f)
            {
                walkFrameTimer -= 0.12f;
                walkFrameIndex++;
                if (playerWalkFrames != null && playerWalkFrames.Length > 0)
                    playerRenderer.sprite = playerWalkFrames[walkFrameIndex % playerWalkFrames.Length];
                if (rabbitWalkFrames != null && rabbitWalkFrames.Length > 0)
                    rabbitRenderer.sprite = rabbitWalkFrames[walkFrameIndex % rabbitWalkFrames.Length];
            }
        }

        if (playerRunning && playerRunFrames != null && playerRunFrames.Length > 0)
        {
            playerRunFrameTimer += Time.deltaTime;
            if (playerRunFrameTimer >= 0.1f)
            {
                playerRunFrameTimer -= 0.1f;
                playerRunFrameIndex = (playerRunFrameIndex + 1) % playerRunFrames.Length;
                playerRenderer.sprite = playerRunFrames[playerRunFrameIndex];
            }
        }

        if (inputLocked || !dialogueActive || Keyboard.current == null ||
            !Keyboard.current.spaceKey.wasPressedThisFrame) return;

        if (visibleCharacters < currentLine.Text.Length)
        {
            visibleCharacters = currentLine.Text.Length;
            lineCompleteAt = Time.unscaledTime;
        }
        else
        {
            advanceRequested = true;
        }
    }

    private IEnumerator PrologueRoutine()
    {
        inputLocked = false;
        Coroutine openingCamera = StartCoroutine(OpeningFlagRoutine());
        narrationText = "어느 평화로운 캠퍼스의 오후";
        Coroutine entrance = StartCoroutine(MoveActorsIntoScene());
        yield return FadeValue(value => narrationAlpha = value, 0f, 1f, 0.6f);
        yield return new WaitForSecondsRealtime(1.8f);
        yield return FadeValue(value => narrationAlpha = value, 1f, 0f, 0.6f);
        narrationText = string.Empty;
        yield return entrance;
        yield return openingCamera;

        yield return RunDialogue(OpeningDialogue);
        yield return new WaitForSecondsRealtime(1.7f);
        yield return FlashRoutine();

        if (sfxSource != null && hardModeSound != null)
            sfxSource.PlayOneShot(hardModeSound, 1f);
        yield return CameraShakeRoutine();

        yield return RunDialogue(AfterImpactDialogue);
        yield return GunTransferRoutine();
        yield return RunDialogue(GunDialogueBeforePause);
        yield return new WaitForSecondsRealtime(0.5f);
        yield return RunDialogue(GunDialogueAfterPause);
        yield return PlayerExitRoutine();
        yield return RabbitCloseUpRoutine();
        yield return RunDialogue(FarewellDialogue);
        yield return FadeValue(value => fadeAlpha = value, 0f, 1f, 0.8f);
        SceneTransition.Load("Tutorial");
    }

    private IEnumerator OpeningFlagRoutine()
    {
        if (mainCamera == null) yield break;
        const float closeOutDuration = 1.5f;
        fadeAlpha = 0f;
        float elapsed = 0f;
        while (elapsed < closeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / closeOutDuration));
            mainCamera.transform.position = Vector3.Lerp(FlagCloseUpPosition, WideCameraPosition, t);
            mainCamera.orthographicSize = Mathf.Lerp(FlagCloseUpSize, WideCameraSize, t);
            yield return null;
        }
        mainCamera.transform.position = WideCameraPosition;
        mainCamera.orthographicSize = WideCameraSize;
    }

    private IEnumerator MoveActorsIntoScene()
    {
        const float duration = 2.25f * 2.5f;
        float elapsed = 0f;
        const float actorCenterY = -2f; // Kinematic cutscene baseline requested for both actors.
        Vector3 playerStart = new Vector3(-10.2f, actorCenterY, 0f);
        Vector3 rabbitStart = new Vector3(-8.8f, actorCenterY, 0f);
        Vector3 playerEnd = new Vector3(-1.25f, actorCenterY, 0f);
        Vector3 rabbitEnd = new Vector3(0.55f, actorCenterY, 0f);

        playerWalkFrames = CreateFrames(playerWalkSheet, 30, 30, 30f);
        rabbitWalkFrames = CreateFrames(rabbitWalkSheet, 30, 30, 30f);
        walkFrameTimer = 0f;
        walkFrameIndex = 0;
        actorsWalking = true;
        if (playerWalkFrames.Length > 0) playerRenderer.sprite = playerWalkFrames[0];
        if (rabbitWalkFrames.Length > 0) rabbitRenderer.sprite = rabbitWalkFrames[0];

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetBodyPosition(playerBody, playerTransform, Vector3.Lerp(playerStart, playerEnd, t));
            SetBodyPosition(rabbitBody, rabbitTransform, Vector3.Lerp(rabbitStart, rabbitEnd, t));
            yield return null;
        }

        actorsWalking = false;
        playerStationaryX = playerEnd.x;
        playerStationaryAnchorSet = true;
        rabbitStationaryX = rabbitEnd.x;
        rabbitStationaryAnchorSet = true;
        SetBodyPosition(playerBody, playerTransform, playerEnd);
        SetBodyPosition(rabbitBody, rabbitTransform, rabbitEnd);
        playerRenderer.sprite = CreateFirstFrame(playerPrologueStandingSheet, 30f);
        rabbitRenderer.sprite = CreateFirstFrame(rabbitStandingSheet, 30f);
    }

    private IEnumerator RunDialogue(Line[] lines)
    {
        foreach (Line line in lines)
        {
            currentLine = line;
            if (currentLine.RabbitLooksLeft && rabbitRenderer != null)
                rabbitRenderer.flipX = true;
            visibleCharacters = 0;
            characterTimer = 0f;
            lineCompleteAt = 0f;
            advanceRequested = false;
            dialogueActive = true;

            while (!advanceRequested)
            {
                if (visibleCharacters < currentLine.Text.Length)
                {
                    characterTimer += Time.unscaledDeltaTime;
                    while (characterTimer >= CharacterInterval && visibleCharacters < currentLine.Text.Length)
                    {
                        characterTimer -= CharacterInterval;
                        char character = currentLine.Text[visibleCharacters++];
                        PlayVoiceCharacter(character, currentLine.Speaker);
                    }

                    if (visibleCharacters >= currentLine.Text.Length)
                        lineCompleteAt = Time.unscaledTime;
                }
                else if (Time.unscaledTime - lineCompleteAt >= LineHoldSeconds)
                {
                    advanceRequested = true;
                }

                yield return null;
            }
        }

        dialogueActive = false;
    }

    private IEnumerator FlashRoutine()
    {
        yield return FadeValue(value => flashAlpha = value, 0f, 0.65f, 0.08f);
        yield return FadeValue(value => flashAlpha = value, 0.65f, 0f, 0.12f);
    }

    private IEnumerator CameraShakeRoutine()
    {
        if (mainCamera == null) yield break;
        const float duration = 1.15f;
        const float magnitude = 0.09f;
        const float frequency = 24f;
        Transform cameraTransform = mainCamera.transform;
        Vector3 origin = cameraTransform.localPosition;
        float seed = Random.value * 100f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float strength = magnitude * (1f - progress) * (1f - progress);
            float sampleTime = Time.unscaledTime * frequency;
            float x = (Mathf.PerlinNoise(seed, sampleTime) - 0.5f) * 2f * strength;
            float y = (Mathf.PerlinNoise(seed + 19f, sampleTime) - 0.5f) * 2f * strength;
            cameraTransform.localPosition = origin + new Vector3(x, y, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraTransform.localPosition = origin;
    }

    private IEnumerator GunTransferRoutine()
    {
        if (gunRenderer == null) yield break;
        playerCinematicAction = true;
        powerfallWindupFrames = CreateFrames(playerPowerfallWindupSheet, 30, 30, 30f);
        powerfallFallFrames = CreateFrames(playerPowerfallFallSheet, 30, 30, 30f);
        powerfallLandFrames = CreateFrames(playerPowerfallLandSheet, 30, 30, 30f);
        gunRenderer.enabled = true;
        Vector3 start = rabbitTransform.position + new Vector3(-0.35f, 0.35f, 0f);
        Vector3 playerGround = playerTransform.position;
        Vector3 catchPosition = playerGround + new Vector3(0.2f, 1.45f, 0f);
        const float duration = 1.25f;
        const float catchTime = 0.48f;
        float elapsed = 0f;
        bool gunCaught = false;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (t < catchTime)
            {
                float phase = t / catchTime;
                // Ascending: show Windup 0 -> 1 -> 2 -> 3 exactly once.
                SetAnimationFrame(playerRenderer, powerfallWindupFrames, phase);
                playerRenderer.transform.rotation = Quaternion.Euler(0f, 0f, -elapsed * 720f);
                Vector3 playerAirPosition = Vector3.Lerp(playerGround, catchPosition - new Vector3(0.2f, 0f, 0f), Mathf.SmoothStep(0f, 1f, phase));
                SetBodyPosition(playerBody, playerTransform, playerAirPosition);
                gunRenderer.transform.position = Vector3.Lerp(start, catchPosition, Mathf.SmoothStep(0f, 1f, phase))
                    + Vector3.up * Mathf.Sin(phase * Mathf.PI) * 0.65f;
            }
            else
            {
                if (!gunCaught)
                {
                    gunCaught = true;
                    gunRenderer.enabled = false;
                    playerArmed = true;
                }
                // Once the gun is caught, switch to the dedicated Powerfall_2 falling motion.
                playerRenderer.transform.rotation = Quaternion.identity;
                if (powerfallFallFrames.Length > 0)
                    playerRenderer.sprite = powerfallFallFrames[Mathf.FloorToInt((elapsed - duration * catchTime) * 12f) % powerfallFallFrames.Length];
                float phase = (t - catchTime) / (1f - catchTime);
                SetBodyPosition(playerBody, playerTransform,
                    Vector3.Lerp(catchPosition - new Vector3(0.2f, 0f, 0f), playerGround, phase * phase));
            }
            yield return null;
        }
        playerRenderer.transform.rotation = Quaternion.identity;
        SetBodyPosition(playerBody, playerTransform, playerGround);
        gunRenderer.enabled = false;
        if (sfxSource != null && slamImpactSound != null) sfxSource.PlayOneShot(slamImpactSound);
        if (slamImpactEffectPrefab != null)
            Instantiate(slamImpactEffectPrefab, playerGround + Vector3.down * (0.5f + 10f / 30f), Quaternion.identity);
        // Powerfall_3 is the landing follow-through and must finish before returning to armed idle.
        for (int i = 0; i < powerfallLandFrames.Length; i++)
        {
            playerRenderer.sprite = powerfallLandFrames[i];
            yield return new WaitForSecondsRealtime(0.065f);
        }
        idleFrameIndex = 0;
        idleFrameTimer = 0f;
        if (playerArmedIdleFrames.Length > 0) playerRenderer.sprite = playerArmedIdleFrames[0];
        playerCinematicAction = false;
    }

    private static void SetAnimationFrame(SpriteRenderer renderer, Sprite[] frames, float normalizedTime)
    {
        if (renderer == null || frames == null || frames.Length == 0) return;
        int index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(Mathf.Clamp01(normalizedTime) * frames.Length));
        renderer.sprite = frames[index];
    }

    private IEnumerator PlayerExitRoutine()
    {
        playerStationaryAnchorSet = false;
        playerRunFrames = CreateFrames(playerRunSheet, 30, 30, 30f);
        if (playerRunFrames.Length > 0) playerRenderer.sprite = playerRunFrames[0];
        playerRenderer.flipX = true;
        playerRunning = true;
        Vector3 start = playerTransform.position;
        Vector3 end = new Vector3(-10.2f, start.y, start.z);
        const float duration = 1.55f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetBodyPosition(playerBody, playerTransform, Vector3.Lerp(start, end, t));
            yield return null;
        }
        playerRunning = false;
    }

    private IEnumerator RabbitCloseUpRoutine()
    {
        if (mainCamera == null || rabbitTransform == null) yield break;
        Transform cameraTransform = mainCamera.transform;
        Vector3 startPosition = cameraTransform.position;
        Vector3 endPosition = new Vector3(rabbitTransform.position.x, -1.4f, startPosition.z);
        float startSize = mainCamera.orthographicSize;
        const float endSize = 2.45f;
        const float duration = 0.65f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cameraTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, endSize, t);
            yield return null;
        }
    }

    private static IEnumerator FadeValue(System.Action<float> setter, float from, float to, float duration)
    {
        float elapsed = 0f;
        setter(from);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            setter(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration))));
            yield return null;
        }
        setter(to);
    }

    private void CreateVisuals()
    {
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = WideCameraSize;
            mainCamera.transform.position = WideCameraPosition;
            mainCamera.backgroundColor = Color.black;
        }

        SpriteRenderer background = CreateRenderer("PrologueBackground", backgroundTexture, 100f, -10);
        if (background != null)
        {
            backgroundTexture.filterMode = FilterMode.Point;
            float visibleHeight = WideCameraSize * 2f;
            float visibleWidth = visibleHeight * (mainCamera != null ? mainCamera.aspect : 16f / 9f);
            Vector2 spriteSize = background.sprite.bounds.size;
            float scale = Mathf.Max(visibleWidth / spriteSize.x, visibleHeight / spriteSize.y);
            backgroundWideScale = new Vector3(scale, scale, 1f);
            backgroundTransform = background.transform;
            backgroundTransform.localScale = backgroundWideScale;
            backgroundTransform.position = Vector3.zero;
        }

        if (mainCamera != null)
        {
            mainCamera.transform.position = WideCameraPosition;
            mainCamera.orthographicSize = WideCameraSize;
        }

        playerRenderer = CreateRenderer("KimNyangi", playerPrologueStandingSheet, 30f, 2);
        rabbitRenderer = CreateRenderer("HanTorong", rabbitStandingSheet, 30f, 2);
        playerPrologueIdleFrames = CreateFrames(playerPrologueStandingSheet, 30, 30, 30f);
        playerArmedIdleFrames = CreateFrames(playerArmedStandingSheet, 30, 30, 30f);
        rabbitIdleFrames = CreateFrames(rabbitStandingSheet, 30, 30, 30f);
        playerTransform = playerRenderer.transform;
        rabbitTransform = rabbitRenderer.transform;
        playerBody = AddKinematicBody(playerRenderer.gameObject);
        rabbitBody = AddKinematicBody(rabbitRenderer.gameObject);
        playerTransform.position = new Vector3(-10.2f, -2f, 0f);
        rabbitTransform.position = new Vector3(-8.8f, -2f, 0f);

        gunRenderer = CreateRenderer("TransferredGun", gunTexture, 30f, 3);
        if (gunRenderer != null) gunRenderer.enabled = false;
        playerMugshotFirstFrame = CreateMugshotFrameTexture(playerMugshotSheet, 0);
    }

    private static Texture2D CreateMugshotFrameTexture(Texture2D sheet, int frameIndex)
    {
        if (sheet == null || !sheet.isReadable) return null;
        const int frameWidth = 45;
        Texture2D frame = new Texture2D(frameWidth, sheet.height, TextureFormat.RGBA32, false);
        frame.filterMode = FilterMode.Point;
        frame.wrapMode = TextureWrapMode.Clamp;
        frame.SetPixels(sheet.GetPixels(frameIndex * frameWidth, 0, frameWidth, sheet.height));
        frame.Apply(false, false);
        return frame;
    }

    private void CreateAudio()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        GameSfx.ApplyVolume(sfxSource, 5f);
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.volume = 0.45f;
    }

    private void PlayVoiceCharacter(char character, Speaker speaker)
    {
        if (voiceSource == null || char.IsWhiteSpace(character) || char.IsPunctuation(character)) return;
        AudioClip clip = speaker == Speaker.Player ? playerVoice : rabbitVoice;
        if (clip != null) voiceSource.PlayOneShot(clip);
    }

    private SpriteRenderer CreateRenderer(string objectName, Texture2D texture, float pixelsPerUnit, int order)
    {
        if (texture == null) return null;
        GameObject created = new GameObject(objectName);
        SpriteRenderer renderer = created.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFirstFrame(texture, pixelsPerUnit);
        renderer.sortingOrder = order;
        return renderer;
    }

    private static Sprite CreateFirstFrame(Texture2D texture, float pixelsPerUnit)
    {
        int frameHeight = texture.height;
        int frameWidth = texture.width > frameHeight * 2 ? frameHeight : texture.width;
        return CreateAnchoredSprite(texture, new Rect(0f, 0f, frameWidth, frameHeight), pixelsPerUnit);
    }

    private static Sprite[] CreateFrames(Texture2D texture, int frameWidth, int frameHeight, float pixelsPerUnit)
    {
        if (texture == null) return System.Array.Empty<Sprite>();
        int count = Mathf.Max(1, texture.width / frameWidth);
        Sprite[] frames = new Sprite[count];
        for (int index = 0; index < count; index++)
            frames[index] = CreateAnchoredSprite(texture, new Rect(index * frameWidth, 0f, frameWidth, frameHeight), pixelsPerUnit);
        return frames;
    }

    private static Sprite CreateAnchoredSprite(Texture2D texture, Rect rect, float pixelsPerUnit)
    {
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0u, SpriteMeshType.FullRect);
    }

    private void SetPlayerSprite(Texture2D sheet, int expectedFrames)
    {
        if (playerRenderer == null || sheet == null) return;
        playerArmed = sheet == playerArmedStandingSheet;
        idleFrameIndex = 0;
        idleFrameTimer = 0f;
        int frameWidth = expectedFrames > 0 ? sheet.width / expectedFrames : sheet.height;
        playerRenderer.sprite = CreateAnchoredSprite(sheet, new Rect(0f, 0f, frameWidth, sheet.height), 30f);
    }

    private static Rigidbody2D AddKinematicBody(GameObject target)
    {
        Rigidbody2D body = target.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;
        return body;
    }

    private static void SetBodyPosition(Rigidbody2D body, Transform target, Vector3 position)
    {
        if (body != null) body.position = position;
        target.position = position;
    }

    private void EnsureStyles()
    {
        if (dialogueStyle != null) return;
        narrationStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
            fontStyle = FontStyle.Normal,
            normal = { textColor = Color.white }
        };
        dialogueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 30,
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            padding = new RectOffset(18, 18, 14, 12),
            normal = { textColor = Color.white }
        };
        dialogueShadowStyle = new GUIStyle(dialogueStyle) { normal = { textColor = Color.black } };
        dialogueHighlightStyle = new GUIStyle(dialogueStyle) { normal = { textColor = new Color(0.58f, 0.82f, 1f, 0.75f) } };
        dialogueExplanationStyle = new GUIStyle(dialogueStyle) { normal = { textColor = new Color(1f, 0.843f, 0.478f) } };
        dialogueExplanationHighlightStyle = new GUIStyle(dialogueStyle) { normal = { textColor = new Color(1f, 0.93f, 0.68f, 0.5f) } };
        dialogueHintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.86f, 0.91f, 1f) }
        };
        dialogueHintShadowStyle = new GUIStyle(dialogueHintStyle) { normal = { textColor = Color.black } };
        GameTypography.ApplyDialogueFont(
            narrationStyle,
            dialogueStyle,
            dialogueShadowStyle,
            dialogueHighlightStyle,
            dialogueExplanationStyle,
            dialogueExplanationHighlightStyle,
            dialogueHintStyle,
            dialogueHintShadowStyle);
    }

    private void OnGUI()
    {
        GameTypography.ApplyToCurrentSkin();
        EnsureStyles();
        if (!string.IsNullOrEmpty(narrationText) && narrationAlpha > 0f)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, narrationAlpha);
            GUI.Label(new Rect(0f, Screen.height * 0.18f, Screen.width, 80f), narrationText, narrationStyle);
            GUI.color = previous;
        }

        if (dialogueActive) DrawDialogue();
        DrawOverlay(Color.white, flashAlpha);
        DrawOverlay(Color.black, fadeAlpha);
    }

    private void DrawDialogue()
    {
        if (string.IsNullOrEmpty(currentLine.Text)) return;
        float width = Mathf.Min(Screen.width - 36f, 560f);
        float height = 150f;
        float pixelScale = GetPixelPerfectUiScale();
        Vector2 anchor = GetSpeechScreenAnchor(currentLine.Speaker);
        float left = Mathf.Clamp(anchor.x - width * 0.5f, 18f, Screen.width - width - 18f);
        float maximumTop = Screen.height - height - 18f;
        float requestedMinimumTop = 18f + (48f * MugshotSizeRatio + MugshotBubbleGapPixels) * pixelScale;
        float top = Mathf.Clamp(anchor.y - height - 24f, Mathf.Min(requestedMinimumTop, maximumTop), maximumTop);
        Rect bubbleRect = new Rect(left, top, width, height);

        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(left + 4f, top + 4f, width, height), Texture2D.whiteTexture);
        GUI.color = currentLine.IsThought
            ? new Color(0.09f, 0.07f, 0.15f, 0.96f)
            : new Color(0.03f, 0.035f, 0.045f, 0.96f);
        GUI.DrawTexture(bubbleRect, Texture2D.whiteTexture);
        GUI.color = currentLine.IsThought
            ? new Color(0.76f, 0.68f, 1f, 1f)
            : new Color(0.84f, 0.9f, 1f, 1f);
        DrawPixelBorder(bubbleRect, 4f);
        if (!currentLine.IsThought) DrawPixelTail(anchor.x, top + height);

        GUI.color = Color.white;
        DrawMugshot(left, top, pixelScale);
        string speakerName = currentLine.Speaker == Speaker.Player ? PlayerName : RabbitName;
        string visibleText = currentLine.Text.Substring(0, Mathf.Min(visibleCharacters, currentLine.Text.Length));
        string displayedText = speakerName + " : " + visibleText;
        GUIStyle textStyle = currentLine.UseExplanationColor ? dialogueExplanationStyle : dialogueStyle;
        GUIStyle highlightStyle = currentLine.UseExplanationColor ? dialogueExplanationHighlightStyle : dialogueHighlightStyle;
        DrawPixelText(new Rect(left + 4f, top + 4f, width - 8f, height - 8f), displayedText, textStyle, dialogueShadowStyle, highlightStyle);
        DrawPixelText(new Rect(left + width - 124f, top + height - 26f, 108f, 18f), "SPACE", dialogueHintStyle, dialogueHintShadowStyle, null);
    }

    private void DrawMugshot(float bubbleLeft, float bubbleTop, float pixelScale)
    {
        Texture2D frame = currentLine.Speaker == Speaker.Player ? playerMugshotFrame : rabbitMugshotFrame;
        Texture2D face = currentLine.Speaker == Speaker.Player
            ? currentLine.UsePlayerMugshot2 ? playerMugshotSheet2 : playerMugshotSheet
            : rabbitMugshotSheet;
        bool forcePlayerFirstFrame = currentLine.Speaker == Speaker.Player &&
                                     !currentLine.UsePlayerMugshot2 &&
                                     currentLine.MugshotFrameIndex == 0 &&
                                     playerMugshotFirstFrame != null;
        if (forcePlayerFirstFrame) face = playerMugshotFirstFrame;
        if (frame == null || face == null) return;

        float portraitScale = pixelScale * MugshotSizeRatio;
        float boxSize = 48f * portraitScale;
        float bubbleGap = MugshotBubbleGapPixels * pixelScale;
        Rect boxRect = new Rect(bubbleLeft, bubbleTop - bubbleGap - boxSize, boxSize, boxSize);
        float nativeFaceHeight = currentLine.Speaker == Speaker.Player ? 30f : 36f;
        Rect faceRect = new Rect(
            boxRect.x + 1.5f * portraitScale,
            boxRect.y + (48f - nativeFaceHeight) * 0.5f * portraitScale,
            45f * portraitScale,
            nativeFaceHeight * portraitScale);
        GUI.DrawTexture(boxRect, frame, ScaleMode.StretchToFill, true);
        int frameCount = forcePlayerFirstFrame ? 1 : Mathf.Max(1, face.width / 45);
        int frameIndex = forcePlayerFirstFrame ? 0 : Mathf.Clamp(currentLine.MugshotFrameIndex, 0, frameCount - 1);
        GUI.DrawTextureWithTexCoords(
            faceRect,
            face,
            new Rect(frameIndex / (float)frameCount, 0f, 1f / frameCount, 1f),
            true);
    }

    private Vector2 GetSpeechScreenAnchor(Speaker speaker)
    {
        Transform target = speaker == Speaker.Player ? playerTransform : rabbitTransform;
        if (target == null || mainCamera == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(target.position + new Vector3(0f, 1.15f, 0f));
        return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
    }

    private static float GetPixelPerfectUiScale()
    {
        return Mathf.Max(1f, Mathf.Floor(Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight)));
    }

    private static void DrawOverlay(Color color, float alpha)
    {
        if (alpha <= 0f) return;
        GUI.color = new Color(color.r, color.g, color.b, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private static void DrawPixelBorder(Rect rect, float thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }

    private static void DrawPixelTail(float anchorX, float bubbleBottom)
    {
        const float size = 12f;
        float left = anchorX - size * 0.5f;
        GUI.DrawTexture(new Rect(left, bubbleBottom, size, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(left + 2f, bubbleBottom + 4f, size - 4f, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(left + 4f, bubbleBottom + 8f, size - 8f, 4f), Texture2D.whiteTexture);
    }

    private static void DrawPixelText(Rect rect, string text, GUIStyle style, GUIStyle shadowStyle, GUIStyle highlightStyle)
    {
        if (shadowStyle != null) GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, shadowStyle);
        if (highlightStyle != null) GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, highlightStyle);
        GUI.Label(rect, text, style);
    }
}
