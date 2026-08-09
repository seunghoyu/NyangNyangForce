using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public sealed class GameSettingsData
{
    public int schemaVersion = 1;
    public float masterVolume = 1f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 1f;
    public bool muted;
    public bool fullscreen = true;
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public int moveLeft = (int)Key.LeftArrow;
    public int moveRight = (int)Key.RightArrow;
    public int aimUp = (int)Key.UpArrow;
    public int aimDown = (int)Key.DownArrow;
    public int jump = (int)Key.Space;
    public int dash = (int)Key.LeftShift;
    public int crouch = (int)Key.C;
    public int attack = (int)Key.Z;
}

public static class DebugCheats
{
    public static bool Invincible { get; private set; }

    public static void ToggleInvincibility()
    {
        Invincible = !Invincible;
        Debug.Log("Debug invincibility: " + (Invincible ? "ON" : "OFF"));
    }
}

public static class GameSettingsService
{
    private const string PrefsKey = "cramming_hamster.settings.v1";
    private static GameSettingsData data;

    public static GameSettingsData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    public static float MusicMultiplier => Data.muted ? 0f : Data.masterVolume * Data.musicVolume;
    public static float SfxMultiplier => Data.muted ? 0f : Data.masterVolume * Data.sfxVolume;

    public static void Load()
    {
        data = new GameSettingsData();
        string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            try { JsonUtility.FromJsonOverwrite(json, data); }
            catch (Exception) { data = new GameSettingsData(); }
        }
        Validate();
        ApplyGlobal();
    }

    public static void Save()
    {
        Validate();
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
        ApplyGlobal();
    }

    public static void ResetToDefaults()
    {
        data = new GameSettingsData();
        Save();
    }

    public static void ApplyGlobal()
    {
        AudioListener.volume = Data.muted ? 0f : Mathf.Clamp01(Data.masterVolume);
    }

    public static void ApplyDisplay()
    {
        Screen.SetResolution(
            Mathf.Max(480, Data.resolutionWidth),
            Mathf.Max(270, Data.resolutionHeight),
            Data.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public static bool Held(int keyValue)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[(Key)keyValue].isPressed;
    }

    public static bool Pressed(int keyValue)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[(Key)keyValue].wasPressedThisFrame;
    }

    public static string KeyLabel(int keyValue) => ((Key)keyValue).ToString();

    private static void Validate()
    {
        data.masterVolume = Mathf.Clamp01(data.masterVolume);
        data.musicVolume = Mathf.Clamp01(data.musicVolume);
        data.sfxVolume = Mathf.Clamp01(data.sfxVolume);
        data.resolutionWidth = Mathf.Max(480, data.resolutionWidth);
        data.resolutionHeight = Mathf.Max(270, data.resolutionHeight);
    }
}
