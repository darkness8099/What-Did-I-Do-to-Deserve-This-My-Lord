using UnityEngine;
using UnityEngine.SceneManagement;

public enum DemoLaunchMode { MainMenu, Gameplay }

// Session navigation and the small set of settings needed by the demo shell.
// The current build keeps one gameplay scene, so Main Menu is a paused full-screen UI state.
public static class DemoGameFlow
{
    private const string VolumeKey = "demo.master_volume";
    private const string FullscreenKey = "demo.fullscreen";
    private const string ResolutionKey = "demo.resolution_index";

    private static readonly Vector2Int[] ResolutionOptions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
    };

    private static DemoLaunchMode launchMode = DemoLaunchMode.MainMenu;
    private static bool settingsLoaded;

    public static DemoLaunchMode LaunchMode => launchMode;
    public static float MasterVolume { get; private set; } = 1f;
    public static bool FullscreenEnabled { get; private set; }
    public static int ResolutionIndex { get; private set; } = 2;
    public static Vector2Int SelectedResolution => ResolutionOptions[ResolutionIndex];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeSession()
    {
        launchMode = DemoLaunchMode.MainMenu;
        settingsLoaded = false;
    }

    public static void InitializeSettings()
    {
        if (!settingsLoaded)
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            FullscreenEnabled = PlayerPrefs.GetInt(FullscreenKey, 0) != 0;
            ResolutionIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(ResolutionKey, ResolutionOptions.Length - 1),
                0,
                ResolutionOptions.Length - 1);
            settingsLoaded = true;
        }

        AudioListener.volume = MasterVolume;
        ApplyDisplaySettings();
    }

    public static void EnterGameplay()
    {
        FloatingResourcePool.Reset();
        launchMode = DemoLaunchMode.Gameplay;
        Time.timeScale = 1f;
    }

    public static void MarkMainMenu()
    {
        launchMode = DemoLaunchMode.MainMenu;
    }

    public static void ReloadGameplay()
    {
        launchMode = DemoLaunchMode.Gameplay;
        ReloadCurrentScene();
    }

    public static void ReturnToMainMenu()
    {
        launchMode = DemoLaunchMode.MainMenu;
        ReloadCurrentScene();
    }

    public static void SetMasterVolume(float value)
    {
        settingsLoaded = true;
        MasterVolume = Mathf.Clamp01(value);
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        PlayerPrefs.Save();
    }

    public static void ToggleFullscreen()
    {
        settingsLoaded = true;
        FullscreenEnabled = !FullscreenEnabled;
        PlayerPrefs.SetInt(FullscreenKey, FullscreenEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyDisplaySettings();
    }

    public static void CycleResolution()
    {
        settingsLoaded = true;
        ResolutionIndex = (ResolutionIndex + 1) % ResolutionOptions.Length;
        PlayerPrefs.SetInt(ResolutionKey, ResolutionIndex);
        PlayerPrefs.Save();
        ApplyDisplaySettings();
    }

    public static void ResetSettings()
    {
        settingsLoaded = true;
        MasterVolume = 1f;
        FullscreenEnabled = false;
        ResolutionIndex = ResolutionOptions.Length - 1;
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        PlayerPrefs.SetInt(FullscreenKey, 0);
        PlayerPrefs.SetInt(ResolutionKey, ResolutionIndex);
        PlayerPrefs.Save();
        ApplyDisplaySettings();
    }

    public static string GetResolutionLabel()
    {
        Vector2Int size = SelectedResolution;
        return $"RESOLUTION   {size.x} x {size.y}";
    }

    public static string GetFullscreenLabel()
    {
        return "FULLSCREEN   " + (FullscreenEnabled ? "ON" : "OFF");
    }

    public static void QuitApplication()
    {
        Debug.Log("[DemoGameFlow] Quit requested.");
#if !UNITY_EDITOR
        Application.Quit();
#endif
    }

    private static void ApplyDisplaySettings()
    {
        if (Application.isEditor) return;
        Vector2Int size = SelectedResolution;
        FullScreenMode mode = FullscreenEnabled
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
        Screen.SetResolution(size.x, size.y, mode);
    }

    private static void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        FloatingResourcePool.Reset();
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
            SceneManager.LoadScene(scene.buildIndex);
        else
            SceneManager.LoadScene(scene.name);
    }
}
