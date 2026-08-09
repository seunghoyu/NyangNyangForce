using UnityEngine;

public enum GameSfxId
{
    PlayerShot,
    PlayerDamage,
    PlayerJump,
    PlayerJumpCrash,
    PlayerDash,
    PlayerItemObtain,
    PlayerDie,
    BossPaperAttack,
    BossAttackVoice1,
    BossBookAttack,
    BossAttackVoice2,
    BossHighlighterCrush,
    BossDie,
    HardMode,
    Button
}

public static class GameSfx
{
    private const string Root = "Audio/SFX/";
    private const float SfxBusVolume = 0.3f;
    private static GameSfxPlayer player;

    public static AudioClip Load(GameSfxId id)
    {
        return Resources.Load<AudioClip>(Root + FileName(id));
    }

    public static void Play(GameSfxId id, float volumeScale = 1f)
    {
        EnsurePlayer().Play(Load(id), volumeScale);
    }

    public static void ApplyVolume(AudioSource source, float volumeScale = 1f)
    {
        if (source == null) return;
        GameSettingsData settings = GameSettingsService.Data;
        source.volume = settings.muted
            ? 0f
            : Mathf.Clamp01(SfxBusVolume * settings.sfxVolume * volumeScale);
    }

    private static GameSfxPlayer EnsurePlayer()
    {
        if (player != null) return player;
        GameObject root = new GameObject("GameSfx");
        player = root.AddComponent<GameSfxPlayer>();
        Object.DontDestroyOnLoad(root);
        return player;
    }

    private static string FileName(GameSfxId id)
    {
        switch (id)
        {
            case GameSfxId.PlayerShot: return "player_shot_sound";
            case GameSfxId.PlayerDamage: return "player_damage_sound";
            case GameSfxId.PlayerJump: return "player_jump_sound";
            case GameSfxId.PlayerJumpCrash: return "player_jumpcrash_sound";
            case GameSfxId.PlayerDash: return "player_dash_sound";
            case GameSfxId.PlayerItemObtain: return "player_itemobtain_sound";
            case GameSfxId.PlayerDie: return "player_die_sound";
            case GameSfxId.BossPaperAttack: return "boss1_paperattack_sound";
            case GameSfxId.BossAttackVoice1: return "boss1_attackvoice1_sound";
            case GameSfxId.BossBookAttack: return "boss1_bookattack_sound";
            case GameSfxId.BossAttackVoice2: return "boss1_attackvoice2_sound";
            case GameSfxId.BossHighlighterCrush: return "boss1_highlightercrush_sound";
            case GameSfxId.BossDie: return "boss1_die_sound";
            case GameSfxId.HardMode: return "hardmode_sound";
            case GameSfxId.Button: return "button_sound";
            default: return string.Empty;
        }
    }
}

public sealed class GameSfxPlayer : MonoBehaviour
{
    private AudioSource source;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        GameSfx.ApplyVolume(source);
    }

    private void Update()
    {
        GameSfx.ApplyVolume(source);
    }

    public void Play(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        GameSfx.ApplyVolume(source);
        source.PlayOneShot(clip, Mathf.Max(0f, volumeScale));
    }
}
