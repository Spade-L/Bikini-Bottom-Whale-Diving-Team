using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 管理存档槽位显示、保存、读取与恢复
public class SaveMenuController : MonoBehaviour
{
// 推进 SaveMenuController 的当前步骤
    public static SaveMenuController Instance { get; private set; }

    [Header("存档预制体")]
// 保存 savePrefab 引用
    [SerializeField] private GameObject savePrefab;

// 配置 快捷打开 分组
    [Header("快捷打开")]
    [SerializeField] private KeyCode toggleKey = KeyCode.H;

// 保存 saveView 引用
    private GameObject saveView;
    private readonly Dictionary<int, Button> slotButtons = new Dictionary<int, Button>();
// 在 SaveMenuController 中处理 推进 SaveMenuController 的当前步骤
    private readonly Dictionary<int, TMP_Text> slotSceneTexts = new Dictionary<int, TMP_Text>();
    private readonly Dictionary<int, TMP_Text> slotTimeTexts = new Dictionary<int, TMP_Text>();

// 同步 SaveMenuController 的相关数据
    private TMP_Text nowSelectText;
    private Button saveButton;
// 同步 SaveMenuController 的相关数据（SaveMenuController 后续步骤）
    private Button loadButton;
    private Button deleteButton;
    private Button backButton;
// 同步 SaveMenuController 的相关数据（SaveMenuController 后续步骤）（33）
    private Selectable firstSelectable;

// 同步 SaveMenuController 的相关数据（SaveMenuController 后续步骤）（36）
    private IDisposable movementLock;
    private IDisposable interactionLock;
// 同步 SaveMenuController 的相关数据（SaveMenuController 后续步骤）（39）
    private SaveData pendingLoad;
    private int pendingLoadSlot;
// 设置 SaveMenuController 的配置数值
    private int selectedSlot;
    private bool isOpen;
// 记录 SaveMenuController 的当前状态
    private bool loadOnly;
    private bool transitionInProgress;
// 记录 SaveMenuController 的当前状态（SaveMenuController 后续步骤）
    private bool listenersRegistered;

// 记录 SaveMenuController 的当前状态（SaveMenuController 后续步骤）（51）
    public bool IsOpen => isOpen;

// 在场景加载前创建常驻管理器
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// AutoCreate 缺少引用时提前结束
        if (Instance == null)
        {
// 添加所需组件
            new GameObject("SaveMenu").AddComponent<SaveMenuController>();
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
        CreateSaveView();
        Close();
    }

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 推进 Start 中的必要步骤
        RegisterListeners();
    }

// 创建 CreateSaveView 对应对象
    private void CreateSaveView()
    {
// 检查 CreateSaveView 的前置条件
        if (saveView != null) return;

// 缺少必要引用时退出 CreateSaveView
        if (savePrefab == null)
        {
// 同步 CreateSaveView 的内部状态
            savePrefab = Resources.Load<GameObject>("Save");
        }

// 缺少必要引用时退出 CreateSaveView（CreateSaveView）
        if (savePrefab == null)
        {
// 输出调试信息
            Debug.LogError("存档页面：未找到 Save.prefab。请在 Inspector 指定 savePrefab，或将其放入 Assets/Resources/Save.prefab。");
            return;
        }

// 同步 CreateSaveView 的内部状态（CreateSaveView）
        saveView = Instantiate(savePrefab, transform);
        saveView.name = "SaveView";
// 更新局部位置
        saveView.transform.localPosition = Vector3.zero;
        saveView.transform.localRotation = Quaternion.identity;
// 同步 CreateSaveView 的内部状态（CreateSaveView）（saveView）
        saveView.transform.localScale = Vector3.one;

// 同步 CreateSaveView 的内部状态（CreateSaveView）（nowSelectText）
        nowSelectText = FindText(saveView.transform, "NowSelect");
        saveButton = FindButton(saveView.transform, "SaveGame");
// 同步 CreateSaveView 的内部状态（CreateSaveView）（loadButton）
        loadButton = FindButton(saveView.transform, "LoadGame");
        deleteButton = FindButton(saveView.transform, "Delete");
        backButton = FindButton(saveView.transform, "BackGame", "Close", "Back", "Return");

// 循环处理当前集合
        for (int slot = SaveSystem.MinSlot; slot <= SaveSystem.MaxSlot; slot++)
        {
// 缓存 CreateSaveView 所需引用
            Transform slotRoot = FindTransform(saveView.transform, $"Save{slot}");
            if (slotRoot == null)
            {
// 在 CreateSaveView 中继续当前处理
                Debug.LogWarning($"存档页面：未找到 Save{slot}。");
                continue;
            }

// 同步 CreateSaveView 的相关数据
            Button slotButton = slotRoot.GetComponent<Button>() ?? slotRoot.GetComponentInChildren<Button>(true);
            TMP_Text sceneText = FindText(slotRoot, "SaveWhere");
// 同步 CreateSaveView 的相关数据（CreateSaveView 后续步骤）
            TMP_Text timeText = FindText(slotRoot, "SaveTime");

// 同步 CreateSaveView 的内部状态（CreateSaveView）（slotButtons）
            slotButtons[slot] = slotButton;
            slotSceneTexts[slot] = sceneText;
// 同步 CreateSaveView 的内部状态（CreateSaveView）（slotTimeTexts）
            slotTimeTexts[slot] = timeText;
            if (firstSelectable == null && slotButton != null) firstSelectable = slotButton;

// 缺少必要引用时退出 CreateSaveView（CreateSaveView）（if）
            if (slotButton == null) Debug.LogWarning($"存档页面：Save{slot} 未找到 Button。");
            if (sceneText == null) Debug.LogWarning($"存档页面：Save{slot} 未找到 SaveWhere 文本。");
// 在 CreateSaveView 中继续当前处理（CreateSaveView 后续步骤）
            if (timeText == null) Debug.LogWarning($"存档页面：Save{slot} 未找到 SaveTime 文本。");
        }

// 在 CreateSaveView 中继续当前处理（CreateSaveView 后续步骤）（161）
        if (firstSelectable == null) firstSelectable = loadButton != null ? loadButton : backButton;
    }

// 处理 RegisterListeners 对应逻辑
    private void RegisterListeners()
    {
// 检查 RegisterListeners 的前置条件
        if (listenersRegistered) return;
        listenersRegistered = true;

// 遍历全部元素
        foreach (KeyValuePair<int, Button> pair in slotButtons)
        {
// 设置 RegisterListeners 的配置数值
            int slot = pair.Key;
            pair.Value?.onClick.AddListener(() => SelectSlot(slot));
        }

// 使用 RegisterListeners 所需功能
        saveButton?.onClick.AddListener(SaveSelectedSlot);
        loadButton?.onClick.AddListener(LoadSelectedSlot);
        deleteButton?.onClick.AddListener(DeleteSelectedSlot);
// 使用 RegisterListeners 所需功能（RegisterListeners）
        backButton?.onClick.AddListener(Close);
    }

// H 键在允许的场景中切换存档页面
    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;
        ToggleFromShortcut();
    }

    public void ToggleFromShortcut()
    {
        if (transitionInProgress) return;
        SettingsOverlayController settings = SettingsOverlayController.Instance;
        if (settings == null || !settings.CanToggleInCurrentScene) return;
// 从设置页面切换到存档页面
        if (settings.IsOpen)
        {
            settings.Close();
            if (settings.IsOpen) return;
            if (settings.OpenSound != null && SfxManager.Instance != null) SfxManager.Instance.Play(settings.OpenSound);
            Open(false);
            return;
        }
        if (isOpen)
        {
            Close();
            return;
        }
        if (settings.OpenSound != null && SfxManager.Instance != null) SfxManager.Instance.Play(settings.OpenSound);
        Open(false);
    }

// 处理 Open 对应逻辑
    public void Open(bool openedFromMainMenu = false)
    {
// 检查 Open 的前置条件
        if (transitionInProgress) return;
        if (SettingsOverlayController.Instance != null && SettingsOverlayController.Instance.IsOpen) return;
        if (saveView == null)
        {
// 推进 Open 中的必要步骤
            CreateSaveView();
            RegisterListeners();
        }
// 缺少必要引用时退出 Open
        if (saveView == null) return;

// 同步 Open 的内部状态
        isOpen = true;
        loadOnly = openedFromMainMenu;
// 同步 Open 的内部状态（Open）
        selectedSlot = 0;
        pendingLoad = null;
// 同步 Open 的内部状态（Open）（pendingLoadSlot）
        pendingLoadSlot = 0;
        UpdateSelectionText();
// 推进 Open 中的必要步骤（Open）
        RefreshSlots();
        if (saveButton != null) saveButton.interactable = !loadOnly;
// 切换 Open 的显示状态
        saveView.SetActive(true);

// 同步 Open 的内部状态（Open）（movementLock）
        movementLock = GameplayInputLock.AcquireMovementLock();
        interactionLock = GameplayInputLock.AcquireInteractionLock();
// 检查 Open 的前置条件（Open）
        if (firstSelectable != null && EventSystem.current != null)
        {
// 使用 Open 所需功能
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }
    }

// 处理 Close 对应逻辑
    public void Close()
    {
// 检查 Close 的前置条件
        if (transitionInProgress) return;
        isOpen = false;
// 同步 Close 的内部状态
        loadOnly = false;
        selectedSlot = 0;
// 同步 Close 的内部状态（Close）
        pendingLoad = null;
        pendingLoadSlot = 0;
// 检查 Close 的前置条件（Close）
        if (saveView != null) saveView.SetActive(false);
        ReleaseLocks();
    }

// 处理 SelectSlot 对应逻辑
    public void SelectSlot(int slot)
    {
// 检查 SelectSlot 的前置条件
        if (!isOpen || !SaveSystem.IsValidSlot(slot)) return;
        selectedSlot = slot;
// 推进 SelectSlot 中的必要步骤
        UpdateSelectionText();
        RefreshSlots();
    }

// 把当前运行状态写入选中槽位
    public void SaveSelectedSlot()
    {
// 检查 SaveSelectedSlot 的前置条件
        if (!isOpen || loadOnly || transitionInProgress) return;
        if (!SaveSystem.IsValidSlot(selectedSlot))
        {
// 在 SaveSelectedSlot 中继续当前处理
            Debug.LogWarning("存档页面：请先选择存档位。");
            return;
        }

// 同步 SaveSelectedSlot 的相关数据
        GameManager manager = GameManager.Instance;
        SaveData data = manager == null ? null : manager.CaptureSaveData();
// 缺少必要引用时退出 SaveSelectedSlot
        if (data == null)
        {
// 在 SaveSelectedSlot 中继续当前处理（SaveSelectedSlot 后续步骤）
            Debug.LogWarning("存档页面：当前场景没有可保存的玩家状态。");
            return;
        }

// 检查 SaveSelectedSlot 的前置条件（SaveSelectedSlot）
        if (SaveSystem.Save(data, selectedSlot))
        {
// 推进 SaveSelectedSlot 中的必要步骤
            RefreshSlots();
            Close();
        }
    }

// 删除当前选中的存档槽位
    public void DeleteSelectedSlot()
    {
        if (!isOpen || transitionInProgress) return;
        if (!SaveSystem.IsValidSlot(selectedSlot))
        {
            Debug.LogWarning("存档页面：请先选择要删除的存档位。");
            return;
        }
        SaveSystem.Delete(selectedSlot);
        RefreshSlots();
        UpdateSelectionText();
    }

// 读取选中槽位并切换到对应场景
    public void LoadSelectedSlot()
    {
// 检查 LoadSelectedSlot 的前置条件
        if (!isOpen || transitionInProgress) return;
        if (!SaveSystem.IsValidSlot(selectedSlot))
        {
// 在 LoadSelectedSlot 中继续当前处理
            Debug.LogWarning("存档页面：请先选择存档位。");
            return;
        }
// 检查 LoadSelectedSlot 的前置条件（LoadSelectedSlot）
        if (!SaveSystem.TryLoad(selectedSlot, out SaveData data))
        {
// 在 LoadSelectedSlot 中继续当前处理（LoadSelectedSlot 后续步骤）
            Debug.LogWarning("存档页面：当前存档位没有有效记录。");
            RefreshSlots();
// 返回 LoadSelectedSlot 的处理结果
            return;
        }
// 检查 LoadSelectedSlot 的前置条件（LoadSelectedSlot）（if）
        if (!IsLoadableScene(data.sceneName))
        {
// 在 LoadSelectedSlot 中继续当前处理（LoadSelectedSlot 后续步骤）（310）
            Debug.LogWarning($"存档页面：存档场景不可加载 ({data.sceneName})。");
            return;
        }

// 同步 LoadSelectedSlot 的内部状态
        transitionInProgress = true;
        pendingLoad = data;
// 同步 LoadSelectedSlot 的内部状态（LoadSelectedSlot）
        pendingLoadSlot = selectedSlot;
        AcquireLocks();
// 同步 LoadSelectedSlot 的内部状态（LoadSelectedSlot）（isOpen）
        isOpen = false;
        if (saveView != null) saveView.SetActive(false);

// 检查转场状态
        if (ScreenFader.Instance != null)
        {
// 执行场景切换
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(data.sceneName));
        }
// 处理 LoadSelectedSlot 的备用分支
        else
        {
// 在 LoadSelectedSlot 中继续当前处理（LoadSelectedSlot 后续步骤）（334）
            SceneManager.LoadScene(data.sceneName);
        }
    }

// 响应 OnSceneLoaded 生命周期
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
// 缺少必要引用时退出 OnSceneLoaded
        if (!transitionInProgress || pendingLoad == null) return;
        if (!scene.name.Equals(pendingLoad.sceneName, StringComparison.Ordinal)) return;
// 启动当前协程
        StartCoroutine(RestoreAfterSceneLoad(scene, pendingLoad));
    }

// 场景就绪后恢复玩家位置与全局状态
    private IEnumerator RestoreAfterSceneLoad(Scene scene, SaveData data)
    {
// 保存 player 引用
        PlayerMovement2D player = null;
        float timeout = 5f;
// 等待条件变化
        while (player == null && timeout > 0f)
        {
// 同步 RestoreAfterSceneLoad 的内部状态
            player = FindFirstObjectByType<PlayerMovement2D>();
            timeout -= Time.unscaledDeltaTime;
// 缺少必要引用时退出 RestoreAfterSceneLoad
            if (player == null) yield return null;
        }

// 检查 RestoreAfterSceneLoad 的前置条件
        if (GameManager.Instance != null) GameManager.Instance.RestoreSaveData(data);
        if (player != null)
        {
// 更新当前位置
            player.transform.position = new Vector3(data.playerX, data.playerY, player.transform.position.z);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
// 检查 RestoreAfterSceneLoad 的前置条件（RestoreAfterSceneLoad）
            if (body != null) body.velocity = Vector2.zero;
        }
// 处理 RestoreAfterSceneLoad 的备用分支
        else
        {
// 在 RestoreAfterSceneLoad 中继续当前处理
            Debug.LogWarning("存档页面：目标场景加载后未找到玩家，已恢复全局状态但未恢复位置。");
        }

// 同步 RestoreAfterSceneLoad 的内部状态（RestoreAfterSceneLoad）
        pendingLoad = null;
        pendingLoadSlot = 0;
// 同步 RestoreAfterSceneLoad 的内部状态（RestoreAfterSceneLoad）（transitionInProgress）
        transitionInProgress = false;
        isOpen = false;
// 推进 RestoreAfterSceneLoad 中的必要步骤
        ReleaseLocks();
    }

// 刷新存档槽位的场景名称与保存时间
    private void RefreshSlots()
    {
// 在 RefreshSlots 中继续当前处理
        for (int slot = SaveSystem.MinSlot; slot <= SaveSystem.MaxSlot; slot++)
        {
// 同步 RefreshSlots 的相关数据
            SaveData data;
            bool available = SaveSystem.TryLoad(slot, out data);
// 检查 RefreshSlots 的前置条件
            if (slotSceneTexts.TryGetValue(slot, out TMP_Text sceneText) && sceneText != null)
            {
// 更新 RefreshSlots 的界面文本
                sceneText.text = available ? GetDisplaySceneName(data.sceneName) : "Null";
            }
// 检查 RefreshSlots 的前置条件（RefreshSlots）
            if (slotTimeTexts.TryGetValue(slot, out TMP_Text timeText) && timeText != null)
            {
// 更新 RefreshSlots 的界面文本（RefreshSlots 后续步骤）
                timeText.text = available && TryFormatSavedTime(data, out string text) ? text : "Null";
            }
// 检查 RefreshSlots 的前置条件（RefreshSlots）（if）
            if (slotButtons.TryGetValue(slot, out Button button) && button != null)
            {
// 同步 RefreshSlots 的内部状态
                button.interactable = true;
            }
        }
    }

// 获取 GetDisplaySceneName 所需引用
    private static string GetDisplaySceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return "Null";
        switch (sceneName.Trim().ToLowerInvariant())
        {
            case "level1": return "Home";
            case "level2": return "School";
            case "level3": return "Store";
            case "level4": return "Alley";
            case "level5": return "Playground";
            case "level6": return "Rooftop";
            default: return sceneName;
        }
    }
// 处理 TryFormatSavedTime 对应逻辑
    private bool TryFormatSavedTime(SaveData data, out string text)
    {
// 同步 TryFormatSavedTime 的内部状态
        text = null;
        if (data == null || !DateTime.TryParse(data.savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime utc))
        {
// 返回 TryFormatSavedTime 的处理结果
            return false;
        }
// 同步 TryFormatSavedTime 的相关数据
        DateTime local = utc.ToLocalTime();
        text = local.ToString("yyyy年MM月dd日 HH时mm分ss秒");
// 返回 TryFormatSavedTime 的处理结果（TryFormatSavedTime）
        return true;
    }

// 刷新 UpdateSelectionText 对应状态
    private void UpdateSelectionText()
    {
// 检查 UpdateSelectionText 的前置条件
        if (nowSelectText != null)
        {
// 更新 UpdateSelectionText 的界面文本
            nowSelectText.text = selectedSlot > 0 ? $"当前选择：Save {selectedSlot}" : "当前选择：Null";
        }
    }

// 判断 IsLoadableScene 对应条件
    private bool IsLoadableScene(string sceneName)
    {
// 检查 IsLoadableScene 的前置条件
        if (string.IsNullOrWhiteSpace(sceneName) || sceneName.Equals("Menu", StringComparison.OrdinalIgnoreCase) || sceneName.Equals("MainMenu", StringComparison.OrdinalIgnoreCase)) return false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
// 缓存 IsLoadableScene 所需引用
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path).Equals(sceneName, StringComparison.Ordinal)) return true;
        }
// 返回 IsLoadableScene 的处理结果
        return false;
    }

// 处理 AcquireLocks 对应逻辑
    private void AcquireLocks()
    {
// 使用 AcquireLocks 所需功能
        movementLock ??= GameplayInputLock.AcquireMovementLock();
        interactionLock ??= GameplayInputLock.AcquireInteractionLock();
    }

// 处理 ReleaseLocks 对应逻辑
    private void ReleaseLocks()
    {
// 使用 ReleaseLocks 所需功能
        movementLock?.Dispose();
        interactionLock?.Dispose();
// 同步 ReleaseLocks 的内部状态
        movementLock = null;
        interactionLock = null;
    }

// 获取 FindTransform 所需引用
    private static Transform FindTransform(Transform root, string name)
    {
// 在 FindTransform 中继续当前处理
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
// 检查 FindTransform 的前置条件
            if (child.name == name) return child;
        }
// 返回 FindTransform 的处理结果
        return null;
    }

// 获取 FindButton 所需引用
    private static Button FindButton(Transform root, params string[] names)
    {
// 在 FindButton 中继续当前处理
        foreach (string name in names)
        {
// 获取 FindTransform 所需引用（FindButton）
            Transform target = FindTransform(root, name);
            Button button = target == null ? null : target.GetComponent<Button>();
// 检查 FindButton 的前置条件
            if (button != null) return button;
        }
// 返回 FindButton 的处理结果
        return null;
    }

// 获取 FindText 所需引用
    private static TMP_Text FindText(Transform root, string name)
    {
// 获取 FindTransform 所需引用（FindText）
        Transform target = FindTransform(root, name);
        return target == null ? null : target.GetComponent<TMP_Text>();
    }

// 启用时订阅事件并恢复状态
    private void OnEnable()
    {
// 在 OnEnable 中继续当前处理
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

// 禁用时取消订阅并清理临时状态
    private void OnDisable()
    {
// 在 OnDisable 中继续当前处理
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleaseLocks();
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 在 OnDestroy 中继续当前处理
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleaseLocks();
// 检查 OnDestroy 的前置条件
        if (Instance == this) Instance = null;
    }
}

