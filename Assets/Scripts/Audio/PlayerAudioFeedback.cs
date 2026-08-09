using UnityEngine;

public sealed class PlayerAudioFeedback : MonoBehaviour
{
    private const string SfxRoot = "Audio/SFX/";

    private AudioSource runSource;
    private AudioSource gatlingSource;
    private bool runRequested;
    private bool gatlingRequested;
    private float runBlockedUntil;

    private void Awake()
    {
        runSource = CreateLoopSource("player_run_sound_loop");
        gatlingSource = CreateLoopSource("player_gatling_sound");
    }

    private void Update()
    {
        UpdateLoop(
            runSource,
            runRequested && Time.timeScale > 0f && Time.unscaledTime >= runBlockedUntil,
            0.5f);
        UpdateLoop(gatlingSource, gatlingRequested && Time.timeScale > 0f, 1f);
    }

    private void OnDisable()
    {
        StopAllLoops();
    }

    public void PlayShot() => GameSfx.Play(GameSfxId.PlayerShot);
    public void PlayJump() => GameSfx.Play(GameSfxId.PlayerJump);
    public void PlayJumpCrash() => GameSfx.Play(GameSfxId.PlayerJumpCrash, 1.5f);
    public void PlayDash()
    {
        runBlockedUntil = Time.unscaledTime + 0.18f;
        SetRunning(false);
        GameSfx.Play(GameSfxId.PlayerDash);
    }
    public void PlayItemObtain() => GameSfx.Play(GameSfxId.PlayerItemObtain);

    public void PlayDamage(bool lethal)
    {
        GameSfx.Play(lethal ? GameSfxId.PlayerDie : GameSfxId.PlayerDamage);
        if (lethal) StopAllLoops();
    }

    public void SetRunning(bool value)
    {
        runRequested = value;
        if (!value && runSource != null && runSource.isPlaying) runSource.Stop();
    }

    public void SetGatlingFiring(bool value)
    {
        gatlingRequested = value;
        if (!value && gatlingSource != null && gatlingSource.isPlaying) gatlingSource.Stop();
    }

    public void StopAllLoops()
    {
        runRequested = false;
        gatlingRequested = false;
        if (runSource != null) runSource.Stop();
        if (gatlingSource != null) gatlingSource.Stop();
    }

    private AudioSource CreateLoopSource(string resourceName)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>(SfxRoot + resourceName);
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static void UpdateLoop(AudioSource source, bool shouldPlay, float volumeScale)
    {
        if (source == null) return;
        GameSfx.ApplyVolume(source, volumeScale);
        if (shouldPlay)
        {
            if (source.clip != null && !source.isPlaying) source.Play();
        }
        else if (source.isPlaying)
        {
            source.Stop();
        }
    }
}
