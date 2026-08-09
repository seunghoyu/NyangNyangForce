using System.Collections.Generic;
using UnityEngine;

// 실제 Stage1 Player 프리팹을 별도 공간에서 자동 입력으로 구동하고 전용 카메라로 촬영한다.
[DefaultExecutionOrder(-20)]
public sealed class TutorialPlayerPreview : MonoBehaviour
{
    private const float OriginX = 0f;
    private const float GroundSurfaceY = 0f;
    private const float PlayerBottomFromRoot = -0.42666665f;
    private const int PreviewLayer = 31;
    private static readonly Color PreviewBackgroundColor = new Color32(28, 31, 38, 255);

    public RenderTexture Output => output;
    public int ShootingMode => shootingPhase < 0 ? 0 : PositiveModulo(shootingPhase, 4);

    private Stage1Game game;
    private Stage1Player previewPlayer;
    private Camera previewCamera;
    private RenderTexture output;
    private GameObject previewRoot;
    private GameObject platformObject;
    private Stage1MachineGunPickup pickup;
    private int action = -1;
    private int cycleIndex = -1;
    private int shootingPhase = -1;
    private int shootingBurstShotCount;
    private float actionStartedAt;
    private float dropLandedAt = -1f;
    private bool firstPulse;
    private bool secondPulse;
    private readonly Dictionary<Camera, int> originalCameraMasks = new Dictionary<Camera, int>();
    private readonly bool[] originalLayerCollisionIgnores = new bool[32];
    private bool previewLayerIsolated;

    private void Start()
    {
        game = FindFirstObjectByType<Stage1Game>();
        if (game == null || game.player == null) return;
        BuildPreviewWorld();
        SelectAction(0);
    }

    public void SelectAction(int selectedAction)
    {
        selectedAction = Mathf.Clamp(selectedAction, 0, 7);
        if (action == selectedAction) return;
        action = selectedAction;
        actionStartedAt = Time.unscaledTime;
        cycleIndex = -1;
        shootingPhase = -1;
        shootingBurstShotCount = 0;
        dropLandedAt = -1f;
        firstPulse = false;
        secondPulse = false;
        ResetScenario();
    }

    public void EnsureRenderSize(int width, int height)
    {
        width = Mathf.Max(64, width);
        height = Mathf.Max(48, height);
        if (output != null && output.width == width && output.height == height) return;

        if (output != null)
        {
            if (previewCamera != null) previewCamera.targetTexture = null;
            output.Release();
            Destroy(output);
        }

        // URP 2D RenderGraph requires camera target textures to own a depth buffer.
        output = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = "TutorialPlayerPreview",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1
        };
        output.Create();
        if (previewCamera != null) previewCamera.targetTexture = output;
        UpdateCameraPixelScale();
    }

    private void BuildPreviewWorld()
    {
        IsolatePreviewLayer();
        previewRoot = new GameObject("TutorialPreviewWorld");
        previewRoot.transform.SetParent(transform, false);

        GameObject cameraObject = new GameObject("TutorialPreviewCamera", typeof(Camera));
        cameraObject.transform.SetParent(previewRoot.transform, false);
        previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.orthographic = true;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = PreviewBackgroundColor;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;
        previewCamera.depth = -50f;
        if (output != null)
        {
            previewCamera.targetTexture = output;
            UpdateCameraPixelScale();
        }

        CreateFloor();
        CreatePlatform();

        previewPlayer = CreatePreviewPlayer("TutorialPreviewPlayer");
    }

    private Stage1Player CreatePreviewPlayer(string objectName)
    {
        GameObject playerObject = Instantiate(game.player.gameObject, previewRoot.transform);
        playerObject.name = objectName;
        SetLayerRecursively(playerObject, PreviewLayer);
        Stage1Player player = playerObject.GetComponent<Stage1Player>();
        player.Initialize(game);
        player.EnableScriptedInput(true);
        ConfigurePreviewRenderers(playerObject, 100);
        player.SetScriptedPreviewLayer(PreviewLayer);
        MutePreviewAudio(playerObject);
        return player;
    }

    private void CreateFloor()
    {
        GameObject floor = new GameObject("TutorialPreviewFloor", typeof(BoxCollider2D), typeof(SpriteRenderer));
        floor.transform.SetParent(previewRoot.transform, false);
        floor.layer = PreviewLayer;
        floor.transform.position = new Vector3(OriginX, -0.5f, 1f);
        BoxCollider2D collider = floor.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(20f, 1f);

        SpriteRenderer renderer = floor.GetComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        renderer.color = PreviewBackgroundColor;
        renderer.sortingOrder = -10;
        floor.transform.localScale = new Vector3(20f, 1f, 1f);
    }

    private void CreatePlatform()
    {
        GameObject source = GameObject.Find("TutorialAirPlatform");
        if (source != null)
        {
            platformObject = Instantiate(source, previewRoot.transform);
            platformObject.name = "TutorialPreviewPlatform";
            platformObject.transform.position = new Vector3(OriginX, 0.72f, 0f);
            SetLayerRecursively(platformObject, PreviewLayer);
            ConfigurePreviewRenderers(platformObject, 0);
            platformObject.SetActive(false);
            return;
        }

        Texture2D texture = Resources.Load<Texture2D>("Tutorial/library_platform_left");
        platformObject = new GameObject("TutorialPreviewPlatform", typeof(SpriteRenderer));
        platformObject.transform.SetParent(previewRoot.transform, false);
        platformObject.layer = PreviewLayer;
        platformObject.transform.position = new Vector3(OriginX, 0.72f, 0f);
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            platformObject.GetComponent<SpriteRenderer>().sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
        ConfigurePreviewRenderers(platformObject, 0);
        GameObject surface = new GameObject("TopSurfaceCollider", typeof(BoxCollider2D), typeof(PlatformEffector2D));
        surface.transform.SetParent(platformObject.transform, false);
        surface.transform.localPosition = new Vector3(0f, 0.285f, 0f);
        surface.layer = PreviewLayer;
        BoxCollider2D collider = surface.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(3.78f, 0.12f);
        collider.usedByEffector = true;
        surface.GetComponent<PlatformEffector2D>().useOneWay = true;
        platformObject.SetActive(false);
    }

    private void Update()
    {
        if (previewPlayer == null) return;
        float elapsed = Time.unscaledTime - actionStartedAt;
        switch (action)
        {
            case 0: UpdateMove(elapsed); break;
            case 1: UpdateDoubleJump(elapsed); break;
            case 2: UpdateDash(elapsed); break;
            case 3: UpdateDropThrough(elapsed); break;
            case 4: UpdateSlam(elapsed); break;
            case 5: UpdateShooting(elapsed); break;
            case 6: UpdateItem(elapsed); break;
            case 7: UpdateCrouch(elapsed); break;
        }
        UpdateCameraPosition();
    }

    private void UpdateMove(float elapsed)
    {
        RestartCycle(elapsed, 2.4f);
        previewPlayer.SetScriptedHeldInput(1f, false, false, false, false, false);
    }

    private void UpdateDoubleJump(float elapsed)
    {
        float local = RestartCycle(elapsed, 3f);
        previewPlayer.SetScriptedHeldInput(0f, false, false, false, false, false);
        Pulse(ref firstPulse, local >= 0.22f, previewPlayer.PressScriptedJump);
        Pulse(ref secondPulse, local >= 0.68f, previewPlayer.PressScriptedJump);
    }

    private void UpdateDash(float elapsed)
    {
        float local = RestartCycle(elapsed, 1.55f);
        previewPlayer.SetScriptedHeldInput(0f, false, false, false, false, false);
        Pulse(ref firstPulse, local >= 0.25f, previewPlayer.PressScriptedDash);
    }

    private void UpdateDropThrough(float elapsed)
    {
        previewPlayer.SetScriptedHeldInput(0f, false, false, false, false, false);
        if (!firstPulse && elapsed >= 0.36f)
            firstPulse = previewPlayer.BeginScriptedDropThrough();

        bool landedOnFloor = firstPulse &&
                             previewPlayer.IsGrounded &&
                             previewPlayer.transform.position.y <= GroundPlayerPosition.y + 0.08f;
        if (landedOnFloor && dropLandedAt < 0f)
            dropLandedAt = Time.unscaledTime;

        if (dropLandedAt >= 0f && Time.unscaledTime - dropLandedAt >= 2f)
        {
            actionStartedAt = Time.unscaledTime;
            firstPulse = false;
            secondPulse = false;
            dropLandedAt = -1f;
            ResetScenario();
        }
    }

    private void UpdateSlam(float elapsed)
    {
        float local = RestartCycle(elapsed, 2.55f);
        bool down = local >= 0.58f && local < 1.05f;
        previewPlayer.SetScriptedHeldInput(0f, false, down, false, false, false);
        Pulse(ref firstPulse, local >= 0.18f, previewPlayer.PressScriptedJump);
        Pulse(ref secondPulse, local >= 0.62f, previewPlayer.PressScriptedDash);
    }

    private void UpdateShooting(float elapsed)
    {
        const float phaseSeconds = 1.45f;
        int phase = Mathf.FloorToInt(elapsed / phaseSeconds);
        if (phase != shootingPhase)
        {
            shootingPhase = phase;
            shootingBurstShotCount = 0;
            ClearPreviewProjectiles();
            int mode = PositiveModulo(phase, 4);
            Vector3 position = GroundPlayerPosition;
            if (mode == 3) position.x = OriginX - 1.35f;
            ResetPlayer(previewPlayer, position);
            previewPlayer.SetCutsceneFacing(1f);
            ApplyShootingInput(mode);
        }
        ApplyShootingInput(PositiveModulo(shootingPhase, 4));

        float phaseElapsed = elapsed - phase * phaseSeconds;
        if (shootingBurstShotCount < 3 && phaseElapsed >= 0.16f + shootingBurstShotCount * 0.18f)
        {
            previewPlayer.PressScriptedAttack();
            shootingBurstShotCount++;
        }
    }

    private void UpdateItem(float elapsed)
    {
        float local = RestartCycle(elapsed, 3.2f);
        float horizontal = local >= 0.48f && local < 1.45f ? 1f : 0f;
        previewPlayer.SetScriptedHeldInput(horizontal, false, false, false, false, false);
    }

    private void UpdateCrouch(float elapsed)
    {
        float local = RestartCycle(elapsed, 2.35f);
        bool crouching = local >= 0.45f && local < 1.35f;
        previewPlayer.SetScriptedHeldInput(0f, false, false, false, crouching, false);
    }

    private float RestartCycle(float elapsed, float duration)
    {
        int currentCycle = Mathf.FloorToInt(elapsed / duration);
        if (currentCycle != cycleIndex)
        {
            cycleIndex = currentCycle;
            firstPulse = false;
            secondPulse = false;
            ResetScenario();
        }
        return elapsed - currentCycle * duration;
    }

    private void ResetScenario()
    {
        if (previewPlayer == null) return;
        ClearPreviewProjectiles();
        if (pickup != null) Destroy(pickup.gameObject);
        pickup = null;
        previewPlayer.gameObject.SetActive(true);
        bool usePlatform = action == 3;
        if (platformObject != null) platformObject.SetActive(usePlatform);
        Physics2D.SyncTransforms();

        if (action == 5)
        {
            shootingPhase = -1;
            shootingBurstShotCount = 0;
            ResetPlayer(previewPlayer, GroundPlayerPosition);
            return;
        }

        Vector3 start = GroundPlayerPosition;
        if (usePlatform)
        {
            Collider2D surface = platformObject.GetComponentInChildren<Collider2D>();
            float surfaceY = surface != null ? surface.bounds.max.y : 1.065f;
            start.y = surfaceY - PlayerBottomFromRoot;
        }
        else if (action == 6)
        {
            start.x = OriginX - 1.7f;
        }
        ResetPlayer(start);
        if (usePlatform)
            previewPlayer.PrepareScriptedDropThroughPreview();

        if (action == 6 && game.machineGunPickupPrefab != null)
        {
            pickup = Instantiate(game.machineGunPickupPrefab, previewRoot.transform);
            pickup.name = "TutorialPreviewItem";
            SetLayerRecursively(pickup.gameObject, PreviewLayer);
            ConfigurePreviewRenderers(pickup.gameObject, 90);
            pickup.transform.position = new Vector3(OriginX + 0.55f, pickup.GetCenterYForSurface(GroundSurfaceY), 0f);
            pickup.Initialize(game);
            MutePreviewAudio(pickup.gameObject);
        }
    }

    private void ResetPlayer(Vector3 position)
    {
        ResetPlayer(previewPlayer, position);
    }

    private static void ResetPlayer(Stage1Player player, Vector3 position)
    {
        player.EnableScriptedInput(true);
        player.ResetScriptedPreview(position);
        SetLayerRecursively(player.gameObject, PreviewLayer);
        Physics2D.SyncTransforms();
    }

    private Vector3 GroundPlayerPosition => new Vector3(OriginX, GroundSurfaceY - PlayerBottomFromRoot, -5f);

    private void ApplyShootingInput(int mode)
    {
        bool aimDown = mode == 1;
        bool aimUp = mode == 2;
        float horizontal = mode == 3 ? 1f : 0f;
        previewPlayer.SetScriptedHeldInput(horizontal, aimUp, aimDown, false, false, false);
    }

    private void ClearPreviewProjectiles()
    {
        if (previewRoot == null) return;
        foreach (Stage1Projectile projectile in previewRoot.GetComponentsInChildren<Stage1Projectile>(true))
            if (projectile != null) Destroy(projectile.gameObject);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }

    private void UpdateCameraPosition()
    {
        if (previewCamera == null || previewPlayer == null) return;
        bool followPlayer = action == 0;
        float x = followPlayer ? previewPlayer.transform.position.x : OriginX;
        previewCamera.transform.position = new Vector3(x, 1.4f, -10f);
    }

    private void UpdateCameraPixelScale()
    {
        if (previewCamera == null || output == null) return;
        int integerScale = Mathf.Max(1, Mathf.FloorToInt(output.height / (30f * 3.8f)));
        previewCamera.orthographicSize = output.height / (2f * 30f * integerScale);
    }

    private static void Pulse(ref bool fired, bool condition, System.Action actionToRun)
    {
        if (fired || !condition) return;
        fired = true;
        actionToRun?.Invoke();
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static void ConfigurePreviewRenderers(GameObject root, int sortingOrder)
    {
        foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.sortingOrder = sortingOrder;
    }

    private void IsolatePreviewLayer()
    {
        if (previewLayerIsolated) return;
        previewLayerIsolated = true;
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera camera in cameras)
        {
            originalCameraMasks[camera] = camera.cullingMask;
            camera.cullingMask &= ~(1 << PreviewLayer);
        }
        for (int layer = 0; layer < originalLayerCollisionIgnores.Length; layer++)
        {
            originalLayerCollisionIgnores[layer] = Physics2D.GetIgnoreLayerCollision(PreviewLayer, layer);
            Physics2D.IgnoreLayerCollision(PreviewLayer, layer, layer != PreviewLayer);
        }
    }

    private static void MutePreviewAudio(GameObject root)
    {
        foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
            source.mute = true;
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<Camera, int> pair in originalCameraMasks)
            if (pair.Key != null) pair.Key.cullingMask = pair.Value;
        if (previewLayerIsolated)
        {
            for (int layer = 0; layer < originalLayerCollisionIgnores.Length; layer++)
                Physics2D.IgnoreLayerCollision(PreviewLayer, layer, originalLayerCollisionIgnores[layer]);
        }
        if (output != null)
        {
            output.Release();
            Destroy(output);
        }
        if (previewRoot != null) Destroy(previewRoot);
    }
}
