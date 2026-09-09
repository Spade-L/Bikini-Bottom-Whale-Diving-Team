using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsOverlayController : MonoBehaviour
{
    public static SettingsOverlayController Instance { get; private set; }

    [Header("设置预制体")]
    [SerializeField] private GameObject settingsPrefab;

    private GameObject settingsView;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Button switchWindowModeButton;
    private Button quitGameButton;
    private Button closeButton;
    private Button saveGameButton;
    private Selectable firstSelectable;
    private GameObject fullscreenLabel;
    private GameObject windowedLabel;
    private GameObject borderlessLabel;

    private System.IDisposable movementLock;
    private System.IDisposable interactionLock;
    private bool isOpen;
    private bool listenersRegistered;
    private int gameplaySceneIndex = 1;

    public bool IsOpen => isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            new GameObject("SettingsOverlay").AddComponent<SettingsOverlayController>();
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
        CreateSettingsView();
        Close();
    }

    private void Start()
    {
        RegisterListeners();
    }

    private void Update()
    {
        if (!IsGameplayScene() || !Input.GetKeyDown(KeyCode.X)) return;
        if (isOpen) Close();
        else Open();
    }

    private bool IsGameplayScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.IsValid()
            && scene.buildIndex == gameplaySceneIndex
            && !scene.name.Equals("Menu", System.StringComparison.OrdinalIgnoreCase)
            && !scene.name.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase);
    }

    private void CreateSettingsView()
    {
        if (settingsView != null) return;

        if (settingsPrefab == null)
        {
            settingsPrefab = Resources.Load<GameObject>("Settings");
        }

        if (settingsPrefab == null)
        {
            Debug.LogError("设置页面：未找到 Settings.prefab。请将预制体放入 Assets/Resources/Settings.prefab，或在 Inspector 中指定 settingsPrefab。");
            return;
        }

        settingsView = Instantiate(settingsPrefab, transform);
        settingsView.name = "SettingsView";
        settingsView.transform.localPosition = Vector3.zero;
        settingsView.transform.localRotation = Quaternion.identity;
        settingsView.transform.localScale = Vector3.one;

        musicSlider = FindComponent<Slider>("MusicSlider");
        sfxSlider = FindComponent<Slider>("SoundSlider");
        switchWindowModeButton = FindButton("Switch window mode");
        quitGameButton = FindButton("QuitGame");
        saveGameButton = FindButton("SaveGame");
        closeButton = FindButton("BackGame", "Close", "Back", "Return", "X", "CloseButton");
        firstSelectable = musicSlider != null ? musicSlider : switchWindowModeButton;

        fullscreenLabel = FindObject("Full screen", "Fullscreen");
        windowedLabel = FindObject("Windowed");
        borderlessLabel = FindObject("Windowed fullscreen", "Borderless");
    }

    private void RegisterListeners()
    {
        if (listenersRegistered) return;
        listenersRegistered = true;
        musicSlider?.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider?.onValueChanged.AddListener(SetSfxVolume);
        switchWindowModeButton?.onClick.AddListener(SwitchWindowMode);
        quitGameButton?.onClick.AddListener(QuitGame);
        saveGameButton?.onClick.AddListener(OpenSaveMenu);
        closeButton?.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (isOpen || SettingsManager.Instance == null) return;
        if (settingsView == null)
        {
            CreateSettingsView();
            RegisterListeners();
        }
        if (settingsView == null) return;

        isOpen = true;
        SettingsManager settings = SettingsManager.Instance;
        musicSlider?.SetValueWithoutNotify(settings.MusicVolume);
        sfxSlider?.SetValueWithoutNotify(settings.SfxVolume);
        RefreshDisplayModeLabel(settings.DisplayMode);
        settingsView.SetActive(true);

        movementLock = GameplayInputLock.AcquireMovementLock();
        interactionLock = GameplayInputLock.AcquireInteractionLock();
        if (firstSelectable != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }
    }

    public void Close()
    {
        isOpen = false;
        if (settingsView != null) settingsView.SetActive(false);
        movementLock?.Dispose();
        interactionLock?.Dispose();
        movementLock = null;
        interactionLock = null;
    }

    public void SetMusicVolume(float value)
    {
        SettingsManager.Instance?.SetMusicVolume(value);
    }

    public void SetSfxVolume(float value)
    {
        SettingsManager.Instance?.SetSfxVolume(value);
    }

    public void SwitchWindowMode()
    {
        if (SettingsManager.Instance == null) return;
        int next = ((int)SettingsManager.Instance.DisplayMode + 1) % 3;
        SettingsManager.Instance.SetDisplayMode((GameDisplayMode)next);
        RefreshDisplayModeLabel((GameDisplayMode)next);
    }

    public void SetDisplayMode(int value)
    {
        GameDisplayMode mode = (GameDisplayMode)Mathf.Clamp(value, 0, 2);
        SettingsManager.Instance?.SetDisplayMode(mode);
        RefreshDisplayModeLabel(mode);
    }

    private void RefreshDisplayModeLabel(GameDisplayMode mode)
    {
        if (fullscreenLabel != null) fullscreenLabel.SetActive(mode == GameDisplayMode.Fullscreen);
        if (windowedLabel != null) windowedLabel.SetActive(mode == GameDisplayMode.Windowed);
        if (borderlessLabel != null) borderlessLabel.SetActive(mode == GameDisplayMode.Borderless);
    }

    public void QuitGame()
    {
        PlayerPrefs.Save();
        Application.Quit();
#if UNITY_EDITOR
        Debug.Log("设置页面：编辑器中无法退出游戏。");
#endif
    }

    public void OpenSaveMenu()
    {
        SaveMenuController target = SaveMenuController.Instance;
        if (target == null)
        {
            Debug.LogWarning("设置页面：未找到存档页面控制器。");
            return;
        }

        Close();
        target.Open(false);
    }

    private T FindComponent<T>(params string[] names) where T : Component
    {
        GameObject target = FindObject(names);
        return target == null ? null : target.GetComponent<T>();
    }

    private Button FindButton(params string[] names)
    {
        return FindComponent<Button>(names);
    }

    private GameObject FindObject(params string[] names)
    {
        if (settingsView == null) return null;
        Transform[] children = settingsView.GetComponentsInChildren<Transform>(true);
        foreach (string name in names)
        {
            foreach (Transform child in children)
            {
                if (child.name == name) return child.gameObject;
            }
        }
        return null;
    }

    private void OnDisable()
    {
        if (Instance == this) Close();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        movementLock?.Dispose();
        interactionLock?.Dispose();
    }
}
