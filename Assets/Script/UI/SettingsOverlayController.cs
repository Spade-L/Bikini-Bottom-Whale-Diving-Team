using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 定义 SettingsOverlayController 类型
public class SettingsOverlayController : MonoBehaviour
{
    public static SettingsOverlayController Instance { get; private set; }

// 配置 设置预制体 分组
    [Header("设置预制体")]
    [SerializeField] private GameObject settingsPrefab;

// 保存 settingsView 引用
    private GameObject settingsView;
    private Slider musicSlider;
// 保存 sfxSlider 数据
    private Slider sfxSlider;
    private Button switchWindowModeButton;
// 保存 quitGameButton 数据
    private Button quitGameButton;
    private Button closeButton;
// 保存 saveGameButton 数据
    private Button saveGameButton;
    private Selectable firstSelectable;
// 保存 fullscreenLabel 引用
    private GameObject fullscreenLabel;
    private GameObject windowedLabel;
// 保存 borderlessLabel 引用
    private GameObject borderlessLabel;

// 保存 movementLock 数据
    private System.IDisposable movementLock;
    private System.IDisposable interactionLock;
// 记录 isOpen 状态
    private bool isOpen;
    private bool listenersRegistered;

// 记录 IsOpen 状态
    public bool IsOpen => isOpen;

// 运行前初始化状态
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// 空引用时直接退出
        if (Instance == null)
        {
// 添加所需组件
            new GameObject("SettingsOverlay").AddComponent<SettingsOverlayController>();
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
// 执行 CreateSettingsView
        CreateSettingsView();
        Close();
    }

// 定义 Start 方法
    private void Start()
    {
// 执行 RegisterListeners
        RegisterListeners();
    }

// 定义 Update 方法
    private void Update()
    {
// 检测按键输入
        if (!IsGameplayScene() || !Input.GetKeyDown(KeyCode.X)) return;
        if (isOpen) Close();
// 处理其他分支
        else Open();
    }

// 定义 IsGameplayScene 方法
    private bool IsGameplayScene()
    {
// 执行场景切换
        Scene scene = SceneManager.GetActiveScene();
        return scene.IsValid()
// 更新当前逻辑
            && scene.isLoaded
            && scene.buildIndex >= 0
// 调用 Equals
            && !scene.name.Equals("Menu", System.StringComparison.OrdinalIgnoreCase)
            && !scene.name.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase);
    }

// 定义 CreateSettingsView 方法
    private void CreateSettingsView()
    {
// 判断当前条件
        if (settingsView != null) return;

// 空引用时直接退出
        if (settingsPrefab == null)
        {
// 更新当前状态
            settingsPrefab = Resources.Load<GameObject>("Settings");
        }

// 空引用时直接退出
        if (settingsPrefab == null)
        {
// 输出调试信息
            Debug.LogError("设置页面：未找到 Settings.prefab。请将预制体放入 Assets/Resources/Settings.prefab，或在 Inspector 中指定 settingsPrefab。");
            return;
        }

// 更新当前状态
        settingsView = Instantiate(settingsPrefab, transform);
        settingsView.name = "SettingsView";
// 更新局部位置
        settingsView.transform.localPosition = Vector3.zero;
        settingsView.transform.localRotation = Quaternion.identity;
// 更新当前状态
        settingsView.transform.localScale = Vector3.one;

// 更新当前状态
        musicSlider = FindComponent<Slider>("MusicSlider");
        sfxSlider = FindComponent<Slider>("SoundSlider");
// 更新当前状态
        switchWindowModeButton = FindButton("Switch window mode");
        quitGameButton = FindButton("QuitGame");
// 更新当前状态
        saveGameButton = FindButton("SaveGame");
        closeButton = FindButton("BackGame", "Close", "Back", "Return", "X", "CloseButton");
// 更新当前状态
        firstSelectable = musicSlider != null ? musicSlider : switchWindowModeButton;

// 更新当前状态
        fullscreenLabel = FindObject("Full screen", "Fullscreen");
        windowedLabel = FindObject("Windowed");
// 更新当前状态
        borderlessLabel = FindObject("Windowed fullscreen", "Borderless");
    }

// 定义 RegisterListeners 方法
    private void RegisterListeners()
    {
// 判断当前条件
        if (listenersRegistered) return;
        listenersRegistered = true;
// 调用 AddListener
        musicSlider?.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider?.onValueChanged.AddListener(SetSfxVolume);
// 调用 AddListener
        switchWindowModeButton?.onClick.AddListener(SwitchWindowMode);
        quitGameButton?.onClick.AddListener(QuitGame);
// 调用 AddListener
        saveGameButton?.onClick.AddListener(OpenSaveMenu);
        closeButton?.onClick.AddListener(Close);
    }

// 定义 Open 方法
    public void Open()
    {
// 空引用时直接退出
        if (isOpen || SettingsManager.Instance == null) return;
        if (settingsView == null)
        {
// 执行 CreateSettingsView
            CreateSettingsView();
            RegisterListeners();
        }
// 空引用时直接退出
        if (settingsView == null) return;

// 更新当前状态
        isOpen = true;
        SettingsManager settings = SettingsManager.Instance;
// 调用 SetValueWithoutNotify
        musicSlider?.SetValueWithoutNotify(settings.MusicVolume);
        sfxSlider?.SetValueWithoutNotify(settings.SfxVolume);
// 执行 RefreshDisplayModeLabel
        RefreshDisplayModeLabel(settings.DisplayMode);
        settingsView.SetActive(true);

// 更新当前状态
        movementLock = GameplayInputLock.AcquireMovementLock();
        interactionLock = GameplayInputLock.AcquireInteractionLock();
// 判断当前条件
        if (firstSelectable != null && EventSystem.current != null)
        {
// 调用 SetSelectedGameObject
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }
    }

// 定义 Close 方法
    public void Close()
    {
// 更新当前状态
        isOpen = false;
        if (settingsView != null) settingsView.SetActive(false);
// 调用 Dispose
        movementLock?.Dispose();
        interactionLock?.Dispose();
// 更新当前状态
        movementLock = null;
        interactionLock = null;
    }

// 定义 SetMusicVolume 方法
    public void SetMusicVolume(float value)
    {
// 调用 SetMusicVolume
        SettingsManager.Instance?.SetMusicVolume(value);
    }

// 定义 SetSfxVolume 方法
    public void SetSfxVolume(float value)
    {
// 调用 SetSfxVolume
        SettingsManager.Instance?.SetSfxVolume(value);
    }

// 定义 SwitchWindowMode 方法
    public void SwitchWindowMode()
    {
// 空引用时直接退出
        if (SettingsManager.Instance == null) return;
        int next = ((int)SettingsManager.Instance.DisplayMode + 1) % 3;
// 调用 SetDisplayMode
        SettingsManager.Instance.SetDisplayMode((GameDisplayMode)next);
        RefreshDisplayModeLabel((GameDisplayMode)next);
    }

// 定义 SetDisplayMode 方法
    public void SetDisplayMode(int value)
    {
// 保存 mode 数据
        GameDisplayMode mode = (GameDisplayMode)Mathf.Clamp(value, 0, 2);
        SettingsManager.Instance?.SetDisplayMode(mode);
// 执行 RefreshDisplayModeLabel
        RefreshDisplayModeLabel(mode);
    }

// 定义 RefreshDisplayModeLabel 方法
    private void RefreshDisplayModeLabel(GameDisplayMode mode)
    {
// 判断当前条件
        if (fullscreenLabel != null) fullscreenLabel.SetActive(mode == GameDisplayMode.Fullscreen);
        if (windowedLabel != null) windowedLabel.SetActive(mode == GameDisplayMode.Windowed);
// 判断当前条件
        if (borderlessLabel != null) borderlessLabel.SetActive(mode == GameDisplayMode.Borderless);
    }

// 定义 QuitGame 方法
    public void QuitGame()
    {
// 保存本地配置
        PlayerPrefs.Save();
        Application.Quit();
#if UNITY_EDITOR
// 输出调试信息
        Debug.Log("设置页面：编辑器中无法退出游戏。");
#endif
    }

// 定义 OpenSaveMenu 方法
    public void OpenSaveMenu()
    {
// 保存 target 数据
        SaveMenuController target = SaveMenuController.Instance;
        if (target == null)
        {
// 输出调试信息
            Debug.LogWarning("设置页面：未找到存档页面控制器。");
            return;
        }

// 执行 Close
        Close();
        target.Open(false);
    }

// 查找目标对象
    private T FindComponent<T>(params string[] names) where T : Component
    {
// 定义 FindObject 方法
        GameObject target = FindObject(names);
        return target == null ? null : target.GetComponent<T>();
    }

// 定义 FindButton 方法
    private Button FindButton(params string[] names)
    {
// 返回当前结果
        return FindComponent<Button>(names);
    }

// 定义 FindObject 方法
    private GameObject FindObject(params string[] names)
    {
// 空引用时直接退出
        if (settingsView == null) return null;
        Transform[] children = settingsView.GetComponentsInChildren<Transform>(true);
// 遍历全部元素
        foreach (string name in names)
        {
// 遍历全部元素
            foreach (Transform child in children)
            {
// 判断当前条件
                if (child.name == name) return child.gameObject;
            }
        }
// 返回当前结果
        return null;
    }

// 定义 OnDisable 方法
    private void OnDisable()
    {
// 判断当前条件
        if (Instance == this) Close();
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 判断当前条件
        if (Instance == this) Instance = null;
        movementLock?.Dispose();
// 调用 Dispose
        interactionLock?.Dispose();
    }
}
