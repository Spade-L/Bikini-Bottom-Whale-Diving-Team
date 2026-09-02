using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 全局黑色渐变转场。游戏启动时自动创建（无需在任何场景摆放/配置）：
/// - 每次场景加载完成：画面从黑渐亮
/// - 调用 FadeOutThen：渐黑后执行回调（切场景用）
/// - 调用 FadeOutIn：渐黑 → 在全黑时执行回调（切 UI 用）→ 渐亮
/// 覆盖层 sortingOrder=9999，渐变期间拦截点击。
/// </summary>
// 提供跨场景统一的黑幕转场。
// 遮罩在首场景加载前自动创建。
// 单例跨场景保留，避免重复创建 Canvas。
// 覆盖层使用屏幕空间叠加模式。
// sortingOrder 保证黑幕位于普通界面上方。
// CanvasGroup 统一控制透明度与点击拦截。
// 渐变期间会锁定射线检测。
// 外部可通过 IsFading 锁定角色移动。
// 启动时先保持全黑，再渐亮显示首场景。
// 每次场景加载完成后都会从黑幕渐亮。
// 新的渐变请求会取消旧协程。
// FadeOutThen 在黑幕完成后执行回调。
// FadeOutIn 在全黑时执行中间回调。
// 回调可安全传入 null。
// duration 小于等于零时使用默认时长。
// FadeRoutine 使用帧间隔时间插值透明度。
// 完全透明时释放点击拦截。
// 停留全黑时继续拦截点击。
// OnDestroy 只由当前单例取消场景事件订阅。
// 运行时创建 Canvas，避免场景维护预制体。
// 黑色 Image 通过全屏锚点填满视口。
// 该组件不负责实际场景加载逻辑。
// 该组件不负责具体 UI 的显示状态。
// 所有转场共享同一个遮罩与协程状态。
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    /// <summary>渐变进行中（PlayerMovement2D 用它锁移动）。</summary>
    public static bool IsFading => Instance != null && Instance.isFading;

    // 未显式传入时使用的单程渐变时长。
    [SerializeField] private float defaultDuration = 0.9f;

    // CanvasGroup 同时驱动透明度和输入拦截；isFading 供外部暂停移动。
    private CanvasGroup group;
    private bool isFading;

    // 比场景对象更早建立，确保首场景也有从黑场进入的效果。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            new GameObject("ScreenFader").AddComponent<ScreenFader>();
        }
    }

    private void Awake()
    {
        // 防止启动回调与场景预置对象同时存在时生成两层遮罩。
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();

        group.alpha = 1f; // 游戏启动画面从黑渐亮
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Start 在覆盖层建好后执行首帧入场淡入。
    private void Start()
    {
        StartCoroutine(FadeRoutine(1f, 0f, defaultDuration, null));
    }

    // 仅由当前单例取消订阅，避免旧重复实例误清理有效监听。
    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新场景就绪：从全黑渐亮（无论切场景前有没有渐黑，都保证入场效果一致）
        StopAllCoroutines();
        group.alpha = 1f;
        StartCoroutine(FadeRoutine(1f, 0f, defaultDuration, null));
    }

    // 新请求优先取消旧协程，防止两个渐变同时争夺 alpha 和输入状态。
    /// <summary>渐黑，全黑后执行回调（通常是 SceneManager.LoadScene）。</summary>
    public void FadeOutThen(Action onComplete, float duration = -1f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(group.alpha, 1f, duration > 0f ? duration : defaultDuration, onComplete));
    }

    /// <summary>渐黑 → 全黑时执行回调（切换 UI）→ 渐亮。</summary>
    public void FadeOutIn(Action atBlack, float duration = -1f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutInRoutine(atBlack, duration > 0f ? duration : defaultDuration));
    }

    // 回调固定在完全不透明时执行，避免 UI 切换帧被玩家看见。
    private IEnumerator FadeOutInRoutine(Action atBlack, float duration)
    {
        yield return FadeRoutine(group.alpha, 1f, duration, null);
        atBlack?.Invoke();
        yield return FadeRoutine(1f, 0f, duration, null);
    }

    // 每帧更新透明度；先锁定输入和移动，完成后再按最终透明度决定是否放行。
    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
        isFading = true;
        group.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
        isFading = false;
        group.blocksRaycasts = to > 0.01f; // 停在全黑时继续挡点击，透明后放行

        onComplete?.Invoke();
    }

    // 运行时构造全屏 Canvas，避免每个场景维护重复的转场预制体。
    private void BuildOverlay()
    {
        var canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        group = canvasGo.AddComponent<CanvasGroup>();
        group.interactable = false;

        var imageGo = new GameObject("Black");
        imageGo.transform.SetParent(canvasGo.transform, false);

        var image = imageGo.AddComponent<Image>();
        image.color = Color.black;

        RectTransform rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
