using System;
using UnityEngine;

// 枚举支持的窗口显示模式
public enum GameDisplayMode
{
    Fullscreen,
// 推进 当前脚本 的当前步骤
    Windowed,
    Borderless
}

// 持久化并应用音乐、音效和显示设置
public class SettingsManager : MonoBehaviour
{
// 推进 SettingsManager 的当前步骤
    public static SettingsManager Instance { get; private set; }

// 推进 SettingsManager 的当前步骤（SettingsManager）
    private const string MusicKey = "settings.musicVolume";
    private const string SfxKey = "settings.sfxVolume";
// 推进 SettingsManager 的当前步骤（SettingsManager）（private）
    private const string DisplayModeKey = "settings.displayMode";
    private const string WidthKey = "settings.windowWidth";
// 在 SettingsManager 中继续当前处理
    private const string HeightKey = "settings.windowHeight";

// 推进 SettingsManager 的当前步骤（SettingsManager）（public）
    public event Action<float> MusicVolumeChanged;
    public event Action<float> SfxVolumeChanged;
// 在 SettingsManager 中继续当前处理（SettingsManager 后续步骤）
    public event Action<GameDisplayMode> DisplayModeChanged;

// 在 SettingsManager 中继续当前处理（SettingsManager 后续步骤）（33）
    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }
// 在 SettingsManager 中继续当前处理（SettingsManager 后续步骤）（36）
    public GameDisplayMode DisplayMode { get; private set; }
    public int WindowWidth { get; private set; }
// 在 SettingsManager 中继续当前处理（SettingsManager 后续步骤）（39）
    public int WindowHeight { get; private set; }

// 在场景加载前创建常驻管理器
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// AutoCreate 缺少引用时提前结束
        if (Instance == null)
        {
// 添加所需组件
            new GameObject("SettingsManager").AddComponent<SettingsManager>();
        }
    }

// 初始化组件引用和运行状态
    private void Awake()
    {
// 检查 Awake 的前置条件
        if (Instance != null && Instance != this)
        {
// 清理当前对象
            Destroy(gameObject);
            return;
        }

// 同步 Awake 的状态
        Instance = this;
        DontDestroyOnLoad(gameObject);
// 推进 Awake 中的必要步骤
        Load();
        ApplyAllSettings();
    }

// 加载 Load 对应数据
    private void Load()
    {
// 保存本地配置
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
// 在 Load 中继续当前处理
        DisplayMode = (GameDisplayMode)Mathf.Clamp(PlayerPrefs.GetInt(DisplayModeKey, (int)GameDisplayMode.Windowed), 0, 2);
        WindowWidth = Mathf.Max(640, PlayerPrefs.GetInt(WidthKey, 1600));
// 在 Load 中继续当前处理（Load 后续步骤）
        WindowHeight = Mathf.Max(360, PlayerPrefs.GetInt(HeightKey, 1000));
    }

// 设置 SetMusicVolume 的目标状态
    public void SetMusicVolume(float value)
    {
// 同步 SetMusicVolume 的内部状态
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(MusicVolume, value)) return;
// 同步 SetMusicVolume 的内部状态（SetMusicVolume）
        MusicVolume = value;
        PlayerPrefs.SetFloat(MusicKey, value);
// 在 SetMusicVolume 中继续当前处理
        PlayerPrefs.Save();
        MusicVolumeChanged?.Invoke(value);
// 检查 SetMusicVolume 的前置条件
        if (MusicManager.Instance != null) MusicManager.Instance.RefreshVolume();
    }

// 设置 SetSfxVolume 的目标状态
    public void SetSfxVolume(float value)
    {
// 同步 SetSfxVolume 的内部状态
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(SfxVolume, value)) return;
// 同步 SetSfxVolume 的内部状态（SetSfxVolume）
        SfxVolume = value;
        PlayerPrefs.SetFloat(SfxKey, value);
// 在 SetSfxVolume 中继续当前处理
        PlayerPrefs.Save();
        SfxVolumeChanged?.Invoke(value);
    }

// 设置 SetDisplayMode 的目标状态
    public void SetDisplayMode(GameDisplayMode mode)
    {
// 同步 SetDisplayMode 的内部状态
        mode = (GameDisplayMode)Mathf.Clamp((int)mode, 0, 2);
        if (DisplayMode == GameDisplayMode.Windowed && Screen.width >= 640 && Screen.height >= 360)
        {
// 同步 SetDisplayMode 的内部状态（SetDisplayMode）
            WindowWidth = Screen.width;
            WindowHeight = Screen.height;
// 在 SetDisplayMode 中继续当前处理
            PlayerPrefs.SetInt(WidthKey, WindowWidth);
            PlayerPrefs.SetInt(HeightKey, WindowHeight);
        }

// 检查 SetDisplayMode 的前置条件
        if (DisplayMode == mode) return;
        DisplayMode = mode;
// 在 SetDisplayMode 中继续当前处理（SetDisplayMode 后续步骤）
        PlayerPrefs.SetInt(DisplayModeKey, (int)mode);
        ApplyDisplaySettings();
// 在 SetDisplayMode 中继续当前处理（SetDisplayMode 后续步骤）（137）
        PlayerPrefs.Save();
        DisplayModeChanged?.Invoke(mode);
    }

// 应用 ApplyAllSettings 对应设置
    public void ApplyAllSettings()
    {
// 推进 ApplyAllSettings 中的必要步骤
        ApplyDisplaySettings();
        if (MusicManager.Instance != null) MusicManager.Instance.RefreshVolume();
    }

// 应用 ApplyDisplaySettings 对应设置
    public void ApplyDisplaySettings()
    {
// 同步 ApplyDisplaySettings 的相关数据
        FullScreenMode mode = FullScreenMode.Windowed;
        if (DisplayMode == GameDisplayMode.Fullscreen) mode = FullScreenMode.ExclusiveFullScreen;
// 检查其他条件
        else if (DisplayMode == GameDisplayMode.Borderless) mode = FullScreenMode.FullScreenWindow;
        Screen.SetResolution(WindowWidth, WindowHeight, mode);
    }

// 响应 OnApplicationPause 生命周期
    private void OnApplicationPause(bool pause)
    {
// 检查 OnApplicationPause 的前置条件
        if (pause) PlayerPrefs.Save();
    }

// 响应 OnApplicationQuit 生命周期
    private void OnApplicationQuit()
    {
// 在 OnApplicationQuit 中继续当前处理
        PlayerPrefs.Save();
    }
}
