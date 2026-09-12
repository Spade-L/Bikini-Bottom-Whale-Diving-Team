using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 在可操作场景中显示 H/X 快捷键，并提供鼠标点击入口
public class GameplayMenuShortcutController : MonoBehaviour
{
    public static GameplayMenuShortcutController Instance { get; private set; }

    [SerializeField] private GameObject saveShortcutPrefab;
    [SerializeField] private GameObject settingsShortcutPrefab;
    [SerializeField] private int canvasSortingOrder = 110;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1600f, 1000f);
    [SerializeField] private Vector2 shortcutMargin = new Vector2(20f, 20f);
    [SerializeField] private float shortcutSpacing = 72f;

    private GameObject saveShortcutInstance;
    private GameObject settingsShortcutInstance;
    private GameObject shortcutCanvasObject;
    private bool lastVisibleState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            new GameObject("GameplayMenuShortcuts").AddComponent<GameplayMenuShortcutController>();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAfterSceneLoad()
    {
        AutoCreate();
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
        BuildShortcutUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (saveShortcutInstance == null || settingsShortcutInstance == null)
        {
            BuildShortcutUI();
        }

        bool shouldShow = CanShowShortcuts();
        if (shortcutCanvasObject != null && !shortcutCanvasObject.activeSelf)
        {
            shortcutCanvasObject.SetActive(true);
        }

        if (saveShortcutInstance != null && saveShortcutInstance.activeSelf != shouldShow)
        {
            saveShortcutInstance.SetActive(shouldShow);
        }

        if (settingsShortcutInstance != null && settingsShortcutInstance.activeSelf != shouldShow)
        {
            settingsShortcutInstance.SetActive(shouldShow);
        }

        if (lastVisibleState != shouldShow)
        {
            lastVisibleState = shouldShow;
            Debug.Log($"[GameplayMenuShortcuts] visible={shouldShow}, scene={SceneManager.GetActiveScene().name}", this);
        }
    }

    private bool CanShowShortcuts()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.buildIndex < 0)
        {
            return false;
        }

        if (scene.name.Equals("Menu", System.StringComparison.OrdinalIgnoreCase)
            || scene.name.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (ScreenFader.IsFading || ScreenFader.IsCovering)
        {
            return false;
        }

        if (GameplayInputLock.IsInteractionLocked)
        {
            return false;
        }

        if (DialogueUIManager.Instance != null && !DialogueUIManager.Instance.CanOpenDialogue)
        {
            return false;
        }

        bool settingsOpen = SettingsOverlayController.Instance != null
            && SettingsOverlayController.Instance.IsOpen;
        bool saveMenuOpen = SaveMenuController.Instance != null
            && SaveMenuController.Instance.IsOpen;
        return !settingsOpen && !saveMenuOpen;
    }

    private void BuildShortcutUI()
    {
        if (saveShortcutInstance != null || settingsShortcutInstance != null)
        {
            return;
        }

        GameObject savePrefab = saveShortcutPrefab != null
            ? saveShortcutPrefab
            : Resources.Load<GameObject>("H");
        GameObject settingsPrefab = settingsShortcutPrefab != null
            ? settingsShortcutPrefab
            : Resources.Load<GameObject>("X");

        if (savePrefab == null || settingsPrefab == null)
        {
            Debug.LogError("快捷键提示：未找到 Resources/H.prefab 或 Resources/X.prefab。", this);
            return;
        }

        shortcutCanvasObject = new GameObject(
            "GameplayMenuShortcutCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        shortcutCanvasObject.transform.SetParent(transform, false);

        Canvas canvas = shortcutCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortingOrder;

        CanvasScaler scaler = shortcutCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        saveShortcutInstance = Instantiate(savePrefab, shortcutCanvasObject.transform, false);
        saveShortcutInstance.name = "Save Shortcut (H)";
        settingsShortcutInstance = Instantiate(settingsPrefab, shortcutCanvasObject.transform, false);
        settingsShortcutInstance.name = "Settings Shortcut (X)";
        PlaceAtBottomRight(saveShortcutInstance, shortcutSpacing);
        PlaceAtBottomRight(settingsShortcutInstance, 0f);

        Button saveButton = ResolveButton(saveShortcutInstance);
        Button settingsButton = ResolveButton(settingsShortcutInstance);
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(ToggleSaveMenu);
            saveButton.onClick.AddListener(ToggleSaveMenu);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(ToggleSettings);
            settingsButton.onClick.AddListener(ToggleSettings);
        }

        saveShortcutInstance.SetActive(false);
        settingsShortcutInstance.SetActive(false);
        Debug.Log("[GameplayMenuShortcuts] H/X UI created.", this);
    }

    private void PlaceAtBottomRight(GameObject prefabInstance, float verticalOffset)
    {
        if (prefabInstance == null || !prefabInstance.TryGetComponent(out RectTransform rect))
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(
            -shortcutMargin.x,
            shortcutMargin.y + verticalOffset);
        rect.localScale = Vector3.one;
    }

    private static Button ResolveButton(GameObject prefabInstance)
    {
        Button button = prefabInstance.GetComponent<Button>();
        return button != null ? button : prefabInstance.GetComponentInChildren<Button>(true);
    }

    private void ToggleSaveMenu()
    {
        SaveMenuController.Instance?.ToggleFromShortcut();
    }

    private void ToggleSettings()
    {
        SettingsOverlayController.Instance?.ToggleFromShortcut();
    }
}