using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 按调查次数触发闪回、封锁和独白
public class InvestigationDirector : MonoBehaviour
{
// 推进 InvestigationDirector 的当前步骤
    public static InvestigationDirector Instance { get; private set; }

// 配置 事件表 分组
    [Header("事件表")]
    [SerializeField] private InvestigationEventTable eventTable;

// 配置 闪回 UI（全屏覆盖） 分组
    [Header("闪回 UI（全屏覆盖）")]
    [SerializeField] private CanvasGroup flashbackOverlay;
// 同步 InvestigationDirector 的相关数据
    [SerializeField] private Image flashbackImage;
    [SerializeField] private TMP_Text flashbackCaption;
// 设置 InvestigationDirector 的配置数值
    [SerializeField] private float fadeDuration = 0.35f;

// 在 InvestigationDirector 中处理 推进 InvestigationDirector 的当前步骤
    private readonly Queue<InvestigationEventTable.ThresholdEvent> pendingEvents
        = new Queue<InvestigationEventTable.ThresholdEvent>();

// 记录 InvestigationDirector 的当前状态
    private bool isPlayingEvent;

// 记录 InvestigationDirector 的当前状态（InvestigationDirector 后续步骤）
    public bool IsPlayingFlashback => isPlayingEvent;

// 初始化组件引用和运行状态
    private void Awake()
    {
// 同步 Awake 的状态
        Instance = this;

// 检查 Awake 的前置条件
        if (flashbackOverlay != null)
        {
// 同步 Awake 的内部状态
            flashbackOverlay.alpha = 0f;
            flashbackOverlay.gameObject.SetActive(false);
        }
    }

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 检查 Start 的前置条件
        if (GameManager.Instance != null)
        {
// 推进 Start 的当前步骤
            GameManager.Instance.OnInvestigationCountChanged += HandleCountChanged;
        }
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 检查 OnDestroy 的前置条件
        if (Instance == this)
        {
// 同步 OnDestroy 的内部状态
            Instance = null;
        }

// 检查 OnDestroy 的前置条件（OnDestroy）
        if (GameManager.Instance != null)
        {
// 推进 OnDestroy 的当前步骤
            GameManager.Instance.OnInvestigationCountChanged -= HandleCountChanged;
        }
    }

// 响应 HandleCountChanged 状态变化
    private void HandleCountChanged(int count)
    {
// HandleCountChanged 缺少引用时提前结束
        if (eventTable == null || eventTable.events == null)
        {
// 返回 HandleCountChanged 的处理结果
            return;
        }

// 同步 HandleCountChanged 的相关数据
        GameManager gm = GameManager.Instance;

// 遍历全部元素
        foreach (var evt in eventTable.events)
        {
// 检查 HandleCountChanged 的前置条件
            if (count < evt.threshold)
            {
// 推进 HandleCountChanged 的当前步骤
                continue;
            }

// 同步 HandleCountChanged 的相关数据（HandleCountChanged 后续步骤）
            string reachedFlag = $"inv_reached_{evt.threshold}";
            if (gm.HasFlag(reachedFlag))
            {
// 推进 HandleCountChanged 的当前步骤（HandleCountChanged）
                continue;
            }

            // 批量写入相关标记，避免每个 Flag 都触发一次全场景刷新
            gm.SetFlags(BuildFlags(evt));
            pendingEvents.Enqueue(evt);
        }

// 检查 HandleCountChanged 的前置条件（HandleCountChanged）
        if (!isPlayingEvent && pendingEvents.Count > 0)
        {
// 启动当前协程
            StartCoroutine(PlayPendingEvents());
        }
    }

// 汇总事件需要写入的剧情标记
    private IEnumerable<string> BuildFlags(InvestigationEventTable.ThresholdEvent evt)
    {
// 等待下一步
        yield return $"inv_reached_{evt.threshold}";
        if (evt.setFlags == null)
        {
// 无法继续时结束 BuildFlags
            yield break;
        }

// 在 BuildFlags 中继续当前处理
        foreach (string flag in evt.setFlags)
        {
// 在 BuildFlags 中继续当前处理（BuildFlags 后续步骤）
            yield return flag;
        }
    }

// 按顺序播放调查阈值触发的事件
    private IEnumerator PlayPendingEvents()
    {
// 同步 PlayPendingEvents 的内部状态
        isPlayingEvent = true;

// 等待条件变化
        while (pendingEvents.Count > 0)
        {
// 同步 PlayPendingEvents 的相关数据
            var evt = pendingEvents.Dequeue();

// 在 PlayPendingEvents 中继续当前处理
            while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            {
// 在 PlayPendingEvents 中继续当前处理（PlayPendingEvents 后续步骤）
                yield return null;
            }

// 检查 PlayPendingEvents 的前置条件
            if (evt.flashback != null && evt.flashback.HasContent)
            {
// 在 PlayPendingEvents 中继续当前处理（PlayPendingEvents 后续步骤）（164）
                yield return PlayFlashback(evt.flashback);
            }

// 检查对话状态
            if (evt.monologue != null && DialogueUIManager.Instance != null)
            {
// 记录 PlayPendingEvents 的当前状态
                bool done = false;
                DialogueUIManager.Instance.StartDialogue(evt.monologue, () => done = true);
// 在 PlayPendingEvents 中继续当前处理（PlayPendingEvents 后续步骤）（174）
                while (!done)
                {
// 在 PlayPendingEvents 中继续当前处理（PlayPendingEvents 后续步骤）（177）
                    yield return null;
                }
            }
        }

// 同步 PlayPendingEvents 的内部状态（PlayPendingEvents）
        isPlayingEvent = false;
    }

// 播放闪回图像并等待玩家看完
    private IEnumerator PlayFlashback(FlashbackSequence flashback)
    {
// 缺少必要引用时退出 PlayFlashback
        if (flashbackOverlay == null || flashbackImage == null)
        {
// 输出调试信息
            Debug.LogWarning("[InvestigationDirector] 闪回 UI 未配置，跳过演出。");
            yield break;
        }

// 切换 PlayFlashback 的显示状态
        flashbackOverlay.gameObject.SetActive(true);

// 检查 PlayFlashback 的前置条件
        if (flashbackCaption != null)
        {
// 更新 PlayFlashback 的界面文本
            flashbackCaption.text = TextTokens.Resolve(flashback.caption ?? string.Empty);
        }

// 在 PlayFlashback 中继续当前处理
        foreach (Sprite sprite in flashback.images)
        {
// 更新显示图像
            flashbackImage.sprite = sprite;
            yield return Fade(0f, 1f);
// 在 PlayFlashback 中继续当前处理（PlayFlashback 后续步骤）
            yield return new WaitForSeconds(flashback.secondsPerImage);
            yield return Fade(1f, 0f);
        }

// 切换 PlayFlashback 的显示状态（PlayFlashback 后续步骤）
        flashbackOverlay.gameObject.SetActive(false);
    }

// 处理 Fade 对应逻辑
    private IEnumerator Fade(float from, float to)
    {
// 设置 Fade 的配置数值
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
// 推进 Fade 的当前步骤
            elapsed += Time.deltaTime;
            flashbackOverlay.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
// 在 Fade 中继续当前处理
            yield return null;
        }

// 同步 Fade 的内部状态
        flashbackOverlay.alpha = to;
    }
}
