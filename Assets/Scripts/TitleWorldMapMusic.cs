using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TitleWorldMapMusic : MonoBehaviour
{
    private const string ObjectName = "TitleWorldMapMusic";
    private const string MainMusicResourcePath = "Audio/BGM/Library Labyrinth";
    private const string WorldMapMusicResourcePath = "Audio/BGM/Pixel Plug";
    private const float DefaultVolume = 0.5f;

    private static TitleWorldMapMusic instance;

    private AudioSource audioSource;

    public static void EnsurePlaying()
    {
        if (!ShouldPlayInScene(SceneManager.GetActiveScene().name)) return;

        if (instance == null)
        {
            GameObject musicObject = new GameObject(ObjectName);
            instance = musicObject.AddComponent<TitleWorldMapMusic>();
            DontDestroyOnLoad(musicObject);
        }

        instance.PlayForScene(SceneManager.GetActiveScene().name);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!ShouldPlayInScene(scene.name))
        {
            Destroy(gameObject);
            return;
        }

        PlayForScene(scene.name);
    }

    private void PlayForScene(string sceneName)
    {
        EnsureAudioListener();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = DefaultVolume * GameSettingsService.Data.musicVolume;
        }

        if (sceneName == "Loading" && audioSource.clip != null)
        {
            if (!audioSource.isPlaying) audioSource.Play();
            return;
        }

        string resourcePath = sceneName == "World Map" ? WorldMapMusicResourcePath : MainMusicResourcePath;
        AudioClip wantedClip = Resources.Load<AudioClip>(resourcePath);
        if (wantedClip != null && audioSource.clip != wantedClip)
        {
            audioSource.Stop();
            audioSource.clip = wantedClip;
        }

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private static bool ShouldPlayInScene(string sceneName)
    {
        return sceneName == "Title" ||
               sceneName == "Prologue" ||
               sceneName == "Loading" ||
               sceneName == "World Map";
    }

    private static void EnsureAudioListener()
    {
        if (FindAnyObjectByType<AudioListener>() != null) return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
            return;
        }

        GameObject listenerObject = new GameObject("AudioListener");
        listenerObject.AddComponent<AudioListener>();
    }
}
