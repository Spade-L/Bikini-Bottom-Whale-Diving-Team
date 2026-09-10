using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 定义 SaveMenuController 类型
public class SaveMenuController : MonoBehaviour
{
// 更新当前逻辑
    public static SaveMenuController Instance { get; private set; }

    [Header("存档预制体")]
// 保存 savePrefab 引用
    [SerializeField] private GameObject savePrefab;

// 保存 saveView 引用
    private GameObject saveView;
    private readonly Dictionary<int, Button> slotButtons = new Dictionary<int, Button>();
// 更新当前逻辑
    private readonly Dictionary<int, TMP_Text> slotSceneTexts = new Dictionary<int, TMP_Text>();
    private readonly Dictionary<int, TMP_Text> slotTimeTexts = new Dictionary<int, TMP_Text>();

// 保存 nowSelectText 数据
    private TMP_Text nowSelectText;
    private Button saveButton;
// 保存 loadButton 数据
    private Button loadButton;
    private Button backButton;
// 保存 firstSelectable 数据
    private Selectable firstSelectable;

// 保存 movementLock 数据
    private IDisposable movementLock;
    private IDisposable interactionLock;
// 保存 pendingLoad 数据
    private SaveData pendingLoad;
    private int pendingLoadSlot;
// 配置 selectedSlot 数值
    private int selectedSlot;
    private bool isOpen;
// 记录 loadOnly 状态
    private bool loadOnly;
    private bool transitionInProgress;
// 记录 listenersRegistered 状态
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
            new GameObject("SaveMenu").AddComponent<SaveMenuController>();
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
// 执行 CreateSaveView
        CreateSaveView();
        Close();
    }

// 定义 Start 方法
    private void Start()
    {
// 执行 RegisterListeners
        RegisterListeners();
    }

// 定义 CreateSaveView 方法
    private void CreateSaveView()
    {
// 判断当前条件
        if (saveView != null) return;

// 空引用时直接退出
        if (savePrefab == null)
        {
// 更新当前状态
            savePrefab = Resources.Load<GameObject>("Save");
        }

// 空引用时直接退出
        if (savePrefab == null)
        {
// 输出调试信息
            Debug.LogError("存档页面：未找到 Save.prefab。请在 Inspector 指定 savePrefab，或将其放入 Assets/Resources/Save.prefab。");
            return;
        }

// 更新当前状态
        saveView = Instantiate(savePrefab, transform);
        saveView.name = "SaveView";
// 更新局部位置
        saveView.transform.localPosition = Vector3.zero;
        saveView.transform.localRotation = Quaternion.identity;
// 更新当前状态
        saveView.transform.localScale = Vector3.one;

// 更新当前状态
        nowSelectText = FindText(saveView.transform, "NowSelect");
        saveButton = FindButton(saveView.transform, "SaveGame");
// 更新当前状态
        loadButton = FindButton(saveView.transform, "LoadGame");
        backButton = FindButton(saveView.transform, "BackGame", "Close", "Back", "Return");

// 循环处理当前集合
        for (int slot = SaveSystem.MinSlot; slot <= SaveSystem.MaxSlot; slot++)
        {
// 定义 FindTransform 方法
            Transform slotRoot = FindTransform(saveView.transform, $"Save{slot}");
            if (slotRoot == null)
            {
// 输出调试信息
                Debug.LogWarning($"存档页面：未找到 Save{slot}。");
                continue;
            }

// 保存 slotButton 数据
            Button slotButton = slotRoot.GetComponent<Button>() ?? slotRoot.GetComponentInChildren<Button>(true);
            TMP_Text sceneText = FindText(slotRoot, "SaveWhere");
// 保存 timeText 数据
            TMP_Text timeText = FindText(slotRoot, "SaveTime");

// 更新当前状态
            slotButtons[slot] = slotButton;
            slotSceneTexts[slot] = sceneText;
// 更新当前状态
            slotTimeTexts[slot] = timeText;
            if (firstSelectable == null && slotButton != null) firstSelectable = slotButton;

// 空引用时直接退出
            if (slotButton == null) Debug.LogWarning($"存档页面：Save{slot} 未找到 Button。");
            if (sceneText == null) Debug.LogWarning($"存档页面：Save{slot} 未找到 SaveWhere 文本。");
// 空引用时直接退出
            if (timeText == null) Debug.LogWarning($"存档页面：Save{slot} 未找到 SaveTime 文本。");
        }

// 空引用时直接退出
        if (firstSelectable == null) firstSelectable = loadButton != null ? loadButton : backButton;
    }

// 定义 RegisterListeners 方法
    private void RegisterListeners()
    {
// 判断当前条件
        if (listenersRegistered) return;
        listenersRegistered = true;

// 遍历全部元素
        foreach (KeyValuePair<int, Button> pair in slotButtons)
        {
// 配置 slot 数值
            int slot = pair.Key;
            pair.Value?.onClick.AddListener(() => SelectSlot(slot));
        }

// 调用 AddListener
        saveButton?.onClick.AddListener(SaveSelectedSlot);
        loadButton?.onClick.AddListener(LoadSelectedSlot);
// 调用 AddListener
        backButton?.onClick.AddListener(Close);
    }

// 定义 Open 方法
    public void Open(bool openedFromMainMenu = false)
    {
// 判断当前条件
        if (transitionInProgress) return;
        if (saveView == null)
        {
// 执行 CreateSaveView
            CreateSaveView();
            RegisterListeners();
        }
// 空引用时直接退出
        if (saveView == null) return;

// 更新当前状态
        isOpen = true;
        loadOnly = openedFromMainMenu;
// 更新当前状态
        selectedSlot = 0;
        pendingLoad = null;
// 更新当前状态
        pendingLoadSlot = 0;
        UpdateSelectionText();
// 执行 RefreshSlots
        RefreshSlots();
        if (saveButton != null) saveButton.interactable = !loadOnly;
// 切换显示状态
        saveView.SetActive(true);

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
// 判断当前条件
        if (transitionInProgress) return;
        isOpen = false;
// 更新当前状态
        loadOnly = false;
        selectedSlot = 0;
// 更新当前状态
        pendingLoad = null;
        pendingLoadSlot = 0;
// 判断当前条件
        if (saveView != null) saveView.SetActive(false);
        ReleaseLocks();
    }

// 定义 SelectSlot 方法
    public void SelectSlot(int slot)
    {
// 判断当前条件
        if (!isOpen || !SaveSystem.IsValidSlot(slot)) return;
        selectedSlot = slot;
// 执行 UpdateSelectionText
        UpdateSelectionText();
        RefreshSlots();
    }

// 定义 SaveSelectedSlot 方法
    public void SaveSelectedSlot()
    {
// 判断当前条件
        if (!isOpen || loadOnly || transitionInProgress) return;
        if (!SaveSystem.IsValidSlot(selectedSlot))
        {
// 输出调试信息
            Debug.LogWarning("存档页面：请先选择存档位。");
            return;
        }

// 保存 manager 数据
        GameManager manager = GameManager.Instance;
        SaveData data = manager == null ? null : manager.CaptureSaveData();
// 空引用时直接退出
        if (data == null)
        {
// 输出调试信息
            Debug.LogWarning("存档页面：当前场景没有可保存的玩家状态。");
            return;
        }

// 判断当前条件
        if (SaveSystem.Save(data, selectedSlot))
        {
// 执行 RefreshSlots
            RefreshSlots();
            Close();
        }
    }

// 定义 LoadSelectedSlot 方法
    public void LoadSelectedSlot()
    {
// 判断当前条件
        if (!isOpen || transitionInProgress) return;
        if (!SaveSystem.IsValidSlot(selectedSlot))
        {
// 输出调试信息
            Debug.LogWarning("存档页面：请先选择存档位。");
            return;
        }
// 判断当前条件
        if (!SaveSystem.TryLoad(selectedSlot, out SaveData data))
        {
// 输出调试信息
            Debug.LogWarning("存档页面：当前存档位没有有效记录。");
            RefreshSlots();
// 返回当前结果
            return;
        }
// 判断当前条件
        if (!IsLoadableScene(data.sceneName))
        {
// 输出调试信息
            Debug.LogWarning($"存档页面：存档场景不可加载 ({data.sceneName})。");
            return;
        }

// 更新当前状态
        transitionInProgress = true;
        pendingLoad = data;
// 更新当前状态
        pendingLoadSlot = selectedSlot;
        AcquireLocks();
// 更新当前状态
        isOpen = false;
        if (saveView != null) saveView.SetActive(false);

// 检查转场状态
        if (ScreenFader.Instance != null)
        {
// 执行场景切换
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(data.sceneName));
        }
// 处理其他分支
        else
        {
// 执行场景切换
            SceneManager.LoadScene(data.sceneName);
        }
    }

// 定义 OnSceneLoaded 方法
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
// 空引用时直接退出
        if (!transitionInProgress || pendingLoad == null) return;
        if (!scene.name.Equals(pendingLoad.sceneName, StringComparison.Ordinal)) return;
// 启动当前协程
        StartCoroutine(RestoreAfterSceneLoad(scene, pendingLoad));
    }

// 定义 RestoreAfterSceneLoad 方法
    private IEnumerator RestoreAfterSceneLoad(Scene scene, SaveData data)
    {
// 保存 player 引用
        PlayerMovement2D player = null;
        float timeout = 5f;
// 等待条件变化
        while (player == null && timeout > 0f)
        {
// 更新当前状态
            player = FindFirstObjectByType<PlayerMovement2D>();
            timeout -= Time.unscaledDeltaTime;
// 空引用时直接退出
            if (player == null) yield return null;
        }

// 判断当前条件
        if (GameManager.Instance != null) GameManager.Instance.RestoreSaveData(data);
        if (player != null)
        {
// 更新当前位置
            player.transform.position = new Vector3(data.playerX, data.playerY, player.transform.position.z);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
// 判断当前条件
            if (body != null) body.velocity = Vector2.zero;
        }
// 处理其他分支
        else
        {
// 输出调试信息
            Debug.LogWarning("存档页面：目标场景加载后未找到玩家，已恢复全局状态但未恢复位置。");
        }

// 更新当前状态
        pendingLoad = null;
        pendingLoadSlot = 0;
// 更新当前状态
        transitionInProgress = false;
        isOpen = false;
// 执行 ReleaseLocks
        ReleaseLocks();
    }

// 定义 RefreshSlots 方法
    private void RefreshSlots()
    {
// 循环处理当前集合
        for (int slot = SaveSystem.MinSlot; slot <= SaveSystem.MaxSlot; slot++)
        {
// 保存 data 数据
            SaveData data;
            bool available = SaveSystem.TryLoad(slot, out data);
// 判断当前条件
            if (slotSceneTexts.TryGetValue(slot, out TMP_Text sceneText) && sceneText != null)
            {
// 更新界面文本
                sceneText.text = available ? GetDisplaySceneName(data.sceneName) : "Null";
            }
// 判断当前条件
            if (slotTimeTexts.TryGetValue(slot, out TMP_Text timeText) && timeText != null)
            {
// 更新界面文本
                timeText.text = available && TryFormatSavedTime(data, out string text) ? text : "Null";
            }
// 判断当前条件
            if (slotButtons.TryGetValue(slot, out Button button) && button != null)
            {
// 更新当前状态
                button.interactable = true;
            }
        }
    }

// 将存档场景编号转换为玩家可见的地点名
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
// 定义 TryFormatSavedTime 方法
    private bool TryFormatSavedTime(SaveData data, out string text)
    {
// 更新当前状态
        text = null;
        if (data == null || !DateTime.TryParse(data.savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime utc))
        {
// 返回当前结果
            return false;
        }
// 保存 local 数据
        DateTime local = utc.ToLocalTime();
        text = local.ToString("yyyy年MM月dd日 HH时mm分ss秒");
// 返回当前结果
        return true;
    }

// 定义 UpdateSelectionText 方法
    private void UpdateSelectionText()
    {
// 判断当前条件
        if (nowSelectText != null)
        {
// 更新界面文本
            nowSelectText.text = selectedSlot > 0 ? $"当前选择：Save {selectedSlot}" : "当前选择：Null";
        }
    }

// 定义 IsLoadableScene 方法
    private bool IsLoadableScene(string sceneName)
    {
// 判断当前条件
        if (string.IsNullOrWhiteSpace(sceneName) || sceneName.Equals("Menu", StringComparison.OrdinalIgnoreCase) || sceneName.Equals("MainMenu", StringComparison.OrdinalIgnoreCase)) return false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
// 定义 GetScenePathByBuildIndex 方法
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path).Equals(sceneName, StringComparison.Ordinal)) return true;
        }
// 返回当前结果
        return false;
    }

// 定义 AcquireLocks 方法
    private void AcquireLocks()
    {
// 调用 AcquireMovementLock
        movementLock ??= GameplayInputLock.AcquireMovementLock();
        interactionLock ??= GameplayInputLock.AcquireInteractionLock();
    }

// 定义 ReleaseLocks 方法
    private void ReleaseLocks()
    {
// 调用 Dispose
        movementLock?.Dispose();
        interactionLock?.Dispose();
// 更新当前状态
        movementLock = null;
        interactionLock = null;
    }

// 定义 FindTransform 方法
    private static Transform FindTransform(Transform root, string name)
    {
// 遍历全部元素
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
// 判断当前条件
            if (child.name == name) return child;
        }
// 返回当前结果
        return null;
    }

// 定义 FindButton 方法
    private static Button FindButton(Transform root, params string[] names)
    {
// 遍历全部元素
        foreach (string name in names)
        {
// 定义 FindTransform 方法
            Transform target = FindTransform(root, name);
            Button button = target == null ? null : target.GetComponent<Button>();
// 判断当前条件
            if (button != null) return button;
        }
// 返回当前结果
        return null;
    }

// 定义 FindText 方法
    private static TMP_Text FindText(Transform root, string name)
    {
// 定义 FindTransform 方法
        Transform target = FindTransform(root, name);
        return target == null ? null : target.GetComponent<TMP_Text>();
    }

// 定义 OnEnable 方法
    private void OnEnable()
    {
// 执行场景切换
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

// 定义 OnDisable 方法
    private void OnDisable()
    {
// 执行场景切换
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleaseLocks();
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 执行场景切换
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleaseLocks();
// 判断当前条件
        if (Instance == this) Instance = null;
    }
}

