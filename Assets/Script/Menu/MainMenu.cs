using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private int gameSceneIndex = 1;

    [Header("性别选择面板")]
    [Tooltip("点击开始游戏后弹出的面板（含两个按钮：寻找哥哥/寻找姐姐），默认隐藏")]
    [SerializeField] private GameObject genderSelectPanel;

    private void Start()
    {
        if (genderSelectPanel != null)
        {
            genderSelectPanel.SetActive(false);
        }
    }

    /// <summary>开始按钮：黑幕过渡弹出性别选择；没配面板则直接以默认（哥哥线）开始。</summary>
    public void StartGame()
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
        ShowPanelWithFade(false);
    }

    private void ShowPanelWithFade(bool show)
    {
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOutIn(() => genderSelectPanel.SetActive(show), 0.3f);
        }
        else
        {
            genderSelectPanel.SetActive(show);
        }
    }

    private void LoadGameScene()
    {
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
