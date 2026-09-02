using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // 菜单生命周期与配置说明：
    // Start 只在组件启用后的首帧调用一次。
    // 因此面板初始状态在场景加载完成后统一设置。
    // genderSelectPanel 需在 Inspector 中绑定对应面板对象。
    // 未绑定面板时开始按钮会采用默认的男性分支。
    // 面板对象本身可以初始激活，Start 会将其隐藏。
    // StartGame、StartAsMale 和 StartAsFemale 可绑定到 Button.OnClick。
    // 按钮回调必须指向场景中已激活的 MainMenu 组件。
    // 性别选择写入静态 PendingFemaleSelection。
    // 该值必须在加载游戏场景前设置。
    // 下一场景由游戏流程读取该待选状态。
    // 重新开始或返回菜单时应由游戏流程重新覆盖该状态。
    // ShowPanelWithFade 依赖场景内可用的 ScreenFader.Instance。
    // 未配置淡入淡出器时会直接切换面板以保持菜单可用。
    // 有淡入淡出器时，面板切换发生在其回调中。
    // 回调执行前 genderSelectPanel 必须仍然有效。
    // LoadGameScene 同样会在淡出完成后再加载场景。
    // gameSceneIndex 必须对应 Build Settings 中已加入的场景。
    // 无效索引会导致 SceneManager.LoadScene 失败。
    // 以索引加载时，Build Settings 的场景顺序即运行时配置。
    // QuitGame 只会在已构建的播放器中结束应用。
    // 在 Unity 编辑器 Play Mode 中该调用不会退出编辑器。
    // 调试日志仅用于确认退出请求已执行。
    // 本组件不创建 ScreenFader 或游戏场景。
    // 所需单例和场景索引均应在项目配置中准备。
    // 仅修改注释不会改变按钮绑定和场景加载行为。
    [Header("场景设置")]
    [SerializeField] private int gameSceneIndex = 1;

    [Header("性别选择面板")]
    [Tooltip("点击开始游戏后弹出的面板（含两个按钮：寻找哥哥/寻找姐姐），默认隐藏")]
    [SerializeField] private GameObject genderSelectPanel;

    // Start 在物体启用后的首帧调用，确保面板初始状态在场景加载后统一关闭。
    private void Start()
    {
        if (genderSelectPanel != null)
        {
            genderSelectPanel.SetActive(false);
        }
    }

    /// <summary>开始按钮：黑幕过渡弹出性别选择；没配面板则直接以默认（哥哥线）开始。</summary>
    // 可直接绑定到 Button.OnClick；未配置面板时使用默认分支，避免空引用。
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
    // 选择结果必须在加载场景前写入，供下一场景读取。
    public void StartAsMale()
    {
        GameManager.PendingFemaleSelection = false;
        LoadGameScene();
    }

    /// <summary>「寻找姐姐」按钮（玩家为女性）。</summary>
    // 该静态待选状态由游戏流程消费；重新开始时应由流程覆盖。
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

    // ScreenFader 未放入场景时降级为直接切换，避免配置缺失阻断菜单。
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

    // gameSceneIndex 必须已加入 Build Settings，否则 LoadScene 会失败。
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

    // Application.Quit 在编辑器中不会退出，仅在已构建的播放器中生效。
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("游戏已退出");
    }
}
