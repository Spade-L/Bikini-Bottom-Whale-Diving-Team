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
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    /// <summary>渐变进行中（PlayerMovement2D 用它锁移动）。</summary>
    public static bool IsFading => Instance != null && Instance.isFading;

    [SerializeField] private float defaultDuration = 0.9f;

    private CanvasGroup group;
    private bool isFading;

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

    private void Start()
    {
        StartCoroutine(FadeRoutine(1f, 0f, defaultDuration, null));
    }

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

    private IEnumerator FadeOutInRoutine(Action atBlack, float duration)
    {
        yield return FadeRoutine(group.alpha, 1f, duration, null);
        atBlack?.Invoke();
        yield return FadeRoutine(1f, 0f, duration, null);
    }

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
