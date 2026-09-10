using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 定义 ScreenFader 类型
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
// 记录 isFading 状态
    private bool isFading;

    // 比场景对象更早建立，确保首场景也有从黑场进入的效果
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// 空引用时直接退出
        if (Instance == null)
        {
// 添加所需组件
            new GameObject("ScreenFader").AddComponent<ScreenFader>();
        }
    }

    private void Awake()
    {
        // 防止启动回调与场景预置对象同时存在时生成两层遮罩
        if (Instance != null && Instance != this)
        {
// 清理当前对象
            Destroy(gameObject);
            return;
        }

// 更新当前状态
        Instance = this;
        DontDestroyOnLoad(gameObject);
// 执行 BuildOverlay
        BuildOverlay();

// 更新当前状态
        group.alpha = 1f; // 游戏启动画面从黑渐亮
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Start 在覆盖层建好后执行首帧入场淡入
    private void Start()
    {
        StartCoroutine(FadeRoutine(1f, 0f, defaultDuration, null));
    }

    // 仅由当前单例取消订阅，避免旧重复实例误清理有效监听
    private void OnDestroy()
    {
// 判断当前条件
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
// 更新当前状态
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新场景就绪：从全黑渐亮（无论切场景前有没有渐黑，都保证入场效果一致）
        StopAllCoroutines();
        group.alpha = 1f;
// 启动当前协程
        StartCoroutine(FadeRoutine(1f, 0f, defaultDuration, null));
    }

// 定义 FadeOutThen 方法
    public void FadeOutThen(Action onComplete, float duration = -1f)
    {
        StopAllCoroutines();
// 判断当前条件
        if (overlayImage != null) overlayImage.color = Color.black;
        StartCoroutine(FadeRoutine(group.alpha, 1f, duration > 0f ? duration : defaultDuration, onComplete));
    }

// 定义 FadeToWhiteThen 方法
    public void FadeToWhiteThen(Action onComplete, float duration = -1f)
    {
// 执行 StopAllCoroutines
        StopAllCoroutines();
        if (overlayImage != null) overlayImage.color = Color.white;
// 启动当前协程
        StartCoroutine(FadeRoutine(group.alpha, 1f, duration > 0f ? duration : defaultDuration, onComplete));
    }

// 从全黑保持不透明渐变到全白
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

// 颜色渐变期间保持遮罩不透明
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
// 定义 SetOverlaySortingOrder 方法
    public void SetOverlaySortingOrder(int sortingOrder)
    {
        Canvas canvas = group == null ? null : group.GetComponent<Canvas>();
// 判断当前条件
        if (canvas != null)
        {
// 更新当前状态
            canvas.sortingOrder = sortingOrder;
        }
    }

    /// <summary>渐黑 → 全黑时执行回调（切换 UI）→ 渐亮。</summary>
    public void FadeOutIn(Action atBlack, float duration = -1f)
    {
// 执行 StopAllCoroutines
        StopAllCoroutines();
        if (overlayImage != null) overlayImage.color = Color.black;
// 启动当前协程
        StartCoroutine(FadeOutInRoutine(atBlack, duration > 0f ? duration : defaultDuration));
    }

    // 回调固定在完全不透明时执行，避免 UI 切换帧被玩家看见
    private IEnumerator FadeOutInRoutine(Action atBlack, float duration)
    {
        yield return FadeRoutine(group.alpha, 1f, duration, null);
// 调用 Invoke
        atBlack?.Invoke();
        yield return FadeRoutine(1f, 0f, duration, null);
    }

    // 每帧更新透明度；先锁定输入和移动，完成后再按最终透明度决定是否放行
    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
// 更新当前状态
        isFading = true;
        group.blocksRaycasts = true;

// 配置 elapsed 数值
        float elapsed = 0f;
        while (elapsed < duration)
        {
// 更新当前逻辑
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
// 等待下一步
            yield return null;
        }

// 更新当前状态
        group.alpha = to;
        isFading = false;
// 更新当前状态
        group.blocksRaycasts = to > 0.01f; // 停在全黑时继续挡点击，透明后放行

// 调用 Invoke
        onComplete?.Invoke();
    }

    // 运行时构造全屏 Canvas，避免每个场景维护重复的转场预制体
    private void BuildOverlay()
    {
// 保存 canvasGo 引用
        var canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform, false);

// 保存 canvas 引用
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
// 更新当前状态
        canvas.sortingOrder = 9999;

// 更新当前状态
        group = canvasGo.AddComponent<CanvasGroup>();
        group.interactable = false;

// 保存 imageGo 引用
        var imageGo = new GameObject("Black");
        imageGo.transform.SetParent(canvasGo.transform, false);

// 更新当前状态
        overlayImage = imageGo.AddComponent<Image>();
        overlayImage.color = Color.black;

// 保存 rt 数据
        RectTransform rt = overlayImage.rectTransform;
        rt.anchorMin = Vector2.zero;
// 更新当前状态
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
// 更新当前状态
        rt.offsetMax = Vector2.zero;
    }
}
