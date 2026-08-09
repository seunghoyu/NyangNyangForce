using UnityEngine;

public enum DialogueVoiceProfile
{
    Player,
    Boss,
    BossNpc
}

/// <summary>
/// Plays one pre-rendered gibberish syllable for every revealed dialogue
/// character. Each profile owns an independent WAV, so changing one character
/// voice cannot alter another. Runtime pitch and tone filtering are not used.
/// </summary>
public sealed class DialogueVoiceController : MonoBehaviour
{
    private const string PlayerVoiceResourcePath = "Audio/Dialogue/dialogue_voice_player";
    private const string BossVoiceResourcePath = "Audio/Dialogue/dialogue_voice_boss";
    private const string BossNpcVoiceResourcePath = "Audio/Dialogue/dialogue_voice_boss_npc";
    private const int SourcePoolSize = 12;

    private AudioClip playerVoiceClip;
    private AudioClip bossVoiceClip;
    private AudioClip bossNpcVoiceClip;
    private AudioSource[] sources;
    private int nextSourceIndex;

    private void Awake()
    {
        playerVoiceClip = Resources.Load<AudioClip>(PlayerVoiceResourcePath);
        bossVoiceClip = Resources.Load<AudioClip>(BossVoiceResourcePath);
        bossNpcVoiceClip = Resources.Load<AudioClip>(BossNpcVoiceResourcePath);
        sources = new AudioSource[SourcePoolSize];
        for (int i = 0; i < sources.Length; i++)
        {
            GameObject voiceObject = new GameObject("DialogueVoiceSource_" + i);
            voiceObject.transform.SetParent(transform, false);
            AudioSource source = voiceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            sources[i] = source;
        }
    }

    public void PlayCharacter(char character, DialogueVoiceProfile profile)
    {
        AudioClip clip = GetVoiceClip(profile);
        if (clip == null || sources == null || char.IsWhiteSpace(character)) return;

        int sourceIndex = nextSourceIndex;
        AudioSource source = sources[sourceIndex];
        nextSourceIndex = (nextSourceIndex + 1) % sources.Length;

        source.Stop();
        source.clip = clip;
        source.pitch = 1f;
        GameSfx.ApplyVolume(source, GetVolumeScale(profile));
        source.Play();
    }

    private AudioClip GetVoiceClip(DialogueVoiceProfile profile)
    {
        switch (profile)
        {
            case DialogueVoiceProfile.Boss:
                return bossVoiceClip;
            case DialogueVoiceProfile.BossNpc:
                return bossNpcVoiceClip;
            default:
                return playerVoiceClip;
        }
    }

    private static float GetVolumeScale(DialogueVoiceProfile profile)
    {
        switch (profile)
        {
            case DialogueVoiceProfile.Boss:
                return 0.58f;
            case DialogueVoiceProfile.BossNpc:
                return 0.36f;
            default:
                return 0.42f;
        }
    }
}
