using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 调查系统导演：监听调查次数变化，触发阈值事件（回溯闪回、独白、封锁 Flag）。
/// 与 GameManager 同物体或单独挂都可，场景常驻（DontDestroyOnLoad 由 GameManager 承担，
/// 本组件建议放在每个场景的 UI Canvas 上，闪回 UI 引用场景内的即可）。
/// </summary>
public class InvestigationDirector : MonoBehaviour
{
    // 全局实例
    public static InvestigationDirector Instance { get; private set; }

    // 阈值配置
    [Header("事件表")]
    [SerializeField] private InvestigationEventTable eventTable;

    // 闪回遮罩
    [Header("闪回 UI（全屏覆盖）")]
    [SerializeField] private CanvasGroup flashbackOverlay;
    // 闪回画面
    [SerializeField] private Image flashbackImage;
    // 闪回字幕
    [SerializeField] private TMP_Text flashbackCaption;
    // 淡入时长
    [SerializeField] private float fadeDuration = 0.35f;

    // 待播队列
    private readonly Queue<InvestigationEventTable.ThresholdEvent> pendingEvents
        = new Queue<InvestigationEventTable.ThresholdEvent>();

    // 播放状态
    private bool isPlayingEvent;

    // 闪回状态
    public bool IsPlayingFlashback => isPlayingEvent;

    // 初始化
    private void Awake()
    {
        // 注册实例
        Instance = this;

        // 隐藏遮罩
        if (flashbackOverlay != null)
        {
            // 设为透明
            flashbackOverlay.alpha = 0f;
            // 关闭物体
            flashbackOverlay.gameObject.SetActive(false);
        }
    }

    // 开始监听
    private void Start()
    {
        // 检查管理器
        if (GameManager.Instance != null)
        {
            // 订阅次数变化
            GameManager.Instance.OnInvestigationCountChanged += HandleCountChanged;
        }
    }

    // 解除监听
    private void OnDestroy()
    {
        // 清理实例
        if (Instance == this)
        {
            // 释放引用
            Instance = null;
        }

        // 检查管理器
        if (GameManager.Instance != null)
        {
            // 取消订阅
            GameManager.Instance.OnInvestigationCountChanged -= HandleCountChanged;
        }
    }

    // 检查阈值
    private void HandleCountChanged(int count)
    {
        // 检查事件表
        if (eventTable == null || eventTable.events == null)
        {
            // 无表返回
            return;
        }

        // 获取管理器
        GameManager gm = GameManager.Instance;

        // 遍历事件
        foreach (var evt in eventTable.events)
        {
            // 未达阈值
            if (count < evt.threshold)
            {
                // 继续检查
                continue;
            }

            // "inv_reached_N" flag 同时充当"已触发"标记与剧情条件
            // 生成标记
            string reachedFlag = $"inv_reached_{evt.threshold}";
            // 已触发则跳过
            if (gm.HasFlag(reachedFlag))
            {
                // 继续检查
                continue;
            }

            // 批量写入所有相关标记，避免每个 Flag 都触发一次全场景刷新。
            gm.SetFlags(BuildFlags(evt));

            // 加入队列
            pendingEvents.Enqueue(evt);
        }

        // 检查播放状态
        if (!isPlayingEvent && pendingEvents.Count > 0)
        {
            // 开始播放
            StartCoroutine(PlayPendingEvents());
        }
    }

    private IEnumerable<string> BuildFlags(InvestigationEventTable.ThresholdEvent evt)
    {
        yield return $"inv_reached_{evt.threshold}";
        if (evt.setFlags == null)
        {
            yield break;
        }

        foreach (string flag in evt.setFlags)
        {
            yield return flag;
        }
    }

    // 播放队列
    private IEnumerator PlayPendingEvents()
    {
        // 标记播放中
        isPlayingEvent = true;

        // 依次处理
        while (pendingEvents.Count > 0)
        {
            // 取出事件
            var evt = pendingEvents.Dequeue();

            // 等当前对话关闭再演出，避免 UI 叠在一起
            // 等待对话结束
            while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            {
                // 等待下一帧
                yield return null;
            }

            // 检查闪回
            if (evt.flashback != null && evt.flashback.HasContent)
            {
                // 播放闪回
                yield return PlayFlashback(evt.flashback);
            }

            // 检查独白
            if (evt.monologue != null && DialogueUIManager.Instance != null)
            {
                // 完成标记
                bool done = false;
                // 开始独白
                DialogueUIManager.Instance.StartDialogue(evt.monologue, () => done = true);
                // 等待独白
                while (!done)
                {
                    // 等待下一帧
                    yield return null;
                }
            }
        }

        // 标记结束
        isPlayingEvent = false;
    }

    // 播放闪回
    private IEnumerator PlayFlashback(FlashbackSequence flashback)
    {
        // 检查界面
        if (flashbackOverlay == null || flashbackImage == null)
        {
            // 记录警告
            Debug.LogWarning("[InvestigationDirector] 闪回 UI 未配置，跳过演出。");
            // 结束协程
            yield break;
        }

        // 显示遮罩
        flashbackOverlay.gameObject.SetActive(true);

        // 检查字幕
        if (flashbackCaption != null)
        {
            // 设置字幕
            flashbackCaption.text = TextTokens.Resolve(flashback.caption ?? string.Empty);
        }

        // 播放画面
        foreach (Sprite sprite in flashback.images)
        {
            // 设置画面
            flashbackImage.sprite = sprite;

            // 淡入画面
            yield return Fade(0f, 1f);
            // 停留画面
            yield return new WaitForSeconds(flashback.secondsPerImage);
            // 淡出画面
            yield return Fade(1f, 0f);
        }

        // 隐藏遮罩
        flashbackOverlay.gameObject.SetActive(false);
    }

    // 执行淡化
    private IEnumerator Fade(float from, float to)
    {
        // 初始化时间
        float elapsed = 0f;
        // 更新透明度
        while (elapsed < fadeDuration)
        {
            // 累加时间
            elapsed += Time.deltaTime;
            // 插值透明度
            flashbackOverlay.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            // 等待下一帧
            yield return null;
        }

        // 设置最终值
        flashbackOverlay.alpha = to;
    }
}
