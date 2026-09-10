using System;
using UnityEngine;

// 定义 GameDisplayMode 类型
public enum GameDisplayMode
{
    Fullscreen,
// 更新当前逻辑
    Windowed,
    Borderless
}

// 定义 SettingsManager 类型
public class SettingsManager : MonoBehaviour
{
// 更新当前逻辑
    public static SettingsManager Instance { get; private set; }

// 更新当前逻辑
    private const string MusicKey = "settings.musicVolume";
    private const string SfxKey = "settings.sfxVolume";
// 更新当前逻辑
    private const string DisplayModeKey = "settings.displayMode";
    private const string WidthKey = "settings.windowWidth";
// 更新当前逻辑
    private const string HeightKey = "settings.windowHeight";

// 更新当前逻辑
    public event Action<float> MusicVolumeChanged;
    public event Action<float> SfxVolumeChanged;
// 更新当前逻辑
    public event Action<GameDisplayMode> DisplayModeChanged;

// 更新当前逻辑
    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }
// 更新当前逻辑
    public GameDisplayMode DisplayMode { get; private set; }
    public int WindowWidth { get; private set; }
// 更新当前逻辑
    public int WindowHeight { get; private set; }

// 运行前初始化状态
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// 空引用时直接退出
        if (Instance == null)
        {
// 添加所需组件
            new GameObject("SettingsManager").AddComponent<SettingsManager>();
        }
    }

// 定义 Awake 方法
    private void Awake()
    {
// 判断当前条件
        if (Instance != null && Instance != this)
        {
// 清理当前对象
            Destroy(gameObject);
            return;
        }

// 更新当前状态
        Instance = this;
        DontDestroyOnLoad(gameObject);
// 执行 Load
        Load();
        ApplyAllSettings();
    }

// 定义 Load 方法
    private void Load()
    {
// 保存本地配置
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
// 保存本地配置
        DisplayMode = (GameDisplayMode)Mathf.Clamp(PlayerPrefs.GetInt(DisplayModeKey, (int)GameDisplayMode.Windowed), 0, 2);
        WindowWidth = Mathf.Max(640, PlayerPrefs.GetInt(WidthKey, 1600));
// 保存本地配置
        WindowHeight = Mathf.Max(360, PlayerPrefs.GetInt(HeightKey, 1000));
    }

// 定义 SetMusicVolume 方法
    public void SetMusicVolume(float value)
    {
// 更新当前状态
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(MusicVolume, value)) return;
// 更新当前状态
        MusicVolume = value;
        PlayerPrefs.SetFloat(MusicKey, value);
// 保存本地配置
        PlayerPrefs.Save();
        MusicVolumeChanged?.Invoke(value);
// 判断当前条件
        if (MusicManager.Instance != null) MusicManager.Instance.RefreshVolume();
    }

// 定义 SetSfxVolume 方法
    public void SetSfxVolume(float value)
    {
// 更新当前状态
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(SfxVolume, value)) return;
// 更新当前状态
        SfxVolume = value;
        PlayerPrefs.SetFloat(SfxKey, value);
// 保存本地配置
        PlayerPrefs.Save();
        SfxVolumeChanged?.Invoke(value);
    }

// 定义 SetDisplayMode 方法
    public void SetDisplayMode(GameDisplayMode mode)
    {
// 更新当前状态
        mode = (GameDisplayMode)Mathf.Clamp((int)mode, 0, 2);
        if (DisplayMode == GameDisplayMode.Windowed && Screen.width >= 640 && Screen.height >= 360)
        {
// 更新当前状态
            WindowWidth = Screen.width;
            WindowHeight = Screen.height;
// 保存本地配置
            PlayerPrefs.SetInt(WidthKey, WindowWidth);
            PlayerPrefs.SetInt(HeightKey, WindowHeight);
        }

// 判断当前条件
        if (DisplayMode == mode) return;
        DisplayMode = mode;
// 保存本地配置
        PlayerPrefs.SetInt(DisplayModeKey, (int)mode);
        ApplyDisplaySettings();
// 保存本地配置
        PlayerPrefs.Save();
        DisplayModeChanged?.Invoke(mode);
    }

// 定义 ApplyAllSettings 方法
    public void ApplyAllSettings()
    {
// 执行 ApplyDisplaySettings
        ApplyDisplaySettings();
        if (MusicManager.Instance != null) MusicManager.Instance.RefreshVolume();
    }

// 定义 ApplyDisplaySettings 方法
    public void ApplyDisplaySettings()
    {
// 保存 mode 数据
        FullScreenMode mode = FullScreenMode.Windowed;
        if (DisplayMode == GameDisplayMode.Fullscreen) mode = FullScreenMode.ExclusiveFullScreen;
// 检查其他条件
        else if (DisplayMode == GameDisplayMode.Borderless) mode = FullScreenMode.FullScreenWindow;
        Screen.SetResolution(WindowWidth, WindowHeight, mode);
    }

// 定义 OnApplicationPause 方法
    private void OnApplicationPause(bool pause)
    {
// 判断当前条件
        if (pause) PlayerPrefs.Save();
    }

// 定义 OnApplicationQuit 方法
    private void OnApplicationQuit()
    {
// 保存本地配置
        PlayerPrefs.Save();
    }
}
