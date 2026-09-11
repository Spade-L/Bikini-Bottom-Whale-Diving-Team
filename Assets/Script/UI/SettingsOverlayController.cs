using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 管理游戏内设置面板与显示选项
public class SettingsOverlayController : MonoBehaviour
{
    public static SettingsOverlayController Instance { get; private set; }

// 配置 设置预制体 分组
    [Header("设置预制体")]
    [SerializeField] private GameObject settingsPrefab;

// 配置 设置菜单打开音效 分组
    [Header("设置菜单打开音效")]
    [SerializeField] private AudioClip openSound;

// 保存 settingsView 引用
    private GameObject settingsView;
    private Slider musicSlider;
// 同步 SettingsOverlayController 的相关数据
    private Slider sfxSlider;
    private Button switchWindowModeButton;
// 同步 SettingsOverlayController 的相关数据（SettingsOverlayController 后续步骤）
    private Button quitGameButton;
    private Button closeButton;
// 同步 SettingsOverlayController 的相关数据（SettingsOverlayController 后续步骤）（23）
    private Button saveGameButton;
    private Selectable firstSelectable;
// 保存 fullscreenLabel 引用
    private GameObject fullscreenLabel;
    private GameObject windowedLabel;
// 保存 borderlessLabel 引用
    private GameObject borderlessLabel;

// 同步 SettingsOverlayController 的相关数据（SettingsOverlayController 后续步骤）（32）
    private System.IDisposable movementLock;
    private System.IDisposable interactionLock;
// 记录 SettingsOverlayController 的当前状态
    private bool isOpen;
    private bool listenersRegistered;

// 记录 SettingsOverlayController 的当前状态（SettingsOverlayController 后续步骤）
    public bool IsOpen => isOpen;

// 在场景加载前创建常驻管理器
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// AutoCreate 缺少引用时提前结束
        if (Instance == null)
        {
// 添加所需组件
            new GameObject("SettingsOverlay").AddComponent<SettingsOverlayController>();
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
        CreateSettingsView();
        Close();
    }

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 推进 Start 中的必要步骤
        RegisterListeners();
    }

// 每帧检查输入与状态变化
    private void Update()
    {
// 检测按键输入
        if (!IsGameplayScene() || !Input.GetKeyDown(KeyCode.X)) return;
        if (isOpen) Close();
// X 键打开设置时先播放专用音效
        else OpenWithSound();
    }

// 判断 IsGameplayScene 对应条件
    private bool IsGameplayScene()
    {
// 执行场景切换
        Scene scene = SceneManager.GetActiveScene();
        return scene.IsValid()
// 推进 IsGameplayScene 的当前步骤
            && scene.isLoaded
            && scene.buildIndex >= 0
// 使用 IsGameplayScene 所需功能
            && !scene.name.Equals("Menu", System.StringComparison.OrdinalIgnoreCase)
            && !scene.name.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase);
    }

// 创建 CreateSettingsView 对应对象
    private void CreateSettingsView()
    {
// 检查 CreateSettingsView 的前置条件
        if (settingsView != null) return;

// 缺少必要引用时退出 CreateSettingsView
        if (settingsPrefab == null)
        {
// 同步 CreateSettingsView 的内部状态
            settingsPrefab = Resources.Load<GameObject>("Settings");
        }

// 缺少必要引用时退出 CreateSettingsView（CreateSettingsView）
        if (settingsPrefab == null)
        {
// 输出调试信息
            Debug.LogError("设置页面：未找到 Settings.prefab。请将预制体放入 Assets/Resources/Settings.prefab，或在 Inspector 中指定 settingsPrefab。");
            return;
        }

// 同步 CreateSettingsView 的内部状态（CreateSettingsView）
        settingsView = Instantiate(settingsPrefab, transform);
        settingsView.name = "SettingsView";
// 从设置预制体读取打开音效引用
        if (openSound == null)
        {
            AudioSource audioSource = settingsView.GetComponentInChildren<AudioSource>(true);
            if (audioSource != null) openSound = audioSource.clip;
        }
// 更新局部位置
        settingsView.transform.localPosition = Vector3.zero;
        settingsView.transform.localRotation = Quaternion.identity;
// 同步 CreateSettingsView 的内部状态（CreateSettingsView）（settingsView）
        settingsView.transform.localScale = Vector3.one;

// 同步 CreateSettingsView 的内部状态（CreateSettingsView）（musicSlider）
        musicSlider = FindComponent<Slider>("MusicSlider");
        sfxSlider = FindComponent<Slider>("SoundSlider");
// 同步 CreateSettingsView 的内部状态（CreateSettingsView）（switchWindowModeButton）
        switchWindowModeButton = FindButton("Switch window mode");
        quitGameButton = FindButton("QuitGame");
// 同步 CreateSettingsView 的内部状态（CreateSettingsView）（saveGameButton）
        saveGameButton = FindButton("SaveGame");
        closeButton = FindButton("BackGame", "Close", "Back", "Return", "X", "CloseButton");
// 同步 CreateSettingsView 的内部状态（CreateSettingsView）（firstSelectable）
        firstSelectable = musicSlider != null ? musicSlider : switchWindowModeButton;

// 同步 CreateSettingsView 的内部状态（CreateSettingsView）（fullscreenLabel）
        fullscreenLabel = FindObject("Full screen", "Fullscreen");
        windowedLabel = FindObject("Windowed");
// 同步 CreateSettingsView 的内部状态（CreateSettingsView）（borderlessLabel）
        borderlessLabel = FindObject("Windowed fullscreen", "Borderless");
    }

// 处理 RegisterListeners 对应逻辑
    private void RegisterListeners()
    {
// 检查 RegisterListeners 的前置条件
        if (listenersRegistered) return;
        listenersRegistered = true;
// 使用 RegisterListeners 所需功能
        musicSlider?.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider?.onValueChanged.AddListener(SetSfxVolume);
// 使用 RegisterListeners 所需功能（RegisterListeners）
        switchWindowModeButton?.onClick.AddListener(SwitchWindowMode);
        quitGameButton?.onClick.AddListener(QuitGame);
// 在 RegisterListeners 中处理 AddListener
        saveGameButton?.onClick.AddListener(OpenSaveMenu);
        closeButton?.onClick.AddListener(Close);
    }

// 播放设置菜单打开音效并打开面板
    private void OpenWithSound()
    {
// 面板已打开或设置系统未就绪时不处理
        if (isOpen || SettingsManager.Instance == null) return;
        if (openSound != null && SfxManager.Instance != null) SfxManager.Instance.Play(openSound);
        Open();
    }

// 处理 Open 对应逻辑
    public void Open()
    {
// 缺少必要引用时退出 Open
        if (isOpen || SettingsManager.Instance == null) return;
        if (settingsView == null)
        {
// 推进 Open 中的必要步骤
            CreateSettingsView();
            RegisterListeners();
        }
// 缺少必要引用时退出 Open（Open）
        if (settingsView == null) return;

// 同步 Open 的内部状态
        isOpen = true;
        SettingsManager settings = SettingsManager.Instance;
// 使用 Open 所需功能
        musicSlider?.SetValueWithoutNotify(settings.MusicVolume);
        sfxSlider?.SetValueWithoutNotify(settings.SfxVolume);
// 推进 Open 中的必要步骤（Open）
        RefreshDisplayModeLabel(settings.DisplayMode);
        settingsView.SetActive(true);

// 同步 Open 的内部状态（Open）
        movementLock = GameplayInputLock.AcquireMovementLock();
        interactionLock = GameplayInputLock.AcquireInteractionLock();
// 检查 Open 的前置条件
        if (firstSelectable != null && EventSystem.current != null)
        {
// 使用 Open 所需功能（Open）
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }
    }

// 处理 Close 对应逻辑
    public void Close()
    {
// 同步 Close 的内部状态
        isOpen = false;
        if (settingsView != null) settingsView.SetActive(false);
// 使用 Close 所需功能
        movementLock?.Dispose();
        interactionLock?.Dispose();
// 同步 Close 的内部状态（Close）
        movementLock = null;
        interactionLock = null;
    }

// 设置 SetMusicVolume 的目标状态
    public void SetMusicVolume(float value)
    {
// 使用 SetMusicVolume 所需功能
        SettingsManager.Instance?.SetMusicVolume(value);
    }

// 设置 SetSfxVolume 的目标状态
    public void SetSfxVolume(float value)
    {
// 使用 SetSfxVolume 所需功能
        SettingsManager.Instance?.SetSfxVolume(value);
    }

// 处理 SwitchWindowMode 对应逻辑
    public void SwitchWindowMode()
    {
// 缺少必要引用时退出 SwitchWindowMode
        if (SettingsManager.Instance == null) return;
        int next = ((int)SettingsManager.Instance.DisplayMode + 1) % 3;
// 使用 SwitchWindowMode 所需功能
        SettingsManager.Instance.SetDisplayMode((GameDisplayMode)next);
        RefreshDisplayModeLabel((GameDisplayMode)next);
    }

// 设置 SetDisplayMode 的目标状态
    public void SetDisplayMode(int value)
    {
// 同步 SetDisplayMode 的相关数据
        GameDisplayMode mode = (GameDisplayMode)Mathf.Clamp(value, 0, 2);
        SettingsManager.Instance?.SetDisplayMode(mode);
// 推进 SetDisplayMode 中的必要步骤
        RefreshDisplayModeLabel(mode);
    }

// 刷新 RefreshDisplayModeLabel 对应状态
    private void RefreshDisplayModeLabel(GameDisplayMode mode)
    {
// 检查 RefreshDisplayModeLabel 的前置条件
        if (fullscreenLabel != null) fullscreenLabel.SetActive(mode == GameDisplayMode.Fullscreen);
        if (windowedLabel != null) windowedLabel.SetActive(mode == GameDisplayMode.Windowed);
// 检查 RefreshDisplayModeLabel 的前置条件（RefreshDisplayModeLabel）
        if (borderlessLabel != null) borderlessLabel.SetActive(mode == GameDisplayMode.Borderless);
    }

// 处理 QuitGame 对应逻辑
    public void QuitGame()
    {
// 保存本地配置
        PlayerPrefs.Save();
        Application.Quit();
#if UNITY_EDITOR
// 在 QuitGame 中继续当前处理
        Debug.Log("设置页面：编辑器中无法退出游戏。");
#endif
    }

// 处理 OpenSaveMenu 对应逻辑
    public void OpenSaveMenu()
    {
// 同步 OpenSaveMenu 的相关数据
        SaveMenuController target = SaveMenuController.Instance;
        if (target == null)
        {
// 在 OpenSaveMenu 中继续当前处理
            Debug.LogWarning("设置页面：未找到存档页面控制器。");
            return;
        }

// 推进 OpenSaveMenu 中的必要步骤
        Close();
        target.Open(false);
    }

// 查找目标对象
    private T FindComponent<T>(params string[] names) where T : Component
    {
// 缓存 OpenSaveMenu 所需引用
        GameObject target = FindObject(names);
        return target == null ? null : target.GetComponent<T>();
    }

// 获取 FindButton 所需引用
    private Button FindButton(params string[] names)
    {
// 返回 FindButton 的处理结果
        return FindComponent<Button>(names);
    }

// 获取 FindObject 所需引用
    private GameObject FindObject(params string[] names)
    {
// 缺少必要引用时退出 FindObject
        if (settingsView == null) return null;
        Transform[] children = settingsView.GetComponentsInChildren<Transform>(true);
// 遍历全部元素
        foreach (string name in names)
        {
// 在 FindObject 中继续当前处理
            foreach (Transform child in children)
            {
// 检查 FindObject 的前置条件
                if (child.name == name) return child.gameObject;
            }
        }
// 返回 FindObject 的处理结果
        return null;
    }

// 禁用时取消订阅并清理临时状态
    private void OnDisable()
    {
// 检查 OnDisable 的前置条件
        if (Instance == this) Close();
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 检查 OnDestroy 的前置条件
        if (Instance == this) Instance = null;
        movementLock?.Dispose();
// 使用 OnDestroy 所需功能
        interactionLock?.Dispose();
    }
}
