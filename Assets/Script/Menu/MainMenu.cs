using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 定义 MainMenu 类型
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
    [SerializeField] private Button saveButton;
// 保存 saveBImage 引用
    [SerializeField] private GameObject saveBImage;
    [SerializeField] private Button settingsButton;
// 保存 settingsBImage 引用
    [SerializeField] private GameObject settingsBImage;
    [SerializeField] private SettingsOverlayController settingsOverlay;

// 记录 transitionInProgress 状态
    private bool transitionInProgress;

// 定义 Start 方法
    private void Start()
    {
// 判断当前条件
        if (genderSelectPanel != null)
        {
// 切换显示状态
            genderSelectPanel.SetActive(false);
        }

// 执行 HideButtonFeedbackImages
        HideButtonFeedbackImages();
    }

    /// <summary>开始按钮：先显示按下图，停留 0.5 秒后弹出性别选择。</summary>
    public void StartGame()
    {
// 执行 BeginButtonFeedback
        BeginButtonFeedback(startBImage, StartGameAfterFeedback);
    }

    /// <summary>从主菜单打开存档页面，只允许读取存档。</summary>
    public void OpenSave()
    {
// 执行 BeginButtonFeedback
        BeginButtonFeedback(saveBImage, () =>
        {
// 保存 target 数据
            SaveMenuController target = SaveMenuController.Instance;
            if (target != null)
            {
// 调用 Open
                target.Open(true);
            }
// 处理其他分支
            else
            {
// 输出调试信息
                Debug.LogWarning("主菜单：未找到存档页面控制器。");
            }
// 更新当前逻辑
        });
    }

    /// <summary>设置按钮的安全占位入口。未配置真实设置界面前不打开猜测的面板。</summary>
    public void OpenSettings()
    {
// 执行 BeginButtonFeedback
        BeginButtonFeedback(settingsBImage, () =>
        {
// 保存 target 数据
            SettingsOverlayController target = settingsOverlay != null
                ? settingsOverlay
// 更新当前逻辑
                : SettingsOverlayController.Instance;
            if (target != null)
            {
// 调用 Open
                target.Open();
            }
// 处理其他分支
            else
            {
// 输出调试信息
                Debug.LogWarning("主菜单：未找到设置页面控制器。");
            }
// 更新当前逻辑
        });
    }

// 定义 HideButtonFeedbackImages 方法
    private void HideButtonFeedbackImages()
    {
// 判断当前条件
        if (startBImage != null)
        {
// 切换显示状态
            startBImage.SetActive(false);
        }

// 判断当前条件
        if (saveBImage != null)
        {
// 切换显示状态
            saveBImage.SetActive(false);
        }

// 判断当前条件
        if (settingsBImage != null)
        {
// 切换显示状态
            settingsBImage.SetActive(false);
        }
    }

    private void StartGameAfterFeedback()
    {
// 判断当前条件
        if (genderSelectPanel != null)
        {
// 执行 ShowPanelWithFade
            ShowPanelWithFade(true);
        }
// 处理其他分支
        else
        {
// 执行 StartAsMale
            StartAsMale();
        }
    }

    private void BeginButtonFeedback(GameObject feedbackImage, Action afterDelay)
    {
// 判断当前条件
        if (transitionInProgress)
        {
// 返回当前结果
            return;
        }

// 启动当前协程
        StartCoroutine(ButtonFeedbackRoutine(feedbackImage, afterDelay));
    }

// 定义 ButtonFeedbackRoutine 方法
    private IEnumerator ButtonFeedbackRoutine(GameObject feedbackImage, Action afterDelay)
    {
        transitionInProgress = true;
// 执行 SetTopLevelButtonsInteractable
        SetTopLevelButtonsInteractable(false);
        HideButtonFeedbackImages();

// 判断当前条件
        if (feedbackImage != null)
        {
// 切换显示状态
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
// 等待下一步
            yield return null;
        }

// 判断当前条件
        if (!isActiveAndEnabled)
        {
// 保存 break 数据
            yield break;
        }

        // 存档页面属于独立模态层：菜单按钮保持禁用，直到页面关闭
        while (isActiveAndEnabled && SaveMenuController.Instance != null && SaveMenuController.Instance.IsOpen)
        {
// 等待下一步
            yield return null;
        }

        if (!isActiveAndEnabled)
        {
// 保存 break 数据
            yield break;
        }

// 执行 HideButtonFeedbackImages
        HideButtonFeedbackImages();
        SetTopLevelButtonsInteractable(true);
// 更新当前状态
        transitionInProgress = false;
    }

// 定义 SetTopLevelButtonsInteractable 方法
    private void SetTopLevelButtonsInteractable(bool interactable)
    {
// 判断当前条件
        if (startButton != null)
        {
// 更新当前状态
            startButton.interactable = interactable;
        }

// 判断当前条件
        if (saveButton != null)
        {
// 更新当前状态
            saveButton.interactable = interactable;
        }

        if (settingsButton != null)
        {
// 更新当前状态
            settingsButton.interactable = interactable;
        }
    }

    /// <summary>「寻找哥哥」按钮（玩家为男性）。</summary>
    public void StartAsMale()
    {
// 更新当前状态
        GameManager.PendingFemaleSelection = false;
        GameManager.Instance?.ResetRuntimeState();
// 执行 LoadGameScene
        LoadGameScene();
    }

    /// <summary>「寻找姐姐」按钮（玩家为女性）。</summary>
    public void StartAsFemale()
    {
// 更新当前状态
        GameManager.PendingFemaleSelection = true;
        GameManager.Instance?.ResetRuntimeState();
// 执行 LoadGameScene
        LoadGameScene();
    }

    /// <summary>性别面板的返回按钮。</summary>
    public void CancelGenderSelect()
    {
// 判断当前条件
        if (transitionInProgress)
        {
// 返回当前结果
            return;
        }

// 执行 ShowPanelWithFade
        ShowPanelWithFade(false);
    }

// 定义 ShowPanelWithFade 方法
    private void ShowPanelWithFade(bool show)
    {
        if (genderSelectPanel == null)
        {
// 返回当前结果
            return;
        }

// 记录 alreadyLocked 状态
        bool alreadyLocked = transitionInProgress;
        transitionInProgress = true;
// 执行 SetTopLevelButtonsInteractable
        SetTopLevelButtonsInteractable(false);

// 检查转场状态
        if (ScreenFader.Instance != null)
        {
// 切换显示状态
            ScreenFader.Instance.FadeOutIn(() => genderSelectPanel.SetActive(show), 0.3f);
        }
// 处理其他分支
        else
        {
// 切换显示状态
            genderSelectPanel.SetActive(show);
        }

        // Start 的反馈协程负责等待并释放锁；取消按钮没有外层反馈协程，需要由这里负责
        if (!alreadyLocked)
        {
            // 启动当前异步流程
            StartCoroutine(ReleaseTransitionAfterFade());
        }
    }

// 定义 ReleaseTransitionAfterFade 方法
    private IEnumerator ReleaseTransitionAfterFade()
    {
// 等待下一步
        yield return WaitForFadeToFinish();

// 判断当前条件
        if (!isActiveAndEnabled)
        {
            yield break;
        }

// 执行 SetTopLevelButtonsInteractable
        SetTopLevelButtonsInteractable(true);
        transitionInProgress = false;
    }

// 定义 WaitForFadeToFinish 方法
    private IEnumerator WaitForFadeToFinish()
    {
// 空引用时直接退出
        if (ScreenFader.Instance == null)
        {
// 保存 break 数据
            yield break;
        }

        // FadeOutIn 在下一帧启动协程，先等待到渐变真正开始，再等待它结束
        yield return null;
        while (isActiveAndEnabled && !ScreenFader.IsFading)
        {
// 等待下一步
            yield return null;
        }

// 等待条件变化
        while (isActiveAndEnabled && ScreenFader.IsFading)
        {
// 等待下一步
            yield return null;
        }
    }

// 定义 LoadGameScene 方法
    private void LoadGameScene()
    {
// 检查转场状态
        if (ScreenFader.IsFading)
        {
// 返回当前结果
            return;
        }

// 更新当前状态
        transitionInProgress = true;
        SetTopLevelButtonsInteractable(false);

// 检查转场状态
        if (ScreenFader.Instance != null)
        {
// 执行场景切换
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(gameSceneIndex));
        }
// 处理其他分支
        else
        {
// 执行场景切换
            SceneManager.LoadScene(gameSceneIndex);
        }
    }

    public void QuitGame()
    {
// 调用 Quit
        Application.Quit();
        Debug.Log("游戏已退出");
    }
}
