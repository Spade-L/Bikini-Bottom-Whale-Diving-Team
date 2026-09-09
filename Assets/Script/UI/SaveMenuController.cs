using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveMenuController : MonoBehaviour
{
    public static SaveMenuController Instance { get; private set; }

    [Header("存档预制体")]
    [SerializeField] private GameObject savePrefab;

    private GameObject saveView;
    private readonly Dictionary<int, Button> slotButtons = new Dictionary<int, Button>();
    private readonly Dictionary<int, TMP_Text> slotSceneTexts = new Dictionary<int, TMP_Text>();
    private readonly Dictionary<int, TMP_Text> slotTimeTexts = new Dictionary<int, TMP_Text>();

    private TMP_Text nowSelectText;
    private Button saveButton;
    private Button loadButton;
    private Button backButton;
    private Selectable firstSelectable;

    private IDisposable movementLock;
    private IDisposable interactionLock;
    private SaveData pendingLoad;
    private int pendingLoadSlot;
    private int selectedSlot;
    private bool isOpen;
    private bool loadOnly;
    private bool transitionInProgress;
    private bool listenersRegistered;

    public bool IsOpen => isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            new GameObject("SaveMenu").AddComponent<SaveMenuController>();
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
        CreateSaveView();
        Close();
    }

    private void Start()
    {
        RegisterListeners();
    }

    private void CreateSaveView()
    {
        if (saveView != null) return;

        if (savePrefab == null)
        {
            savePrefab = Resources.Load<GameObject>("Save");
        }

        if (savePrefab == null)
        {
            Debug.LogError("存档页面：未找到 Save.prefab。请在 Inspector 指定 savePrefab，或将其放入 Assets/Resources/Save.prefab。");
            return;
        }

        saveView = Instantiate(savePrefab, transform);
        saveView.name = "SaveView";
        saveView.transform.localPosition = Vector3.zero;
        saveView.transform.localRotation = Quaternion.identity;
        saveView.transform.localScale = Vector3.one;

        nowSelectText = FindText(saveView.transform, "NowSelect");
        saveButton = FindButton(saveView.transform, "SaveGame");
        loadButton = FindButton(saveView.transform, "LoadGame");
        backButton = FindButton(saveView.transform, "BackGame", "Close", "Back", "Return");

        for (int slot = SaveSystem.MinSlot; slot <= SaveSystem.MaxSlot; slot++)
        {
            Transform slotRoot = FindTransform(saveView.transform, $"Save{slot}");
            if (slotRoot == null)
            {
                Debug.LogWarning($"存档页面：未找到 Save{slot}。");
                continue;
            }

            Button slotButton = slotRoot.GetComponent<Button>() ?? slotRoot.GetComponentInChildren<Button>(true);
            TMP_Text sceneText = FindText(slotRoot, "SaveWhere");
            TMP_Text timeText = FindText(slotRoot, "SaveTime");

            slotButtons[slot] = slotButton;
            slotSceneTexts[slot] = sceneText;
            slotTimeTexts[slot] = timeText;
            if (firstSelectable == null && slotButton != null) firstSelectable = slotButton;

            if (slotButton == null) Debug.LogWarning($"存档页面：Save{slot} 未找到 Button。");
            if (sceneText == null) Debug.LogWarning($"存档页面：Save{slot} 未找到 SaveWhere 文本。");
            if (timeText == null) Debug.LogWarning($"存档页面：Save{slot} 未找到 SaveTime 文本。");
        }

        if (firstSelectable == null) firstSelectable = loadButton != null ? loadButton : backButton;
    }

    private void RegisterListeners()
    {
        if (listenersRegistered) return;
        listenersRegistered = true;

        foreach (KeyValuePair<int, Button> pair in slotButtons)
        {
            int slot = pair.Key;
            pair.Value?.onClick.AddListener(() => SelectSlot(slot));
        }

        saveButton?.onClick.AddListener(SaveSelectedSlot);
        loadButton?.onClick.AddListener(LoadSelectedSlot);
        backButton?.onClick.AddListener(Close);
    }

    public void Open(bool openedFromMainMenu = false)
    {
        if (transitionInProgress) return;
        if (saveView == null)
        {
            CreateSaveView();
            RegisterListeners();
        }
        if (saveView == null) return;

        isOpen = true;
        loadOnly = openedFromMainMenu;
        selectedSlot = 0;
        pendingLoad = null;
        pendingLoadSlot = 0;
        UpdateSelectionText();
        RefreshSlots();
        if (saveButton != null) saveButton.interactable = !loadOnly;
        saveView.SetActive(true);

        movementLock = GameplayInputLock.AcquireMovementLock();
        interactionLock = GameplayInputLock.AcquireInteractionLock();
        if (firstSelectable != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }
    }

    public void Close()
    {
        if (transitionInProgress) return;
        isOpen = false;
        loadOnly = false;
        selectedSlot = 0;
        pendingLoad = null;
        pendingLoadSlot = 0;
        if (saveView != null) saveView.SetActive(false);
        ReleaseLocks();
    }

    public void SelectSlot(int slot)
    {
        if (!isOpen || !SaveSystem.IsValidSlot(slot)) return;
        selectedSlot = slot;
        UpdateSelectionText();
        RefreshSlots();
    }

    public void SaveSelectedSlot()
    {
        if (!isOpen || loadOnly || transitionInProgress) return;
        if (!SaveSystem.IsValidSlot(selectedSlot))
        {
            Debug.LogWarning("存档页面：请先选择存档位。");
            return;
        }

        GameManager manager = GameManager.Instance;
        SaveData data = manager == null ? null : manager.CaptureSaveData();
        if (data == null)
        {
            Debug.LogWarning("存档页面：当前场景没有可保存的玩家状态。");
            return;
        }

        if (SaveSystem.Save(data, selectedSlot))
        {
            RefreshSlots();
            Close();
        }
    }

    public void LoadSelectedSlot()
    {
        if (!isOpen || transitionInProgress) return;
        if (!SaveSystem.IsValidSlot(selectedSlot))
        {
            Debug.LogWarning("存档页面：请先选择存档位。");
            return;
        }
        if (!SaveSystem.TryLoad(selectedSlot, out SaveData data))
        {
            Debug.LogWarning("存档页面：当前存档位没有有效记录。");
            RefreshSlots();
            return;
        }
        if (!IsLoadableScene(data.sceneName))
        {
            Debug.LogWarning($"存档页面：存档场景不可加载 ({data.sceneName})。");
            return;
        }

        transitionInProgress = true;
        pendingLoad = data;
        pendingLoadSlot = selectedSlot;
        AcquireLocks();
        isOpen = false;
        if (saveView != null) saveView.SetActive(false);

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(data.sceneName));
        }
        else
        {
            SceneManager.LoadScene(data.sceneName);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!transitionInProgress || pendingLoad == null) return;
        if (!scene.name.Equals(pendingLoad.sceneName, StringComparison.Ordinal)) return;
        StartCoroutine(RestoreAfterSceneLoad(scene, pendingLoad));
    }

    private IEnumerator RestoreAfterSceneLoad(Scene scene, SaveData data)
    {
        PlayerMovement2D player = null;
        float timeout = 5f;
        while (player == null && timeout > 0f)
        {
            player = FindFirstObjectByType<PlayerMovement2D>();
            timeout -= Time.unscaledDeltaTime;
            if (player == null) yield return null;
        }

        if (GameManager.Instance != null) GameManager.Instance.RestoreSaveData(data);
        if (player != null)
        {
            player.transform.position = new Vector3(data.playerX, data.playerY, player.transform.position.z);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
        }
        else
        {
            Debug.LogWarning("存档页面：目标场景加载后未找到玩家，已恢复全局状态但未恢复位置。");
        }

        pendingLoad = null;
        pendingLoadSlot = 0;
        transitionInProgress = false;
        isOpen = false;
        ReleaseLocks();
    }

    private void RefreshSlots()
    {
        for (int slot = SaveSystem.MinSlot; slot <= SaveSystem.MaxSlot; slot++)
        {
            SaveData data;
            bool available = SaveSystem.TryLoad(slot, out data);
            if (slotSceneTexts.TryGetValue(slot, out TMP_Text sceneText) && sceneText != null)
            {
                sceneText.text = available ? data.sceneName : "Null";
            }
            if (slotTimeTexts.TryGetValue(slot, out TMP_Text timeText) && timeText != null)
            {
                timeText.text = available && TryFormatSavedTime(data, out string text) ? text : "Null";
            }
            if (slotButtons.TryGetValue(slot, out Button button) && button != null)
            {
                button.interactable = true;
            }
        }
    }

    private bool TryFormatSavedTime(SaveData data, out string text)
    {
        text = null;
        if (data == null || !DateTime.TryParse(data.savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime utc))
        {
            return false;
        }
        DateTime local = utc.ToLocalTime();
        text = local.ToString("yyyy年MM月dd日 HH时mm分ss秒");
        return true;
    }

    private void UpdateSelectionText()
    {
        if (nowSelectText != null)
        {
            nowSelectText.text = selectedSlot > 0 ? $"当前选择：Save {selectedSlot}" : "当前选择：Null";
        }
    }

    private bool IsLoadableScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || sceneName.Equals("Menu", StringComparison.OrdinalIgnoreCase) || sceneName.Equals("MainMenu", StringComparison.OrdinalIgnoreCase)) return false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path).Equals(sceneName, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private void AcquireLocks()
    {
        movementLock ??= GameplayInputLock.AcquireMovementLock();
        interactionLock ??= GameplayInputLock.AcquireInteractionLock();
    }

    private void ReleaseLocks()
    {
        movementLock?.Dispose();
        interactionLock?.Dispose();
        movementLock = null;
        interactionLock = null;
    }

    private static Transform FindTransform(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }

    private static Button FindButton(Transform root, params string[] names)
    {
        foreach (string name in names)
        {
            Transform target = FindTransform(root, name);
            Button button = target == null ? null : target.GetComponent<Button>();
            if (button != null) return button;
        }
        return null;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        Transform target = FindTransform(root, name);
        return target == null ? null : target.GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleaseLocks();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleaseLocks();
        if (Instance == this) Instance = null;
    }
}

