using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

public static class Stage1Builder
{
    private const string Root = "Assets/Stage1";
    // Shared player animation assets use player_<state>.png naming.
    private const string PlayerRoot = "Assets/Player";
    private const string PlayerArtRoot = PlayerRoot + "/Art";
    private const string IdleArtPath = PlayerArtRoot + "/player_idle.png";
    private const string RunArtPath = PlayerArtRoot + "/player_run.png";
    private const string ShootIdleArtPath = PlayerArtRoot + "/player_shoot_idle.png";
    private const string ShootRunArtPath = PlayerArtRoot + "/player_shoot_run.png";
    private const string JumpArtPath = PlayerArtRoot + "/player_jump.png";
    private const string FallArtPath = PlayerArtRoot + "/player_fall.png";
    private const string DashArtPath = PlayerArtRoot + "/player_dash.png";
    private const string ShootIdleUpArtPath = PlayerArtRoot + "/shot_up.png";
    private const string ShootIdleDownArtPath = PlayerArtRoot + "/shot_down.png";
    private const string ShootRunUpArtPath = PlayerArtRoot + "/run_shot_up.png";
    private const string ShootRunDownArtPath = PlayerArtRoot + "/run_shot_down.png";
    private const string ShootJumpUpArtPath = PlayerArtRoot + "/jump_shot_up.png";
    private const string ShootJumpDownArtPath = PlayerArtRoot + "/jump_shot_down.png";
    private const string ShootJumpSideArtPath = PlayerArtRoot + "/jump_shot_side.png";
    private const string CrouchArtPath = PlayerArtRoot + "/crouch.png";
    private const string SlamWindupArtPath = PlayerArtRoot + "/powerfall_1.png";
    private const string SlamFallArtPath = PlayerArtRoot + "/powerfall_2.png";
    private const string SlamLandArtPath = PlayerArtRoot + "/powerfall_3.png";
    private const string BasicBulletArtPath = PlayerArtRoot + "/Projectiles/player_basic_bullet.png";
    private const string BasicBulletBurstArtPath = PlayerArtRoot + "/Projectiles/player_basic_bullet_burst.png";
    private const string GatlingBulletArtPath = PlayerArtRoot + "/Projectiles/player_gatling_bullet.png";
    private const string FxRoot = Root + "/Art/FX";
    private const string ItemArtRoot = Root + "/Art/Items";
    private const string SlamImpactFxArtPath = "Assets/Resources/Player/FX/crashdust_effect_1.png";
    private const string GatlingAppearArtPath = ItemArtRoot + "/item_gatling_appear.png";
    private const string GatlingIdleArtPath = ItemArtRoot + "/item_gatling.png";
    private const string EnvironmentRoot = Root + "/Art/Environment";
    private const string BackgroundArtPath = EnvironmentRoot + "/stage1_background.png";
    private const string FloorArtPath = EnvironmentRoot + "/library_floor.png";
    private const string LeftPlatformArtPath = EnvironmentRoot + "/library_platform_left.png";
    private const string RightPlatformArtPath = EnvironmentRoot + "/library_platform_right.png";
    private const string BossArtPath = Root + "/Art/Boss/boss1_stand.png";
    private const string BossAttackReadyArtPath = Root + "/Art/Boss/boss1_attack_ready.png";
    private const string BossAttackArtPath = Root + "/Art/Boss/boss1_attack.png";
    private const string BossBookDropReadyArtPath = Root + "/Art/Boss/boss1_attack_ready2.png";
    private const string BossBookDropAttackArtPath = Root + "/Art/Boss/boss1_attack2.png";
    private const string BossHardArtPath = Root + "/Art/Boss/boss1_hard.png";
    private const string BossDeathArtPath = Root + "/Art/Boss/boss1_die.png";
    private const string BossNpcArtPath = "Assets/Resources/Stage1/Boss/NPC/boss1_npc.png";
    private const string BossStoryRoot = "Assets/Resources/Stage1/Boss/Story";
    private const string BossStoryIdleArtPath = BossStoryRoot + "/boss1_story_1.png";
    private const string BossStoryOutburstArtPath = BossStoryRoot + "/boss1_story_2.png";
    private const string BossStoryThrowArtPath = BossStoryRoot + "/boss1_story_3.png";
    private const string BossCardHazardArtPath = Root + "/Art/Boss/Projectiles/boss_card_hazard.png";
    private const string BossBookHazardArtPath = Root + "/Art/Boss/Projectiles/boss_book_hazard.png";
    private const string BossLaserWarningArtPath = Root + "/Art/Boss/Projectiles/boss_laser_warning.png";
    private const string BossLaserBeamArtPath = Root + "/Art/Boss/Projectiles/boss_laser_beam.png";
    private const string StudyPageProjectileRoot = "Assets/Resources/Stage1/BossProjectiles/StudyPages";
    private const string BossEffectProjectileRoot = "Assets/Resources/Stage1/BossProjectiles/BossEffects";
    private const string BossEffect3Folder = BossEffectProjectileRoot + "/boss_effect_3";
    private const string BossHighlighterGroundImpactPath =
        BossEffect3Folder + "/ground_impact/boss_highlighter_ground_impact.png";
    private static readonly Vector2 BossVisibleHitboxSize = new Vector2(82f / 30f, 90f / 30f);
    private static readonly Vector2 BossVisibleHitboxOffset = new Vector2(1f / 30f, 0f);
    private const float BackgroundPixelsPerUnit = 104.5f;
    private const float AirPlatformPixelsPerUnit = 100f;
    private const float AirPlatformBehindHighlighterZ = 0.3f;
    private const int BossStandFrameCount = 4;
    private const float BossStandFrameSize = 90f;
    private const float BossStandFramesPerSecond = 4f;
    private const int BossBookDropAttackFrameCount = 4;
    private const float BossBookDropAttackFrameWidth = 751f / BossBookDropAttackFrameCount;
    private const float BossBookDropAttackFrameHeight = 207f;
    private const float BossBookDropAttackPixelsPerUnit = 30f;
    private static readonly Vector2 BossAttackReadyPivot = new Vector2(0.5f, 54.5f / 105f);
    private static readonly Vector2 BossAttackPivot = new Vector2(0.5f, 54f / 105f);
    // The boss body is drawn at a different horizontal position in each wide effect frame.
    // Keep the Transform fixed by anchoring every frame to the body's visual center.
    // Y=66 keeps the feet on the same baseline as the standing/ready sprites.
    private static readonly Vector2[] BossBookDropAttackPivots =
    {
        new Vector2(105f / BossBookDropAttackFrameWidth, 66f / BossBookDropAttackFrameHeight),
        new Vector2(67.25f / BossBookDropAttackFrameWidth, 66f / BossBookDropAttackFrameHeight),
        new Vector2(59.5f / BossBookDropAttackFrameWidth, 66f / BossBookDropAttackFrameHeight),
        new Vector2(66.75f / BossBookDropAttackFrameWidth, 66f / BossBookDropAttackFrameHeight)
    };
    private static readonly Vector2 BossHardPivot = new Vector2(0.5f, 60f / 105f);
    private static readonly Vector2[] BossDeathPivots =
    {
        new Vector2(0.5f, 45f / 120f),
        new Vector2(0.5f, 45f / 120f),
        new Vector2(0.5f, 45f / 120f),
        new Vector2(0.5f, 45f / 120f),
        new Vector2(0.5f, 45f / 120f),
        new Vector2(30f / 90f, 45f / 120f)
    };

    [MenuItem("Tools/Stage 1/Update Boss Death Animation")]
    public static void UpdateBossDeathAnimation()
    {
        ConfigureAnimationTexture(BossDeathArtPath, "boss1_die", 6, 90f, 120f, BossDeathPivots, 30f);
        ConfigureAnimationTexture(BossNpcArtPath, "boss1_npc", 1, 30f, 30f, new Vector2(16f / 30f, 0f), 30f);
        Sprite[] deathFrames = LoadSprites(BossDeathArtPath);
        string prefabPath = Root + "/Prefabs/Boss.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Stage1BossAnimation animation = prefabRoot.GetComponent<Stage1BossAnimation>();
            if (animation == null) animation = prefabRoot.AddComponent<Stage1BossAnimation>();
            animation.deathFrames = deathFrames;
            animation.deathFramesPerSecond = 8f;
            EditorUtility.SetDirty(animation);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        foreach (Stage1BossAnimation animation in Object.FindObjectsByType<Stage1BossAnimation>(FindObjectsInactive.Include))
        {
            animation.deathFrames = deathFrames;
            animation.deathFramesPerSecond = 8f;
            EditorUtility.SetDirty(animation);
        }

        EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        Debug.Log($"Boss death animation updated: frames={deathFrames.Length}");
    }

    [MenuItem("Tools/Stage 1/Update Boss Book Drop Animation")]
    public static void UpdateBossBookDropAnimation()
    {
        ConfigureAnimationTexture(BossAttackReadyArtPath, "boss1_attack_ready", 4, 90f, 105f, BossAttackReadyPivot);
        ConfigureAnimationTexture(BossAttackArtPath, "boss1_attack", 2, 90f, 105f, BossAttackPivot);
        ConfigureAnimationTexture(BossBookDropReadyArtPath, "boss1_attack_ready2", 4, 90f, 105f, BossAttackReadyPivot);
        ConfigureAnimationTexture(
            BossBookDropAttackArtPath,
            "boss1_attack2",
            BossBookDropAttackFrameCount,
            BossBookDropAttackFrameWidth,
            BossBookDropAttackFrameHeight,
            BossBookDropAttackPivots,
            BossBookDropAttackPixelsPerUnit);

        Sprite[] directReadyFrames = TakeLastSprites(LoadSprites(BossAttackReadyArtPath), 3);
        Sprite[] directAttackFrames = LoadSprites(BossAttackArtPath);
        Sprite[] readyFrames = LoadSprites(BossBookDropReadyArtPath);
        Sprite[] attackFrames = LoadSprites(BossBookDropAttackArtPath);
        string prefabPath = Root + "/Prefabs/Boss.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Stage1BossAnimation animation = prefabRoot.GetComponent<Stage1BossAnimation>();
            if (animation == null) animation = prefabRoot.AddComponent<Stage1BossAnimation>();
            animation.attackReadyFrames = directReadyFrames;
            animation.attackFrames = directAttackFrames;
            animation.bookDropReadyFrames = readyFrames;
            animation.bookDropAttackFrames = attackFrames;
            animation.attackReadyFramesPerSecond = 8f;
            animation.attackFramesPerSecond = 10f;
            EditorUtility.SetDirty(animation);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        foreach (Stage1BossAnimation animation in Object.FindObjectsByType<Stage1BossAnimation>(FindObjectsInactive.Include))
        {
            animation.attackReadyFrames = directReadyFrames;
            animation.attackFrames = directAttackFrames;
            animation.bookDropReadyFrames = readyFrames;
            animation.bookDropAttackFrames = attackFrames;
            animation.attackReadyFramesPerSecond = 8f;
            animation.attackFramesPerSecond = 10f;
            EditorUtility.SetDirty(animation);
        }

        EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        Debug.Log($"Boss book-drop animation updated: ready={readyFrames.Length}, attack={attackFrames.Length}");
    }

    [MenuItem("Tools/Stage 1/Build Stage")]
    public static void Build()
    {
        EnsureFolders();
        ConfigureAnimationTexture(IdleArtPath, "Idle", 8, 30f, new Vector2(0.5f, 14f / 30f));
        ConfigureAnimationTexture(RunArtPath, "Run", 8, 30f, new Vector2(0.5f, 14f / 30f));
        ConfigureAnimationTexture(ShootIdleArtPath, "ShootIdle", 3, 45f, new Vector2(15f / 45f, 14f / 30f));
        ConfigureAnimationTexture(ShootRunArtPath, "ShootRun", 8, 45f, new Vector2(15f / 45f, 14f / 30f));
        ConfigureAnimationTexture(JumpArtPath, "Jump", 1, 30f, new Vector2(0.5f, 14f / 30f));
        ConfigureAnimationTexture(FallArtPath, "Fall", 2, 30f, new Vector2(0.5f, 14f / 30f));
        ConfigureAnimationTexture(DashArtPath, "Dash", 5, 45f, new Vector2(15f / 45f, 14f / 30f));
        ConfigureAnimationTexture(ShootIdleUpArtPath, "ShotUp", 2, 30f, 45f, new Vector2(0.5f, 14f / 45f));
        ConfigureAnimationTexture(ShootIdleDownArtPath, "ShotDown", 2, 30f, 45f, new Vector2(0.5f, 29f / 45f));
        ConfigureAnimationTexture(ShootRunUpArtPath, "RunShotUp", 8, 30f, 45f, new Vector2(0.5f, 14f / 45f));
        ConfigureAnimationTexture(ShootRunDownArtPath, "RunShotDown", 8, 30f, 44f, new Vector2(0.5f, 28f / 44f));
        ConfigureAnimationTexture(ShootJumpUpArtPath, "JumpShotUp", 2, 30f, 45f, new Vector2(0.5f, 14f / 45f));
        ConfigureAnimationTexture(ShootJumpDownArtPath, "JumpShotDown", 2, 30f, 45f, new Vector2(0.5f, 29f / 45f));
        ConfigureAnimationTexture(ShootJumpSideArtPath, "JumpShotSide", 2, 45f, new Vector2(15f / 45f, 14f / 30f));
        ConfigureAnimationTexture(CrouchArtPath, "Crouch", 1, 30f, new Vector2(0.5f, 14f / 30f));
        ConfigureAnimationTexture(SlamWindupArtPath, "PowerfallWindup", 4, 30f, new Vector2(0.5f, 14f / 30f));
        ConfigureAnimationTexture(SlamFallArtPath, "PowerfallFall", 3, 30f, new Vector2(0.5f, 14f / 30f));
        ConfigureAnimationTexture(SlamLandArtPath, "PowerfallLand", 5, 30f, new Vector2(0.5f, 14f / 30f));
        ConfigureSlamImpactTexture(SlamImpactFxArtPath);
        ConfigureAnimationTexture(GatlingAppearArtPath, "item_gatling_appear", 3, 45f, 85f, new Vector2(0.5f, 22.5f / 85f), 30f);
        ConfigureAnimationTexture(GatlingIdleArtPath, "item_gatling", 1, 45f, 45f, new Vector2(0.5f, 0.5f), 30f);
        ConfigureAnimationTexture(GatlingBulletArtPath, "player_gatling_bullet", 2, 30f, 30f, new Vector2(0.5f, 0.5f), 30f, true);
        ConfigurePlayerTexture(BasicBulletArtPath);
        ConfigurePlayerTexture(BasicBulletBurstArtPath);
        ConfigureBossProjectileTexture(BossCardHazardArtPath);
        ConfigureBossProjectileTexture(BossBookHazardArtPath);
        ConfigureBossProjectileTexture(BossLaserWarningArtPath);
        ConfigureBossProjectileTexture(BossLaserBeamArtPath);
        ConfigureStudyPageProjectileTextures();
        ConfigureBossEffectProjectileTextures();
        ConfigureEnvironmentTexture(BackgroundArtPath, false, BackgroundPixelsPerUnit);
        ConfigureEnvironmentTexture(FloorArtPath, true, BackgroundPixelsPerUnit);
        ConfigureEnvironmentTexture(LeftPlatformArtPath, true, AirPlatformPixelsPerUnit);
        ConfigureEnvironmentTexture(RightPlatformArtPath, true, AirPlatformPixelsPerUnit);
        ConfigureAnimationTexture(BossArtPath, "boss1_stand", BossStandFrameCount, BossStandFrameSize, BossStandFrameSize, new Vector2(0.5f, 0.5f));
        ConfigureAnimationTexture(BossAttackReadyArtPath, "boss1_attack_ready", 4, 90f, 105f, BossAttackReadyPivot);
        ConfigureAnimationTexture(BossAttackArtPath, "boss1_attack", 2, 90f, 105f, BossAttackPivot);
        ConfigureAnimationTexture(BossBookDropReadyArtPath, "boss1_attack_ready2", 4, 90f, 105f, BossAttackReadyPivot);
        ConfigureAnimationTexture(
            BossBookDropAttackArtPath,
            "boss1_attack2",
            BossBookDropAttackFrameCount,
            BossBookDropAttackFrameWidth,
            BossBookDropAttackFrameHeight,
            BossBookDropAttackPivots,
            BossBookDropAttackPixelsPerUnit);
        ConfigureAnimationTexture(BossHardArtPath, "boss1_hard", 4, 90f, 105f, BossHardPivot);
        ConfigureAnimationTexture(BossDeathArtPath, "boss1_die", 6, 90f, 120f, BossDeathPivots, 30f);
        ConfigureAnimationTexture(BossStoryIdleArtPath, "boss1_story_1", 4, 90f, 105f, new Vector2(0.5f, 60f / 105f), 30f);
        ConfigureAnimationTexture(BossStoryOutburstArtPath, "boss1_story_2", 1, 90f, 100f, new Vector2(0.5f, 55f / 100f), 30f);
        ConfigureAnimationTexture(BossStoryThrowArtPath, "boss1_story_3", 5, 90f, 145f, new Vector2(0.5f, 55f / 145f), 30f);

        Sprite[] idleSprites = LoadSprites(IdleArtPath);
        Sprite[] runSprites = LoadSprites(RunArtPath);
        Sprite[] shootIdleSprites = LoadSprites(ShootIdleArtPath);
        Sprite[] shootRunSprites = LoadSprites(ShootRunArtPath);
        Sprite[] jumpSprites = LoadSprites(JumpArtPath);
        Sprite[] fallSprites = LoadSprites(FallArtPath);
        Sprite[] dashSprites = LoadSprites(DashArtPath);
        Sprite[] shootIdleUpSprites = LoadSprites(ShootIdleUpArtPath);
        Sprite[] shootIdleDownSprites = LoadSprites(ShootIdleDownArtPath);
        Sprite[] shootRunUpSprites = LoadSprites(ShootRunUpArtPath);
        Sprite[] shootRunDownSprites = LoadSprites(ShootRunDownArtPath);
        Sprite[] shootJumpUpSprites = LoadSprites(ShootJumpUpArtPath);
        Sprite[] shootJumpDownSprites = LoadSprites(ShootJumpDownArtPath);
        Sprite[] shootJumpSideSprites = LoadSprites(ShootJumpSideArtPath);
        Sprite[] crouchSprites = LoadSprites(CrouchArtPath);
        Sprite[] slamWindupSprites = LoadSprites(SlamWindupArtPath);
        Sprite[] slamFallSprites = LoadSprites(SlamFallArtPath);
        Sprite[] slamLandSprites = LoadSprites(SlamLandArtPath);
        Sprite[] slamImpactSprites = LoadSprites(SlamImpactFxArtPath);
        Sprite[] gatlingAppearSprites = LoadSprites(GatlingAppearArtPath);
        Sprite[] gatlingIdleSprites = LoadSprites(GatlingIdleArtPath);
        Sprite gatlingIdleSprite = gatlingIdleSprites.Length > 0 ? gatlingIdleSprites[0] : null;
        Sprite basicBulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BasicBulletArtPath);
        Sprite basicBulletBurstSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BasicBulletBurstArtPath);
        Sprite[] gatlingBulletSprites = LoadSprites(GatlingBulletArtPath);
        Sprite playerSprite = idleSprites.Length > 0 ? idleSprites[0] : null;
        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundArtPath);
        Sprite floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FloorArtPath);
        Sprite leftPlatformSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LeftPlatformArtPath);
        Sprite rightPlatformSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RightPlatformArtPath);
        Sprite[] bossSprites = LoadSprites(BossArtPath);
        Sprite[] bossAttackReadySprites = TakeLastSprites(LoadSprites(BossAttackReadyArtPath), 3);
        Sprite[] bossAttackSprites = LoadSprites(BossAttackArtPath);
        Sprite[] bossBookDropReadySprites = LoadSprites(BossBookDropReadyArtPath);
        Sprite[] bossBookDropAttackSprites = LoadSprites(BossBookDropAttackArtPath);
        Sprite[] bossHardSprites = LoadSprites(BossHardArtPath);
        Sprite[] bossDeathSprites = LoadSprites(BossDeathArtPath);
        Sprite[] bossNpcSprites = LoadSprites(BossNpcArtPath);
        Sprite bossSprite = bossSprites.Length > 0 ? bossSprites[0] : null;
        Sprite bossCardHazardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossCardHazardArtPath);
        Sprite bossBookHazardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossBookHazardArtPath);
        Sprite bossLaserWarningSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossLaserWarningArtPath);
        Sprite bossLaserBeamSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossLaserBeamArtPath);
        Sprite square = CreateSquareSprite();

        CreateSlamImpactEffectPrefab(slamImpactSprites);
        GameObject slamImpactEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/SlamImpactEffect.prefab");
        CreatePlayerPrefab(
            playerSprite,
            idleSprites,
            runSprites,
            shootIdleSprites,
            shootRunSprites,
            jumpSprites,
            fallSprites,
            dashSprites,
            shootIdleUpSprites,
            shootIdleDownSprites,
            shootRunUpSprites,
            shootRunDownSprites,
            shootJumpUpSprites,
            shootJumpDownSprites,
            shootJumpSideSprites,
            crouchSprites,
            slamWindupSprites,
            slamFallSprites,
            slamLandSprites,
            slamImpactEffectPrefab);
        CreateProjectilePrefab("BasicPurificationShot", basicBulletSprite, Color.white, Vector2.one, new Vector2(14f / 30f, 5f / 30f));
        CreateProjectilePrefab(
            "PurificationShot",
            gatlingBulletSprites.Length > 0 ? gatlingBulletSprites[0] : square,
            Color.white,
            Vector2.one,
            new Vector2(29f / 30f, 5f / 30f),
            gatlingBulletSprites);
        CreateProjectilePrefab("Hazard", square, new Color(1f, 0.25f, 0.18f), new Vector2(0.32f, 0.32f), Vector2.one);
        CreateMachineGunPickupPrefab(gatlingAppearSprites, gatlingIdleSprite != null ? gatlingIdleSprite : square);
        CreateBossPrefab(
            bossSprite != null ? bossSprite : square,
            bossCardHazardSprite,
            bossBookHazardSprite,
            bossLaserWarningSprite,
            bossLaserBeamSprite,
            basicBulletBurstSprite,
            bossSprites,
            bossHardSprites,
            bossAttackReadySprites,
            bossAttackSprites,
            bossBookDropReadySprites,
            bossBookDropAttackSprites,
            bossDeathSprites,
            bossNpcSprites.Length > 0 ? bossNpcSprites[0] : null);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Player.prefab");
        GameObject basicPlayerShotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/BasicPurificationShot.prefab");
        GameObject machineGunShotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/PurificationShot.prefab");
        GameObject hazardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Hazard.prefab");
        GameObject machineGunPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/MachineGunPickup.prefab");
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Boss.prefab");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Stage 1";

        CreateCamera();
        CreateArena(backgroundSprite, floorSprite, leftPlatformSprite, rightPlatformSprite);

        GameObject playerObject = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerObject.transform.position = new Vector3(-5.5f, -3.02f, 0f);
        playerObject.name = "Player";
        Stage1Player player = playerObject.GetComponent<Stage1Player>();
        GameObject bossObject = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
        bossObject.transform.position = new Vector3(0f, 0.65f, 0f);
        bossObject.name = "Boss";
        Stage1Boss boss = bossObject.GetComponent<Stage1Boss>();

        GameObject managerObject = new GameObject("Stage1Game");
        Stage1Game game = managerObject.AddComponent<Stage1Game>();
        game.player = player;
        game.boss = boss;
        game.basicPlayerShotPrefab = basicPlayerShotPrefab.GetComponent<Stage1Projectile>();
        game.machineGunShotPrefab = machineGunShotPrefab.GetComponent<Stage1Projectile>();
        game.hazardPrefab = hazardPrefab.GetComponent<Stage1Projectile>();
        game.machineGunPickupPrefab = machineGunPickupPrefab.GetComponent<Stage1MachineGunPickup>();

        string scenePath = "Assets/Scenes/Stage 1.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Loading.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Prologue.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Tutorial.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/World Map.unity", true),
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = buildScenes.ToArray();

        PlayerSettings.defaultScreenWidth = 480;
        PlayerSettings.defaultScreenHeight = 270;
        PlayerSettings.resizableWindow = false;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = managerObject;
        Debug.Log("[Stage1Builder] Stage 1 scene and prefabs built successfully.");
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "Stage1");
        CreateFolder(Root, "Art");
        CreateFolder(Root + "/Art", "FX");
        CreateFolder(Root + "/Art", "Environment");
        CreateFolder(Root + "/Art", "Items");
        CreateFolder(Root, "Prefabs");
        CreateFolder(Root, "Generated");
        CreateFolder("Assets", "Player");
        CreateFolder(PlayerRoot, "Art");
        CreateFolder(PlayerArtRoot, "Projectiles");
    }

    private static void CreateFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }

    private static void ConfigurePlayerTexture(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 30f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void ConfigureBossEffectProjectileTextures()
    {
        if (!AssetDatabase.IsValidFolder(BossEffectProjectileRoot)) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { BossEffectProjectileRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool isBossEffect3 = path.Contains("/boss_effect_3/");
            bool isBossEffect4 = path.Contains("/boss_effect_4/");
            bool isBossEffect4BookOut = path.Contains("/boss_effect_4/book_out/");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = isBossEffect4BookOut
                ? SpriteImportMode.Multiple
                : isBossEffect4 ? SpriteImportMode.Single : SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = isBossEffect3 ? 100f : 15f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.SaveAndReimport();

            if (isBossEffect3)
            {
                SliceBossEffect3Texture(importer, path);
            }
            else if (isBossEffect4BookOut)
            {
                SliceBossEffect4BookOutTexture(importer, path);
            }
        }
    }

    private static void SliceBossEffect3Texture(TextureImporter importer, string path)
    {
        bool isGroundImpact = path == BossHighlighterGroundImpactPath;
        int frameCount = isGroundImpact
            ? 3
            : path == BossEffect3Folder + "/boss_effect_3_2.png" ? 5 : 1;
        float frameWidth = isGroundImpact ? 135f : 75f;
        float frameHeight = isGroundImpact ? 60f : 200f;
        string frameName = Path.GetFileNameWithoutExtension(path);

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] sprites = new SpriteRect[frameCount];
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i] = new SpriteRect
            {
                name = frameName + "_" + i,
                rect = new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                alignment = SpriteAlignment.Custom,
                // 충격파는 바닥 Y에 바로 배치할 수 있도록 하단 중앙을 기준점으로 사용한다.
                pivot = isGroundImpact ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            };
        }

        dataProvider.SetSpriteRects(sprites);
        dataProvider.Apply();
        AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void SliceBossEffect4BookOutTexture(TextureImporter importer, string path)
    {
        const int frameCount = 3;
        const float frameSize = 30f;
        string frameName = Path.GetFileNameWithoutExtension(path);

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] sprites = new SpriteRect[frameCount];
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i] = new SpriteRect
            {
                name = frameName + "_" + i,
                rect = new Rect(i * frameSize, 0f, frameSize, frameSize),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            };
        }

        dataProvider.SetSpriteRects(sprites);
        dataProvider.Apply();
        AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void ConfigureBossProjectileTexture(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void ConfigureStudyPageProjectileTextures()
    {
        if (!AssetDatabase.IsValidFolder(StudyPageProjectileRoot)) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { StudyPageProjectileRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 30f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureAnimationTexture(
        string path,
        string frameName,
        int frameCount,
        float frameWidth,
        Vector2 pivot)
    {
        ConfigureAnimationTexture(path, frameName, frameCount, frameWidth, 30f, pivot);
    }

    private static void ConfigureAnimationTexture(
        string path,
        string frameName,
        int frameCount,
        float frameWidth,
        float frameHeight,
        Vector2 pivot)
    {
        ConfigureAnimationTexture(path, frameName, frameCount, frameWidth, frameHeight, pivot, 30f);
    }

    private static void ConfigureAnimationTexture(
        string path,
        string frameName,
        int frameCount,
        float frameWidth,
        float frameHeight,
        Vector2 pivot,
        float pixelsPerUnit)
    {
        ConfigureAnimationTexture(path, frameName, frameCount, frameWidth, frameHeight, pivot, pixelsPerUnit, false);
    }

    private static void ConfigureAnimationTexture(
        string path,
        string frameName,
        int frameCount,
        float frameWidth,
        float frameHeight,
        Vector2[] framePivots,
        float pixelsPerUnit)
    {
        ConfigureAnimationTexture(
            path,
            frameName,
            frameCount,
            frameWidth,
            frameHeight,
            Vector2.zero,
            pixelsPerUnit,
            false,
            framePivots);
    }

    private static void ConfigureAnimationTexture(
        string path,
        string frameName,
        int frameCount,
        float frameWidth,
        float frameHeight,
        Vector2 pivot,
        float pixelsPerUnit,
        bool isReadable,
        Vector2[] framePivots = null)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = isReadable;

        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] sprites = new SpriteRect[frameCount];
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i] = new SpriteRect
            {
                name = frameName + "_" + i,
                rect = new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                alignment = SpriteAlignment.Custom,
                pivot = framePivots != null && i < framePivots.Length ? framePivots[i] : pivot,
                spriteID = GUID.Generate()
            };
        }
        dataProvider.SetSpriteRects(sprites);
        dataProvider.Apply();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static void ConfigureSlamImpactTexture(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 30f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        Rect[] frameRects =
        {
            new Rect(0f, 0f, 46f, 45f),
            new Rect(46f, 0f, 55f, 45f),
            new Rect(101f, 0f, 103f, 45f),
            new Rect(204f, 0f, 103f, 45f),
            new Rect(307f, 0f, 83f, 45f)
        };
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] sprites = new SpriteRect[frameRects.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i] = new SpriteRect
            {
                name = "CrashDust_" + i,
                rect = frameRects[i],
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 5f / 45f),
                spriteID = GUID.Generate()
            };
        }
        dataProvider.SetSpriteRects(sprites);
        dataProvider.Apply();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static void ConfigureEnvironmentTexture(string path, bool alphaIsTransparency, float pixelsPerUnit = 30f)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = alphaIsTransparency;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadSprites(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite) sprites.Add(sprite);
        }
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }

    private static Sprite[] TakeLastSprites(Sprite[] sprites, int count)
    {
        if (sprites == null || sprites.Length <= count) return sprites;
        Sprite[] result = new Sprite[count];
        System.Array.Copy(sprites, sprites.Length - count, result, 0, count);
        return result;
    }

    private static Sprite CreateSquareSprite()
    {
        string path = Root + "/Generated/Square.asset";
        AssetDatabase.DeleteAsset(path);

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "SquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sprite.name = "Square";
        AssetDatabase.CreateAsset(texture, path);
        AssetDatabase.AddObjectToAsset(sprite, path);
        AssetDatabase.SaveAssets();
        return sprite;
    }

    private static void CreatePlayerPrefab(
        Sprite sprite,
        Sprite[] idleSprites,
        Sprite[] runSprites,
        Sprite[] shootIdleSprites,
        Sprite[] shootRunSprites,
        Sprite[] jumpSprites,
        Sprite[] fallSprites,
        Sprite[] dashSprites,
        Sprite[] shootIdleUpSprites,
        Sprite[] shootIdleDownSprites,
        Sprite[] shootRunUpSprites,
        Sprite[] shootRunDownSprites,
        Sprite[] shootJumpUpSprites,
        Sprite[] shootJumpDownSprites,
        Sprite[] shootJumpSideSprites,
        Sprite[] crouchSprites,
        Sprite[] slamWindupSprites,
        Sprite[] slamFallSprites,
        Sprite[] slamLandSprites,
        GameObject slamImpactEffectPrefab)
    {
        GameObject root = new GameObject("Player");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 5;

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.72f, 0.92f);
        Stage1Player player = root.AddComponent<Stage1Player>();
        player.slamImpactEffectPrefab = slamImpactEffectPrefab;

        GameObject hurtboxObject = new GameObject("Hurtbox");
        hurtboxObject.transform.SetParent(root.transform, false);
        hurtboxObject.transform.localPosition = new Vector3(0f, -0.0167f, 0f);
        BoxCollider2D hurtboxCollider = hurtboxObject.AddComponent<BoxCollider2D>();
        hurtboxCollider.size = new Vector2(0.6f, 0.9667f);
        hurtboxCollider.isTrigger = true;
        hurtboxObject.AddComponent<Stage1PlayerHurtbox>();

        Stage1PlayerAnimation animation = root.AddComponent<Stage1PlayerAnimation>();
        animation.idleFrames = idleSprites;
        animation.runFrames = runSprites;
        animation.shootIdleFrames = shootIdleSprites;
        animation.shootRunFrames = shootRunSprites;
        animation.jumpFrames = jumpSprites;
        animation.fallFrames = fallSprites;
        animation.dashFrames = dashSprites;
        animation.shootIdleUpFrames = shootIdleUpSprites;
        animation.shootIdleDownFrames = shootIdleDownSprites;
        animation.shootRunUpFrames = shootRunUpSprites;
        animation.shootRunDownFrames = shootRunDownSprites;
        animation.shootJumpUpFrames = shootJumpUpSprites;
        animation.shootJumpDownFrames = shootJumpDownSprites;
        animation.shootJumpSideFrames = shootJumpSideSprites;
        animation.crouchFrames = crouchSprites;
        animation.slamWindupFrames = slamWindupSprites;
        animation.slamFallFrames = slamFallSprites;
        animation.slamLandFrames = slamLandSprites;
        animation.idleFramesPerSecond = 8f;
        animation.runFramesPerSecond = 10f;
        animation.shootFramesPerSecond = 12f;

        string path = Root + "/Prefabs/Player.prefab";
        SavePrefab(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateSlamImpactEffectPrefab(Sprite[] frames)
    {
        GameObject root = new GameObject("SlamImpactEffect");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = frames.Length > 0 ? frames[0] : null;
        renderer.sortingOrder = 6;

        SlamImpactEffect effect = root.AddComponent<SlamImpactEffect>();
        effect.frames = frames;
        effect.framesPerSecond = 20f;

        string path = Root + "/Prefabs/SlamImpactEffect.prefab";
        SavePrefab(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateProjectilePrefab(
        string name,
        Sprite sprite,
        Color color,
        Vector2 visualScale,
        Vector2 colliderSize,
        Sprite[] visualVariants = null)
    {
        GameObject root = new GameObject(name);
        root.SetActive(false);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 6;
        root.transform.localScale = new Vector3(visualScale.x, visualScale.y, 1f);

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = colliderSize;
        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        Stage1Projectile projectile = root.AddComponent<Stage1Projectile>();
        projectile.visualVariants = visualVariants;

        string path = Root + "/Prefabs/" + name + ".prefab";
        SavePrefab(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateBossPrefab(
        Sprite sprite,
        Sprite cardHazardSprite,
        Sprite bookHazardSprite,
        Sprite laserWarningSprite,
        Sprite laserBeamSprite,
        Sprite hitBurstSprite,
        Sprite[] standFrames,
        Sprite[] hardFrames,
        Sprite[] attackReadyFrames,
        Sprite[] attackFrames,
        Sprite[] bookDropReadyFrames,
        Sprite[] bookDropAttackFrames,
        Sprite[] deathFrames,
        Sprite npcFrame)
    {
        GameObject root = new GameObject("Boss");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 2;
        root.transform.localScale = Vector3.one;
        BoxCollider2D bodyCollider = root.AddComponent<BoxCollider2D>();
        bodyCollider.isTrigger = true;
        if (sprite != null)
        {
            bodyCollider.size = BossVisibleHitboxSize;
            bodyCollider.offset = BossVisibleHitboxOffset;
        }
        Stage1Boss boss = root.AddComponent<Stage1Boss>();
        boss.cardHazardSprite = cardHazardSprite;
        boss.bookHazardSprite = bookHazardSprite;
        boss.laserWarningSprite = laserWarningSprite;
        boss.laserBeamSprite = laserBeamSprite;
        boss.hitBurstSprite = hitBurstSprite;
        Stage1BossAnimation animation = root.AddComponent<Stage1BossAnimation>();
        animation.standFrames = standFrames;
        animation.hardFrames = hardFrames;
        animation.attackReadyFrames = attackReadyFrames;
        animation.attackFrames = attackFrames;
        animation.bookDropReadyFrames = bookDropReadyFrames;
        animation.bookDropAttackFrames = bookDropAttackFrames;
        animation.deathFrames = deathFrames;
        animation.npcFrame = npcFrame;
        animation.framesPerSecond = BossStandFramesPerSecond;
        root.AddComponent<Stage1HitShake>();

        string path = Root + "/Prefabs/Boss.prefab";
        SavePrefab(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateMachineGunPickupPrefab(Sprite[] appearFrames, Sprite idleSprite)
    {
        GameObject root = new GameObject("MachineGunPickup");
        root.SetActive(false);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = idleSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 6;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        // Match the 41x39 visible-pixel bounds of the 45x45 idle sprite at 30 PPU.
        collider.size = new Vector2(41f / 30f, 39f / 30f);
        collider.offset = new Vector2(0f, -3f / 30f);
        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        Stage1MachineGunPickup pickup = root.AddComponent<Stage1MachineGunPickup>();
        pickup.appearFrames = appearFrames;
        pickup.idleSprite = idleSprite;

        string path = Root + "/Prefabs/MachineGunPickup.prefab";
        SavePrefab(root, path);
        Object.DestroyImmediate(root);
    }

    private static void SavePrefab(GameObject source, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(source, path);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        PixelPerfectCamera pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
        pixelPerfect.assetsPPU = 30;
        pixelPerfect.refResolutionX = 480;
        pixelPerfect.refResolutionY = 270;
        // Keep the authored 480x270 stage framing at non-integer WebGL viewport sizes.
        // Without StretchFill, PixelPerfectCamera exposes extra world space around the
        // 16:9 background whenever the browser viewport is between integer scales.
        pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
        pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
    }

    private static void CreateArena(
        Sprite backgroundSprite,
        Sprite floorSprite,
        Sprite leftPlatformSprite,
        Sprite rightPlatformSprite)
    {
        CreateEnvironmentVisual("Stage1Background", backgroundSprite, -10);

        GameObject floor = CreateEnvironmentVisual("Floor", floorSprite, 1);
        BoxCollider2D floorCollider = floor.AddComponent<BoxCollider2D>();
        // The collider top (-3.41) crosses the middle of the brightest gray
        // strip in boss1_floor.png, so the player's feet rest on that line.
        floorCollider.offset = new Vector2(0f, -3.91f);
        floorCollider.size = new Vector2(16f, 1f);

        CreatePlatform(
            "PlatformLeft",
            leftPlatformSprite,
            new Vector2(-5.4167f, -1.425f),
            new Vector2(-5.4167f, -1.14f));
        CreatePlatform(
            "PlatformRight",
            rightPlatformSprite,
            new Vector2(5.4833f, -1.425f),
            new Vector2(5.4833f, -1.14f));
    }

    private static GameObject CreateEnvironmentVisual(string name, Sprite sprite, int sortingOrder)
    {
        GameObject root = new GameObject(name);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return root;
    }

    private static void CreatePlatform(
        string name,
        Sprite sprite,
        Vector2 visualPosition,
        Vector2 colliderPosition)
    {
        GameObject platform = CreateEnvironmentVisual(name, sprite, 2);
        platform.transform.position = new Vector3(
            visualPosition.x,
            visualPosition.y,
            AirPlatformBehindHighlighterZ);

        // Keep collision separate from the artwork so no legacy platform
        // collider remains underneath the new gray walking surface.
        GameObject surface = new GameObject("TopSurfaceCollider");
        surface.transform.SetParent(platform.transform, false);
        surface.transform.position = colliderPosition;
        BoxCollider2D collider = surface.AddComponent<BoxCollider2D>();
        // 3.78 units follows the flat gray top from roughly x=14..392 px.
        // With a 0.12 height, center Y + 0.06 aligns the top edge to y=5 px.
        collider.size = new Vector2(3.78f, 0.12f);
        PlatformEffector2D effector = surface.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        collider.usedByEffector = true;
    }
}
