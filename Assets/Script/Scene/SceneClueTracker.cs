using System;
using System.Collections;
using TMPro;
using UnityEngine;

// 统计关键线索并触发场景通关演出
public class SceneClueTracker : MonoBehaviour
{
    // 用于生成场景通关状态的唯一标识
    [Header("场景标识（用于生成通关 Flag: scene_cleared_<sceneId>）")]
    // 同时也是存档中对应 Flag 的后缀
    [SerializeField] private string sceneId;

    // 本场景必须收集的线索集合
    [Header("本场景的关键线索（3-4 个）")]
    // 进度和通关条件均以该数组为准
    [SerializeField] private ClueData[] keyClues;

    // 屏幕角落的进度文本及其格式
    [Header("进度 UI（屏幕角落）")]
    // 若配置该文本，则实时写入收集进度
    [SerializeField] private TMP_Text progressText;
    // 控制进度字符串中当前值与总数的排版
    [SerializeField] private string progressFormat = "寻踪进度：{0}/{1}";

    // 通关时可选播放的视觉与对话演出资源
    [Header("通关演出")]
    // 天台模式下要求真相 Flag 已经设置
    [Tooltip("勾选 = 只有 truth_revealed 已设置才播通关演出（天台专用：区分真/坏结局）")]
    // 关闭时所有场景都只按关键线索数量判定
    [SerializeField] private bool requireTruthRevealed = false;
    // 通关瞬间短暂显示的场景对象
    [Tooltip("“哥哥”的影子（场景里预放好，默认隐藏）")]
    // 演出开始时启用，黑幕完全覆盖后再次隐藏
    [SerializeField] private GameObject brotherShadow;
    // 影子保持可见的时间长度
    [Tooltip("影子展示秒数")]
// 设置 SceneClueTracker 的配置数值
    [SerializeField] private float shadowDuration = 2f;
    // 承载全屏淡入淡出效果的画布组
    [Tooltip("全屏黑幕 CanvasGroup")]
// 保存 blackout 引用
    [SerializeField] private CanvasGroup blackout;
    // 黑幕从透明到不透明或反向变化的用时
    [SerializeField] private float blackoutFadeDuration = 0.6f;
    // 影子展示完成后黑幕停留的时长
    [SerializeField] private float blackoutHoldDuration = 1f;
    // 黑幕后可选播放的结算独白
    [Tooltip("黑幕后播放的独白（可空）")]
// 保存 clearMonologue 引用
    [SerializeField] private DialogueData clearMonologue;
    [Tooltip("结局门接管后停止普通场景清场演出")]
// 同步 SceneClueTracker 的相关数据
    [SerializeField] private EndingGate endingGate;

// 同步 SceneClueTracker 的相关数据（SceneClueTracker 后续步骤）
    private string ClearedFlag => $"scene_cleared_{sceneId}";

    private bool clearSequencePlaying;
// 黑幕期间阻止移动与交互
    private IDisposable movementLease;
    private IDisposable interactionLease;

    // 读取初始依赖并同步首帧状态
    private void Start()
    {
        if (brotherShadow != null)
        {
// 切换 Start 的显示状态
            brotherShadow.SetActive(false);
        }

        // 黑幕初始为透明且隐藏，避免遮挡场景加载画面
        if (blackout != null)
        {
// 同步 Start 的状态
            blackout.alpha = 0f;
            blackout.gameObject.SetActive(false);
        }

        // 同时监听线索与 Flag，支持天台真相 Flag 的补触发
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClueCollected += HandleClueCollected;
// 推进 Start 的当前步骤
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
        }

        // 初始化 UI，使已读档的进度立即可见
        RefreshProgressUI();
        // 读档后可能已经集齐核心线索但尚未写入清场 Flag
        TryTriggerClear();
    }

    // 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 同步 OnDestroy 的内部状态
        clearSequencePlaying = false;
        ReleaseBlackoutLocks();

        if (GameManager.Instance != null)
        {
// 推进 OnDestroy 的当前步骤
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
        }
    }

    // 处理 CountCollected 对应逻辑
    private int CountCollected()
    {
        // 没有管理器或线索数组时，进度固定为零
        GameManager gm = GameManager.Instance;
        if (gm == null || keyClues == null)
        {
// 返回 CountCollected 的处理结果
            return 0;
        }

        int count = 0;
// 遍历全部元素
        foreach (ClueData clue in keyClues)
        {
            if (clue != null && gm.HasClue(clue.ClueId))
            {
// 推进 CountCollected 的当前步骤
                count++;
            }
        }

        return count;
    }

// 处理 CountRequiredClues 对应逻辑
    private int CountRequiredClues()
    {
        if (keyClues == null)
        {
// 返回 CountRequiredClues 的处理结果
            return 0;
        }

        int count = 0;
// 在 CountRequiredClues 中继续当前处理
        foreach (ClueData clue in keyClues)
        {
            if (clue != null && !string.IsNullOrEmpty(clue.ClueId))
            {
// 推进 CountRequiredClues 的当前步骤
                count++;
            }
        }

        return count;
    }

// 刷新 RefreshProgressUI 对应状态
    private void RefreshProgressUI()
    {
        // UI 与线索配置齐全时，按格式显示当前收集数量
        if (progressText != null && keyClues != null)
        {
// 更新 RefreshProgressUI 的界面文本
            progressText.text = string.Format(progressFormat, CountCollected(), CountRequiredClues());
        }
    }

    // 响应 HandleClueCollected 状态变化
    private void HandleClueCollected(ClueData _)
    {
        // 收集任意线索后刷新显示，并检查是否刚好集齐
        RefreshProgressUI();
// 推进 HandleClueCollected 中的必要步骤
        TryTriggerClear();
    }

    // 剧情标记变化后同步当前界面与交互
    private void HandleFlagsChanged()
    {
// 检查 HandleFlagsChanged 的前置条件
        if (requireTruthRevealed)
        {
            TryTriggerClear();
        }
    }

    // 确认关键线索完整后启动通关演出
    private void TryTriggerClear()
    {
        // 已通关的场景不重复启动演出协程
        GameManager gm = GameManager.Instance;
// 检查 TryTriggerClear 的前置条件
        if (endingGate != null && endingGate.HasTakenOverEnding)
        {
            return;
        }
// TryTriggerClear 缺少引用时提前结束
        if (gm == null || gm.HasFlag(ClearedFlag) || clearSequencePlaying)
        {
            return;
        }

        // 天台专用门：坏结局不设 truth_revealed，集齐线索也不通关（交给 EndingGate 收尾）
        if (requireTruthRevealed && !gm.HasFlag("truth_revealed"))
        {
            return;
        }

        // 配置了至少一项关键线索且全部收集后才播放演出
        int requiredCount = CountRequiredClues();
        if (requiredCount > 0 && CountCollected() >= requiredCount)
        {
            // 启动后由协程负责等待已有演出结束
            clearSequencePlaying = true;
            StartCoroutine(PlayClearSequence());
        }
    }

// 处理 ShouldStopForEnding 对应逻辑
    private bool ShouldStopForEnding()
    {
// 返回 ShouldStopForEnding 的处理结果
        return endingGate != null && endingGate.HasTakenOverEnding;
    }

    // 处理 AbortClearSequenceForEnding 对应逻辑
    private bool AbortClearSequenceForEnding()
    {
// 检查 AbortClearSequenceForEnding 的前置条件
        if (!ShouldStopForEnding()) return false;
        ReleaseBlackoutLocks();

        if (brotherShadow != null)
        {
// 切换 AbortClearSequenceForEnding 的显示状态
            brotherShadow.SetActive(false);
        }

        if (blackout != null)
        {
// 同步 AbortClearSequenceForEnding 的内部状态
            blackout.alpha = 0f;
            blackout.gameObject.SetActive(false);
        }

// 同步 AbortClearSequenceForEnding 的内部状态（AbortClearSequenceForEnding）
        clearSequencePlaying = false;
        return true;
    }

    // 播放线索收集完成后的通关演出
    private IEnumerator PlayClearSequence()
    {
        if (AbortClearSequenceForEnding())
        {
// 无法继续时结束 PlayClearSequence
            yield break;
        }

        while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
// 检查 PlayClearSequence 的前置条件
            if (AbortClearSequenceForEnding())
            {
                yield break;
            }

// 等待下一步
            yield return null;
        }

        // 等回溯闪回演出结束，避免叠加
        while (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback)
        {
// 检查 PlayClearSequence 的前置条件（PlayClearSequence）
            if (AbortClearSequenceForEnding())
            {
                yield break;
            }

// 在 PlayClearSequence 中继续当前处理
            yield return null;
        }

// 检查 PlayClearSequence 的前置条件（PlayClearSequence）（if）
        if (AbortClearSequenceForEnding())
        {
            yield break;
        }

// 在 PlayClearSequence 中继续当前处理（PlayClearSequence 后续步骤）
        if (brotherShadow != null)
        {
            brotherShadow.SetActive(true);
// 在 PlayClearSequence 中继续当前处理（PlayClearSequence 后续步骤）（288）
            yield return new WaitForSeconds(shadowDuration);
        }

        if (AbortClearSequenceForEnding())
        {
// 在 PlayClearSequence 中处理 无法继续时结束 PlayClearSequence
            yield break;
        }

        // 黑幕
        if (blackout != null)
        {
            // 激活画布前锁住交互，确保 F 提示不会出现在黑幕上
            AcquireBlackoutLocks();
            blackout.gameObject.SetActive(true);
            yield return FadeBlackout(0f, 1f);

// 在 PlayClearSequence 中继续当前处理（PlayClearSequence 后续步骤）（305）
            if (AbortClearSequenceForEnding())
            {
                yield break;
            }

// 在 PlayClearSequence 中继续当前处理（PlayClearSequence 后续步骤）（311）
            if (brotherShadow != null)
            {
                brotherShadow.SetActive(false);
            }

            // 保持黑幕，为场景内状态变化预留时间；期间仍允许最终结局接管
            float holdElapsed = 0f;
            while (holdElapsed < Mathf.Max(0f, blackoutHoldDuration))
            {
// 在 PlayClearSequence 中继续当前处理（PlayClearSequence 后续步骤）（321）
                if (AbortClearSequenceForEnding())
                {
                    yield break;
                }

// 推进 PlayClearSequence 的当前步骤
                holdElapsed += Time.deltaTime;
                yield return null;
            }

// 在 PlayClearSequence 中继续当前处理（PlayClearSequence 后续步骤）（332）
            if (AbortClearSequenceForEnding())
            {
// 无法继续时结束 PlayClearSequence（PlayClearSequence）
                yield break;
            }

            // 通关 Flag 在黑幕中设置——场景门/物件在黑幕里完成变化
            GameManager.Instance.SetFlag(ClearedFlag);

            // 状态更新完成后淡回游戏画面并隐藏画布
            yield return FadeBlackout(1f, 0f);
            blackout.gameObject.SetActive(false);
            ReleaseBlackoutLocks();
        }
// 处理 PlayClearSequence 的备用分支
        else
        {
            // 未配置黑幕时仍必须写入通关 Flag
            GameManager.Instance.SetFlag(ClearedFlag);
        }

        // 演出完成后，如有配置则播放场景通关独白
        if (clearMonologue != null && DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.StartDialogue(clearMonologue);
        }

// 同步 PlayClearSequence 的内部状态
        clearSequencePlaying = false;
    }

// 获取黑幕期间的输入锁
    private void AcquireBlackoutLocks()
    {
        movementLease ??= GameplayInputLock.AcquireMovementLock();
        interactionLease ??= GameplayInputLock.AcquireInteractionLock();
    }

// 黑幕消失后释放输入锁
    private void ReleaseBlackoutLocks()
    {
        movementLease?.Dispose();
        interactionLease?.Dispose();
        movementLease = null;
        interactionLease = null;
    }

    // 处理 FadeBlackout 对应逻辑
    private IEnumerator FadeBlackout(float from, float to)
    {
        // 黑幕过渡时间可配置为零，零时直接写入目标值
        if (blackoutFadeDuration <= 0f)
        {
            blackout.alpha = to;
// 无法继续时结束 FadeBlackout
            yield break;
        }

        float elapsed = 0f;
// 等待条件变化
        while (elapsed < blackoutFadeDuration)
        {
            elapsed += Time.deltaTime;
// 同步 FadeBlackout 的内部状态
            blackout.alpha = Mathf.Lerp(from, to, elapsed / blackoutFadeDuration);
            yield return null;
        }

        // 循环结束后写入精确目标值，消除插值余量
        blackout.alpha = to;
    }
}
