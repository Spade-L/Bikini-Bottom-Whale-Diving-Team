using System;
using UnityEngine;

public enum GameDisplayMode
{
    Fullscreen,
    Windowed,
    Borderless
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string MusicKey = "settings.musicVolume";
    private const string SfxKey = "settings.sfxVolume";
    private const string DisplayModeKey = "settings.displayMode";
    private const string WidthKey = "settings.windowWidth";
    private const string HeightKey = "settings.windowHeight";

    public event Action<float> MusicVolumeChanged;
    public event Action<float> SfxVolumeChanged;
    public event Action<GameDisplayMode> DisplayModeChanged;

    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }
    public GameDisplayMode DisplayMode { get; private set; }
    public int WindowWidth { get; private set; }
    public int WindowHeight { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            new GameObject("SettingsManager").AddComponent<SettingsManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        ApplyAllSettings();
    }

    private void Load()
    {
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
        DisplayMode = (GameDisplayMode)Mathf.Clamp(PlayerPrefs.GetInt(DisplayModeKey, (int)GameDisplayMode.Windowed), 0, 2);
        WindowWidth = Mathf.Max(640, PlayerPrefs.GetInt(WidthKey, 1600));
        WindowHeight = Mathf.Max(360, PlayerPrefs.GetInt(HeightKey, 1000));
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(MusicVolume, value)) return;
        MusicVolume = value;
        PlayerPrefs.SetFloat(MusicKey, value);
        PlayerPrefs.Save();
        MusicVolumeChanged?.Invoke(value);
        if (MusicManager.Instance != null) MusicManager.Instance.RefreshVolume();
    }

    public void SetSfxVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(SfxVolume, value)) return;
        SfxVolume = value;
        PlayerPrefs.SetFloat(SfxKey, value);
        PlayerPrefs.Save();
        SfxVolumeChanged?.Invoke(value);
    }

    public void SetDisplayMode(GameDisplayMode mode)
    {
        mode = (GameDisplayMode)Mathf.Clamp((int)mode, 0, 2);
        if (DisplayMode == GameDisplayMode.Windowed && Screen.width >= 640 && Screen.height >= 360)
        {
            WindowWidth = Screen.width;
            WindowHeight = Screen.height;
            PlayerPrefs.SetInt(WidthKey, WindowWidth);
            PlayerPrefs.SetInt(HeightKey, WindowHeight);
        }

        if (DisplayMode == mode) return;
        DisplayMode = mode;
        PlayerPrefs.SetInt(DisplayModeKey, (int)mode);
        ApplyDisplaySettings();
        PlayerPrefs.Save();
        DisplayModeChanged?.Invoke(mode);
    }

    public void ApplyAllSettings()
    {
        ApplyDisplaySettings();
        if (MusicManager.Instance != null) MusicManager.Instance.RefreshVolume();
    }

    public void ApplyDisplaySettings()
    {
        FullScreenMode mode = FullScreenMode.Windowed;
        if (DisplayMode == GameDisplayMode.Fullscreen) mode = FullScreenMode.ExclusiveFullScreen;
        else if (DisplayMode == GameDisplayMode.Borderless) mode = FullScreenMode.FullScreenWindow;
        Screen.SetResolution(WindowWidth, WindowHeight, mode);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
