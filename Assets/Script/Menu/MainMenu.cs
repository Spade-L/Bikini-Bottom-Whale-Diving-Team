using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 管理主菜单按钮、性别选择和场景进入
public class MainMenu : MonoBehaviour
{
// 配置 场景设置 分组
    [Header("场景设置")]
    [SerializeField] private int gameSceneIndex = 1;

// 配置 性别选择面板 分组
    [Header("性别选择面板")]
    [Tooltip("点击开始游戏后弹出的面板（含两个按钮：寻找哥哥/寻找姐姐），默认隐藏")]
// 保存 genderSelectPanel 引用
    [SerializeField] private GameObject genderSelectPanel;

// 配置 主菜单按钮反馈 分组
    [Header("主菜单按钮反馈")]
    [SerializeField] private Button startButton;
// 保存 startBImage 引用
    [SerializeField] private GameObject startBImage;
    [SerializeField] private Texture2D startBTexture;
    [SerializeField] private Button saveButton;
// 保存 saveBImage 引用
    [SerializeField] private GameObject saveBImage;
    [SerializeField] private Texture2D saveBTexture;
    [SerializeField] private Button settingsButton;
// 保存 settingsBImage 引用
    [SerializeField] private GameObject settingsBImage;
    [SerializeField] private Texture2D settingsBTexture;
    [SerializeField] private SettingsOverlayController settingsOverlay;

// 配置 真结局主菜单 分组
    [Header("真结局主菜单")]
    [SerializeField] private GameObject[] normalEndingMenuObjects;
    [SerializeField] private GameObject[] trueEndingMenuObjects;
    [SerializeField] private Button trueEndingStartButton;
    [SerializeField] private Button trueEndingSaveButton;
    [SerializeField] private Button trueEndingSettingsButton;

// 记录 MainMenu 的当前状态
    private bool transitionInProgress;
    private bool trueEndingMenuActive;
    private GameObject startBFeedback;
    private GameObject saveBFeedback;
    private GameObject settingsBFeedback;

// 读取初始依赖并同步首帧状态
    private void Start()
    {
        ApplyEndingMenuState();
        InitializeButtonFeedbackImages();

// 检查 Start 的前置条件
        if (genderSelectPanel != null)
        {
// 切换 Start 的显示状态
            genderSelectPanel.SetActive(false);
        }

// 推进 Start 中的必要步骤
        HideButtonFeedbackImages();
    }

    // 处理 StartGame 对应逻辑
    public void StartGame()
    {
        if (trueEndingMenuActive)
        {
            StartGameAfterFeedback();
            return;
        }

// 推进 StartGame 中的必要步骤
        BeginButtonFeedback(startBFeedback, StartGameAfterFeedback);
    }

    // 处理 OpenSave 对应逻辑
    public void OpenSave()
    {
        if (trueEndingMenuActive)
        {
            OpenSaveDirect();
            return;
        }

// 推进 OpenSave 中的必要步骤
        BeginButtonFeedback(saveBFeedback, OpenSaveDirect);
    }

    private void OpenSaveDirect()
    {
        SaveMenuController target = SaveMenuController.Instance;
        if (target != null)
        {
            target.Open(true);
        }
        else
        {
            Debug.LogWarning("主菜单：未找到存档页面控制器。");
        }
    }

    // 处理 OpenSettings 对应逻辑
    public void OpenSettings()
    {
        if (trueEndingMenuActive)
        {
            OpenSettingsDirect();
            return;
        }

// 推进 OpenSettings 中的必要步骤
        BeginButtonFeedback(settingsBFeedback, OpenSettingsDirect);
    }

    private void OpenSettingsDirect()
    {
        SettingsOverlayController target = settingsOverlay != null
            ? settingsOverlay
            : SettingsOverlayController.Instance;
        if (target != null)
        {
            target.Open();
        }
        else
        {
            Debug.LogWarning("主菜单：未找到设置页面控制器。");
        }
    }

// 隐藏 HideButtonFeedbackImages 对应界面
    private void HideButtonFeedbackImages()
    {
// 检查 HideButtonFeedbackImages 的前置条件
        if (startBFeedback != null)
        {
// 切换 HideButtonFeedbackImages 的显示状态
            startBFeedback.SetActive(false);
        }

// 检查 HideButtonFeedbackImages 的前置条件（HideButtonFeedbackImages）
        if (saveBFeedback != null)
        {
// 切换 HideButtonFeedbackImages 的显示状态（HideButtonFeedbackImages 后续步骤）
            saveBFeedback.SetActive(false);
        }

// 检查 HideButtonFeedbackImages 的前置条件（HideButtonFeedbackImages）（if）
        if (settingsBFeedback != null)
        {
// 切换 HideButtonFeedbackImages 的显示状态（HideButtonFeedbackImages 后续步骤）（125）
            settingsBFeedback.SetActive(false);
        }
    }

    private void ApplyEndingMenuState()
    {
        bool trueEndingUnlocked = PlayerPrefs.GetInt(EndingGate.TrueEndingCompletedPlayerPrefsKey, 0) == 1
            || (GameManager.Instance != null && GameManager.Instance.HasFlag("truth_revealed"));
        trueEndingMenuActive = trueEndingUnlocked;

        if (normalEndingMenuObjects == null || normalEndingMenuObjects.Length == 0)
        {
            normalEndingMenuObjects = ResolveMenuObjects(
                "Menu-Adjusted",
                "start",
                "setting",
                "save");
        }

        if (trueEndingMenuObjects == null || trueEndingMenuObjects.Length == 0)
        {
            trueEndingMenuObjects = ResolveMenuObjects(
                "Menu-Adjusted (1)",
                "start (1)",
                "setting (1)",
                "save (1)");
        }

        SetMenuObjectsActive(normalEndingMenuObjects, !trueEndingUnlocked);
        SetMenuObjectsActive(trueEndingMenuObjects, trueEndingUnlocked);

        if (trueEndingStartButton == null) trueEndingStartButton = ResolveMenuButton("start (1)");
        if (trueEndingSaveButton == null) trueEndingSaveButton = ResolveMenuButton("save (1)");
        if (trueEndingSettingsButton == null) trueEndingSettingsButton = ResolveMenuButton("setting (1)");
    }

    private static GameObject[] ResolveMenuObjects(params string[] objectNames)
    {
        GameObject[] result = new GameObject[objectNames.Length];
        for (int i = 0; i < objectNames.Length; i++)
        {
            result[i] = FindInActiveScene(objectNames[i]);
        }

        return result;
    }

    private static Button ResolveMenuButton(string objectName)
    {
        GameObject target = FindInActiveScene(objectName);
        if (target == null)
        {
            return null;
        }

        Button button = target.GetComponent<Button>();
        return button != null ? button : target.GetComponentInChildren<Button>(true);
    }

    private static void SetMenuObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        foreach (GameObject target in objects)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }

    private static GameObject FindInActiveScene(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = FindChild(root.transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindChild(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root.gameObject;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject found = FindChild(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void InitializeButtonFeedbackImages()
    {
        startBFeedback = CreateButtonFeedback(startBTexture, startBImage, "Start B Feedback");
        saveBFeedback = CreateButtonFeedback(saveBTexture, saveBImage, "Save B Feedback");
        settingsBFeedback = CreateButtonFeedback(settingsBTexture, settingsBImage, "Settings B Feedback");

        if (startBImage != null) startBImage.SetActive(false);
        if (saveBImage != null) saveBImage.SetActive(false);
        if (settingsBImage != null) settingsBImage.SetActive(false);
    }

    private GameObject CreateButtonFeedback(Texture2D texture, GameObject source, string objectName)
    {
        if (texture == null)
        {
            return null;
        }

        GameObject feedbackObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = feedbackObject.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.SetAsLastSibling();

        CreateIllustrationImages(rect, source);

        GameObject overlayObject = new GameObject(
            "B Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.SetParent(rect, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = Vector2.zero;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);

        RawImage image = overlayObject.GetComponent<RawImage>();
        image.texture = texture;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        image.color = Color.white;
        image.raycastTarget = false;

        feedbackObject.SetActive(false);
        return feedbackObject;
    }

    private void CreateIllustrationImages(RectTransform parent, GameObject source)
    {
        if (source == null)
        {
            return;
        }

        float sourceScale = source.transform.localScale.x;
        SpriteRenderer[] renderers = source.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.transform == source.transform || renderer.sprite == null)
            {
                continue;
            }

            CreateIllustrationImage(parent, renderer, sourceScale);
        }
    }

    private void CreateIllustrationImage(RectTransform parent, SpriteRenderer source, float sourceScale)
    {
        GameObject illustrationObject = new GameObject(
            source.name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform rect = illustrationObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Vector3 localPosition = source.transform.localPosition;
        rect.anchoredPosition = new Vector2(localPosition.x, localPosition.y) * sourceScale;

        Vector3 spriteSize = source.sprite.bounds.size;
        rect.sizeDelta = new Vector2(spriteSize.x, spriteSize.y) * sourceScale;

        Image image = illustrationObject.GetComponent<Image>();
        image.sprite = source.sprite;
        image.color = source.color;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }


    private void StartGameAfterFeedback()
    {
// 检查 StartGameAfterFeedback 的前置条件
        if (genderSelectPanel != null)
        {
// 推进 StartGameAfterFeedback 中的必要步骤
            ShowPanelWithFade(true);
        }
// 处理 StartGameAfterFeedback 的备用分支
        else
        {
// 推进 StartGameAfterFeedback 中的必要步骤（StartGameAfterFeedback）
            StartAsMale();
        }
    }

    private void BeginButtonFeedback(GameObject feedbackImage, Action afterDelay)
    {
// 检查 BeginButtonFeedback 的前置条件
        if (transitionInProgress)
        {
// 返回 BeginButtonFeedback 的处理结果
            return;
        }

// 启动当前协程
        StartCoroutine(ButtonFeedbackRoutine(feedbackImage, afterDelay));
    }

// 处理 ButtonFeedbackRoutine 对应逻辑
    private IEnumerator ButtonFeedbackRoutine(GameObject feedbackImage, Action afterDelay)
    {
        transitionInProgress = true;
// 推进 ButtonFeedbackRoutine 中的必要步骤
        SetTopLevelButtonsInteractable(false);
        HideButtonFeedbackImages();

// 检查 ButtonFeedbackRoutine 的前置条件
        if (feedbackImage != null)
        {
// 切换 ButtonFeedbackRoutine 的显示状态
            feedbackImage.SetActive(true);
        }

// 等待下一步
        yield return new WaitForSecondsRealtime(0.5f);
        afterDelay?.Invoke();

        // FadeOutIn 在下一帧才会启动其内部协程，先让出一帧再检查渐变状态
        yield return null;

        // Start 的面板切换或场景淡出完成前保持锁定，避免重复反馈和重复转场
        while (isActiveAndEnabled && ScreenFader.IsFading)
        {
// 在 ButtonFeedbackRoutine 中继续当前处理
            yield return null;
        }

// 检查 ButtonFeedbackRoutine 的前置条件（ButtonFeedbackRoutine）
        if (!isActiveAndEnabled)
        {
// 无法继续时结束 ButtonFeedbackRoutine
            yield break;
        }

        // 存档页面属于独立模态层：菜单按钮保持禁用，直到页面关闭
        while (isActiveAndEnabled && SaveMenuController.Instance != null && SaveMenuController.Instance.IsOpen)
        {
// 在 ButtonFeedbackRoutine 中继续当前处理（ButtonFeedbackRoutine 后续步骤）
            yield return null;
        }

        if (!isActiveAndEnabled)
        {
// 在 ButtonFeedbackRoutine 中处理 无法继续时结束 ButtonFeedbackRoutine
            yield break;
        }

// 推进 ButtonFeedbackRoutine 中的必要步骤（ButtonFeedbackRoutine）
        HideButtonFeedbackImages();
        SetTopLevelButtonsInteractable(true);
// 同步 ButtonFeedbackRoutine 的状态
        transitionInProgress = false;
    }

// 设置 SetTopLevelButtonsInteractable 的目标状态
    private void SetTopLevelButtonsInteractable(bool interactable)
    {
// 检查 SetTopLevelButtonsInteractable 的前置条件
        if (startButton != null)
        {
// 同步 SetTopLevelButtonsInteractable 的内部状态
            startButton.interactable = interactable;
        }

// 检查 SetTopLevelButtonsInteractable 的前置条件（SetTopLevelButtonsInteractable）
        if (saveButton != null)
        {
// 同步 SetTopLevelButtonsInteractable 的内部状态（SetTopLevelButtonsInteractable）
            saveButton.interactable = interactable;
        }

        if (settingsButton != null)
        {
// 同步 SetTopLevelButtonsInteractable 的内部状态（SetTopLevelButtonsInteractable）（settingsButton）
            settingsButton.interactable = interactable;
        }

        if (trueEndingStartButton != null)
        {
            trueEndingStartButton.interactable = interactable;
        }

        if (trueEndingSaveButton != null)
        {
            trueEndingSaveButton.interactable = interactable;
        }

        if (trueEndingSettingsButton != null)
        {
            trueEndingSettingsButton.interactable = interactable;
        }
    }

    // 处理 StartAsMale 对应逻辑
    public void StartAsMale()
    {
// 同步 StartAsMale 的内部状态
        GameManager.PendingFemaleSelection = false;
        GameManager.Instance?.ResetRuntimeState();
// 推进 StartAsMale 中的必要步骤
        LoadGameScene();
    }

    // 处理 StartAsFemale 对应逻辑
    public void StartAsFemale()
    {
// 同步 StartAsFemale 的内部状态
        GameManager.PendingFemaleSelection = true;
        GameManager.Instance?.ResetRuntimeState();
// 推进 StartAsFemale 中的必要步骤
        LoadGameScene();
    }

    // 判断 CancelGenderSelect 对应条件
    public void CancelGenderSelect()
    {
// 检查 CancelGenderSelect 的前置条件
        if (transitionInProgress)
        {
// 返回 CancelGenderSelect 的处理结果
            return;
        }

// 推进 CancelGenderSelect 中的必要步骤
        ShowPanelWithFade(false);
    }

// 显示 ShowPanelWithFade 对应界面
    private void ShowPanelWithFade(bool show)
    {
        if (genderSelectPanel == null)
        {
// 返回 ShowPanelWithFade 的处理结果
            return;
        }

// 记录 ShowPanelWithFade 的当前状态
        bool alreadyLocked = transitionInProgress;
        transitionInProgress = true;
// 推进 ShowPanelWithFade 中的必要步骤
        SetTopLevelButtonsInteractable(false);

// 检查转场状态
        if (ScreenFader.Instance != null)
        {
// 切换 ShowPanelWithFade 的显示状态
            ScreenFader.Instance.FadeOutIn(() => genderSelectPanel.SetActive(show), 0.3f);
        }
// 处理 ShowPanelWithFade 的备用分支
        else
        {
// 切换 ShowPanelWithFade 的显示状态（ShowPanelWithFade 后续步骤）
            genderSelectPanel.SetActive(show);
        }

        // Start 的反馈协程负责等待并释放锁；取消按钮没有外层反馈协程，需要由这里负责
        if (!alreadyLocked)
        {
            // 启动当前异步流程
            StartCoroutine(ReleaseTransitionAfterFade());
        }
    }

// 处理 ReleaseTransitionAfterFade 对应逻辑
    private IEnumerator ReleaseTransitionAfterFade()
    {
// 在 ReleaseTransitionAfterFade 中继续当前处理
        yield return WaitForFadeToFinish();

// 检查 ReleaseTransitionAfterFade 的前置条件
        if (!isActiveAndEnabled)
        {
            yield break;
        }

// 推进 ReleaseTransitionAfterFade 中的必要步骤
        SetTopLevelButtonsInteractable(true);
        transitionInProgress = false;
    }

// 处理 WaitForFadeToFinish 对应逻辑
    private IEnumerator WaitForFadeToFinish()
    {
// WaitForFadeToFinish 缺少引用时提前结束
        if (ScreenFader.Instance == null)
        {
// 无法继续时结束 WaitForFadeToFinish
            yield break;
        }

        // FadeOutIn 在下一帧启动协程，先等待到渐变真正开始，再等待它结束
        yield return null;
        while (isActiveAndEnabled && !ScreenFader.IsFading)
        {
// 在 WaitForFadeToFinish 中继续当前处理
            yield return null;
        }

// 等待条件变化
        while (isActiveAndEnabled && ScreenFader.IsFading)
        {
// 在 WaitForFadeToFinish 中继续当前处理（WaitForFadeToFinish 后续步骤）
            yield return null;
        }
    }

// 加载 LoadGameScene 对应数据
    private void LoadGameScene()
    {
// 在 LoadGameScene 中继续当前处理
        if (ScreenFader.IsFading)
        {
// 返回 LoadGameScene 的处理结果
            return;
        }

// 同步 LoadGameScene 的内部状态
        transitionInProgress = true;
        SetTopLevelButtonsInteractable(false);

// 在 LoadGameScene 中继续当前处理（LoadGameScene 后续步骤）
        if (ScreenFader.Instance != null)
        {
// 执行场景切换
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(gameSceneIndex));
        }
// 处理 LoadGameScene 的备用分支
        else
        {
// 在 LoadGameScene 中继续当前处理（LoadGameScene 后续步骤）（375）
            SceneManager.LoadScene(gameSceneIndex);
        }
    }

    public void QuitGame()
    {
// 使用 QuitGame 所需功能
        Application.Quit();
        Debug.Log("游戏已退出");
    }
}
