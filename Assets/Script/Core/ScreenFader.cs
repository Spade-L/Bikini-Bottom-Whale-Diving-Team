using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 统一处理黑屏、白屏和场景渐变
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    /// <summary>渐变进行中（PlayerMovement2D 用它锁移动）。</summary>
    public static bool IsFading => Instance != null && Instance.isFading;

    // 未显式传入时使用的单程渐变时长
    [SerializeField] private float defaultDuration = 0.9f;

    // CanvasGroup 同时驱动透明度和输入拦截；isFading 供外部暂停移动
    private CanvasGroup group;
    private Image overlayImage;
// 记录 ScreenFader 的当前状态
    private bool isFading;

    // 在场景加载前创建常驻管理器
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// AutoCreate 缺少引用时提前结束
        if (Instance == null)
        {
// 添加所需组件
            new GameObject("ScreenFader").AddComponent<ScreenFader>();
        }
    }

    // 初始化组件引用和运行状态
    private void Awake()
    {
        // 防止启动回调与场景预置对象同时存在时生成两层遮罩
        if (Instance != null && Instance != this)
        {
// 清理当前对象
            Destroy(gameObject);
            return;
        }

// 同步 Awake 的状态
        Instance = this;
        DontDestroyOnLoad(gameObject);
// 推进 Awake 中的必要步骤
        BuildOverlay();

// 同步 Awake 的内部状态
        group.alpha = 1f; // 游戏启动画面从黑渐亮
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 读取初始依赖并同步首帧状态
    private void Start()
    {
        StartCoroutine(FadeRoutine(1f, 0f, defaultDuration, null));
    }

    // 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 检查 OnDestroy 的前置条件
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
// 同步 OnDestroy 的内部状态
            Instance = null;
        }
    }

    // 响应 OnSceneLoaded 生命周期
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新场景就绪：从全黑渐亮（无论切场景前有没有渐黑，都保证入场效果一致）
        StopAllCoroutines();
        group.alpha = 1f;
// 启动当前协程
        StartCoroutine(FadeRoutine(1f, 0f, defaultDuration, null));
    }

// 处理 FadeOutThen 对应逻辑
    public void FadeOutThen(Action onComplete, float duration = -1f)
    {
        StopAllCoroutines();
// 检查 FadeOutThen 的前置条件
        if (overlayImage != null) overlayImage.color = Color.black;
        StartCoroutine(FadeRoutine(group.alpha, 1f, duration > 0f ? duration : defaultDuration, onComplete));
    }

// 处理 FadeToWhiteThen 对应逻辑
    public void FadeToWhiteThen(Action onComplete, float duration = -1f)
    {
// 推进 FadeToWhiteThen 中的必要步骤
        StopAllCoroutines();
        if (overlayImage != null) overlayImage.color = Color.white;
// 在 FadeToWhiteThen 中继续当前处理
        StartCoroutine(FadeRoutine(group.alpha, 1f, duration > 0f ? duration : defaultDuration, onComplete));
    }

// 处理 FadeBlackToWhiteThen 对应逻辑
    public void FadeBlackToWhiteThen(Action onComplete, float duration = -1f)
    {
        StopAllCoroutines();
        if (overlayImage == null)
        {
            onComplete?.Invoke();
            return;
        }
        float actualDuration = duration > 0f ? duration : defaultDuration;
        StartCoroutine(FadeColorRoutine(Color.black, Color.white, actualDuration, onComplete));
    }

// 处理 FadeColorRoutine 对应逻辑
    private IEnumerator FadeColorRoutine(Color from, Color to, float duration, Action onComplete)
    {
        isFading = true;
        group.blocksRaycasts = true;
        group.alpha = 1f;
        overlayImage.color = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            overlayImage.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        overlayImage.color = to;
        group.alpha = 1f;
        isFading = false;
        group.blocksRaycasts = true;
        onComplete?.Invoke();
    }
// 设置 SetOverlaySortingOrder 的目标状态
    public void SetOverlaySortingOrder(int sortingOrder)
    {
        Canvas canvas = group == null ? null : group.GetComponent<Canvas>();
// 检查 SetOverlaySortingOrder 的前置条件
        if (canvas != null)
        {
// 同步 SetOverlaySortingOrder 的内部状态
            canvas.sortingOrder = sortingOrder;
        }
    }

    // 处理 FadeOutIn 对应逻辑
    public void FadeOutIn(Action atBlack, float duration = -1f)
    {
// 推进 FadeOutIn 中的必要步骤
        StopAllCoroutines();
        if (overlayImage != null) overlayImage.color = Color.black;
// 在 FadeOutIn 中继续当前处理
        StartCoroutine(FadeOutInRoutine(atBlack, duration > 0f ? duration : defaultDuration));
    }

    // 处理 FadeOutInRoutine 对应逻辑
    private IEnumerator FadeOutInRoutine(Action atBlack, float duration)
    {
        yield return FadeRoutine(group.alpha, 1f, duration, null);
// 使用 FadeOutInRoutine 所需功能
        atBlack?.Invoke();
        yield return FadeRoutine(1f, 0f, duration, null);
    }

    // 处理 FadeRoutine 对应逻辑
    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
// 同步 FadeRoutine 的内部状态
        isFading = true;
        group.blocksRaycasts = true;

// 设置 FadeRoutine 的配置数值
        float elapsed = 0f;
        while (elapsed < duration)
        {
// 推进 FadeRoutine 的当前步骤
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
// 等待下一步
            yield return null;
        }

// 同步 FadeRoutine 的内部状态（FadeRoutine）
        group.alpha = to;
        isFading = false;
// 同步 FadeRoutine 的内部状态（FadeRoutine）（group）
        group.blocksRaycasts = to > 0.01f; // 停在全黑时继续挡点击，透明后放行

// 使用 FadeRoutine 所需功能
        onComplete?.Invoke();
    }

    // 处理 BuildOverlay 对应逻辑
    private void BuildOverlay()
    {
// 保存 canvasGo 引用
        var canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform, false);

// 保存 canvas 引用
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
// 同步 BuildOverlay 的内部状态
        canvas.sortingOrder = 9999;

// 同步 BuildOverlay 的内部状态（BuildOverlay）
        group = canvasGo.AddComponent<CanvasGroup>();
        group.interactable = false;

// 保存 imageGo 引用
        var imageGo = new GameObject("Black");
        imageGo.transform.SetParent(canvasGo.transform, false);

// 同步 BuildOverlay 的内部状态（BuildOverlay）（overlayImage）
        overlayImage = imageGo.AddComponent<Image>();
        overlayImage.color = Color.black;

// 同步 BuildOverlay 的相关数据
        RectTransform rt = overlayImage.rectTransform;
        rt.anchorMin = Vector2.zero;
// 同步 BuildOverlay 的内部状态（BuildOverlay）（rt）
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
// 在 BuildOverlay 中继续当前处理
        rt.offsetMax = Vector2.zero;
    }
}
