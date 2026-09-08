using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private int gameSceneIndex = 1;

    [Header("性别选择面板")]
    [Tooltip("点击开始游戏后弹出的面板（含两个按钮：寻找哥哥/寻找姐姐），默认隐藏")]
    [SerializeField] private GameObject genderSelectPanel;

    [Header("主菜单按钮反馈")]
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject startBImage;
    [SerializeField] private Button saveButton;
    [SerializeField] private GameObject saveBImage;
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsBImage;

    private bool transitionInProgress;

    private void Start()
    {
        if (genderSelectPanel != null)
        {
            genderSelectPanel.SetActive(false);
        }

        HideButtonFeedbackImages();
    }

    /// <summary>开始按钮：先显示按下图，停留 0.5 秒后弹出性别选择。</summary>
    public void StartGame()
    {
        BeginButtonFeedback(startBImage, StartGameAfterFeedback);
    }

    /// <summary>存档按钮的安全占位入口。未配置真实存档界面前不执行存档业务。</summary>
    public void OpenSave()
    {
        BeginButtonFeedback(saveBImage, () =>
        {
            Debug.Log("主菜单：Save 尚未配置存档界面或存档操作，已返回菜单。");
        });
    }

    /// <summary>设置按钮的安全占位入口。未配置真实设置界面前不打开猜测的面板。</summary>
    public void OpenSettings()
    {
        BeginButtonFeedback(settingsBImage, () =>
        {
            Debug.Log("主菜单：Settings 尚未配置设置界面，已返回菜单。");
        });
    }

    private void HideButtonFeedbackImages()
    {
        if (startBImage != null)
        {
            startBImage.SetActive(false);
        }

        if (saveBImage != null)
        {
            saveBImage.SetActive(false);
        }

        if (settingsBImage != null)
        {
            settingsBImage.SetActive(false);
        }
    }

    private void StartGameAfterFeedback()
    {
        if (genderSelectPanel != null)
        {
            ShowPanelWithFade(true);
        }
        else
        {
            StartAsMale();
        }
    }

    private void BeginButtonFeedback(GameObject feedbackImage, Action afterDelay)
    {
        if (transitionInProgress)
        {
            return;
        }

        StartCoroutine(ButtonFeedbackRoutine(feedbackImage, afterDelay));
    }

    private IEnumerator ButtonFeedbackRoutine(GameObject feedbackImage, Action afterDelay)
    {
        transitionInProgress = true;
        SetTopLevelButtonsInteractable(false);
        HideButtonFeedbackImages();

        if (feedbackImage != null)
        {
            feedbackImage.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(0.5f);
        afterDelay?.Invoke();

        // FadeOutIn 在下一帧才会启动其内部协程，先让出一帧再检查渐变状态
        yield return null;

        // Start 的面板切换或场景淡出完成前保持锁定，避免重复反馈和重复转场
        while (isActiveAndEnabled && ScreenFader.IsFading)
        {
            yield return null;
        }

        if (!isActiveAndEnabled)
        {
            yield break;
        }

        HideButtonFeedbackImages();
        SetTopLevelButtonsInteractable(true);
        transitionInProgress = false;
    }

    private void SetTopLevelButtonsInteractable(bool interactable)
    {
        if (startButton != null)
        {
            startButton.interactable = interactable;
        }

        if (saveButton != null)
        {
            saveButton.interactable = interactable;
        }

        if (settingsButton != null)
        {
            settingsButton.interactable = interactable;
        }
    }

    /// <summary>「寻找哥哥」按钮（玩家为男性）。</summary>
    public void StartAsMale()
    {
        GameManager.PendingFemaleSelection = false;
        LoadGameScene();
    }

    /// <summary>「寻找姐姐」按钮（玩家为女性）。</summary>
    public void StartAsFemale()
    {
        GameManager.PendingFemaleSelection = true;
        LoadGameScene();
    }

    /// <summary>性别面板的返回按钮。</summary>
    public void CancelGenderSelect()
    {
        if (transitionInProgress)
        {
            return;
        }

        ShowPanelWithFade(false);
    }

    private void ShowPanelWithFade(bool show)
    {
        if (genderSelectPanel == null)
        {
            return;
        }

        bool alreadyLocked = transitionInProgress;
        transitionInProgress = true;
        SetTopLevelButtonsInteractable(false);

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOutIn(() => genderSelectPanel.SetActive(show), 0.3f);
        }
        else
        {
            genderSelectPanel.SetActive(show);
        }

        // Start 的反馈协程负责等待并释放锁；取消按钮没有外层反馈协程，需要由这里负责
        if (!alreadyLocked)
        {
            // 启动当前异步流程
            StartCoroutine(ReleaseTransitionAfterFade());
        }
    }

    private IEnumerator ReleaseTransitionAfterFade()
    {
        yield return WaitForFadeToFinish();

        if (!isActiveAndEnabled)
        {
            yield break;
        }

        SetTopLevelButtonsInteractable(true);
        transitionInProgress = false;
    }

    private IEnumerator WaitForFadeToFinish()
    {
        if (ScreenFader.Instance == null)
        {
            yield break;
        }

        // FadeOutIn 在下一帧启动协程，先等待到渐变真正开始，再等待它结束
        yield return null;
        while (isActiveAndEnabled && !ScreenFader.IsFading)
        {
            yield return null;
        }

        while (isActiveAndEnabled && ScreenFader.IsFading)
        {
            yield return null;
        }
    }

    private void LoadGameScene()
    {
        if (ScreenFader.IsFading)
        {
            return;
        }

        transitionInProgress = true;
        SetTopLevelButtonsInteractable(false);

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(gameSceneIndex));
        }
        else
        {
            SceneManager.LoadScene(gameSceneIndex);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("游戏已退出");
    }
}
