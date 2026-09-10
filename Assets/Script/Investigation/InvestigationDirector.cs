using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 定义 InvestigationDirector 类型
public class InvestigationDirector : MonoBehaviour
{
// 更新当前逻辑
    public static InvestigationDirector Instance { get; private set; }

// 配置 事件表 分组
    [Header("事件表")]
    [SerializeField] private InvestigationEventTable eventTable;

// 配置 闪回 UI（全屏覆盖） 分组
    [Header("闪回 UI（全屏覆盖）")]
    [SerializeField] private CanvasGroup flashbackOverlay;
// 保存 flashbackImage 数据
    [SerializeField] private Image flashbackImage;
    [SerializeField] private TMP_Text flashbackCaption;
// 配置 fadeDuration 数值
    [SerializeField] private float fadeDuration = 0.35f;

// 更新当前逻辑
    private readonly Queue<InvestigationEventTable.ThresholdEvent> pendingEvents
        = new Queue<InvestigationEventTable.ThresholdEvent>();

// 记录 isPlayingEvent 状态
    private bool isPlayingEvent;

// 记录 IsPlayingFlashback 状态
    public bool IsPlayingFlashback => isPlayingEvent;

// 定义 Awake 方法
    private void Awake()
    {
// 更新当前状态
        Instance = this;

// 判断当前条件
        if (flashbackOverlay != null)
        {
// 更新当前状态
            flashbackOverlay.alpha = 0f;
            flashbackOverlay.gameObject.SetActive(false);
        }
    }

// 定义 Start 方法
    private void Start()
    {
// 判断当前条件
        if (GameManager.Instance != null)
        {
// 更新当前逻辑
            GameManager.Instance.OnInvestigationCountChanged += HandleCountChanged;
        }
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 判断当前条件
        if (Instance == this)
        {
// 更新当前状态
            Instance = null;
        }

// 判断当前条件
        if (GameManager.Instance != null)
        {
// 更新当前逻辑
            GameManager.Instance.OnInvestigationCountChanged -= HandleCountChanged;
        }
    }

// 定义 HandleCountChanged 方法
    private void HandleCountChanged(int count)
    {
// 空引用时直接退出
        if (eventTable == null || eventTable.events == null)
        {
// 返回当前结果
            return;
        }

// 保存 gm 数据
        GameManager gm = GameManager.Instance;

// 遍历全部元素
        foreach (var evt in eventTable.events)
        {
// 判断当前条件
            if (count < evt.threshold)
            {
// 更新当前逻辑
                continue;
            }

// 保存 reachedFlag 数据
            string reachedFlag = $"inv_reached_{evt.threshold}";
            if (gm.HasFlag(reachedFlag))
            {
// 更新当前逻辑
                continue;
            }

            // 批量写入相关标记，避免每个 Flag 都触发一次全场景刷新
            gm.SetFlags(BuildFlags(evt));
            pendingEvents.Enqueue(evt);
        }

// 判断当前条件
        if (!isPlayingEvent && pendingEvents.Count > 0)
        {
// 启动当前协程
            StartCoroutine(PlayPendingEvents());
        }
    }

// 定义 BuildFlags 方法
    private IEnumerable<string> BuildFlags(InvestigationEventTable.ThresholdEvent evt)
    {
// 等待下一步
        yield return $"inv_reached_{evt.threshold}";
        if (evt.setFlags == null)
        {
// 保存 break 数据
            yield break;
        }

// 遍历全部元素
        foreach (string flag in evt.setFlags)
        {
// 等待下一步
            yield return flag;
        }
    }

// 定义 PlayPendingEvents 方法
    private IEnumerator PlayPendingEvents()
    {
// 更新当前状态
        isPlayingEvent = true;

// 等待条件变化
        while (pendingEvents.Count > 0)
        {
// 保存 evt 数据
            var evt = pendingEvents.Dequeue();

// 等待条件变化
            while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            {
// 等待下一步
                yield return null;
            }

// 判断当前条件
            if (evt.flashback != null && evt.flashback.HasContent)
            {
// 等待下一步
                yield return PlayFlashback(evt.flashback);
            }

// 检查对话状态
            if (evt.monologue != null && DialogueUIManager.Instance != null)
            {
// 记录 done 状态
                bool done = false;
                DialogueUIManager.Instance.StartDialogue(evt.monologue, () => done = true);
// 等待条件变化
                while (!done)
                {
// 等待下一步
                    yield return null;
                }
            }
        }

// 更新当前状态
        isPlayingEvent = false;
    }

// 定义 PlayFlashback 方法
    private IEnumerator PlayFlashback(FlashbackSequence flashback)
    {
// 空引用时直接退出
        if (flashbackOverlay == null || flashbackImage == null)
        {
// 输出调试信息
            Debug.LogWarning("[InvestigationDirector] 闪回 UI 未配置，跳过演出。");
            yield break;
        }

// 切换显示状态
        flashbackOverlay.gameObject.SetActive(true);

// 判断当前条件
        if (flashbackCaption != null)
        {
// 更新界面文本
            flashbackCaption.text = TextTokens.Resolve(flashback.caption ?? string.Empty);
        }

// 遍历全部元素
        foreach (Sprite sprite in flashback.images)
        {
// 更新显示图像
            flashbackImage.sprite = sprite;
            yield return Fade(0f, 1f);
// 等待下一步
            yield return new WaitForSeconds(flashback.secondsPerImage);
            yield return Fade(1f, 0f);
        }

// 切换显示状态
        flashbackOverlay.gameObject.SetActive(false);
    }

// 定义 Fade 方法
    private IEnumerator Fade(float from, float to)
    {
// 配置 elapsed 数值
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
// 更新当前逻辑
            elapsed += Time.deltaTime;
            flashbackOverlay.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
// 等待下一步
            yield return null;
        }

// 更新当前状态
        flashbackOverlay.alpha = to;
    }
}
