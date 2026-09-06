using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 场景寻踪进度：登记本场景的关键线索，UI 显示“寻踪进度：2/4”。
/// 集齐后播放通关演出：影子出现 → 黑幕 → 场景变化（设置 Flag，门由 Flag 打开）。
/// 每个游戏场景放一个。
/// </summary>
public class SceneClueTracker : MonoBehaviour
{
    // 用于生成场景通关状态的唯一标识。
    [Header("场景标识（用于生成通关 Flag: scene_cleared_<sceneId>）")]
    // 同时也是存档中对应 Flag 的后缀。
    [SerializeField] private string sceneId;

    // 本场景必须收集的线索集合。
    [Header("本场景的关键线索（3-4 个）")]
    // 进度和通关条件均以该数组为准。
    [SerializeField] private ClueData[] keyClues;

    // 屏幕角落的进度文本及其格式。
    [Header("进度 UI（屏幕角落）")]
    // 若配置该文本，则实时写入收集进度。
    [SerializeField] private TMP_Text progressText;
    // 控制进度字符串中当前值与总数的排版。
    [SerializeField] private string progressFormat = "寻踪进度：{0}/{1}";

    // 通关时可选播放的视觉与对话演出资源。
    [Header("通关演出")]
    // 天台模式下要求真相 Flag 已经设置。
    [Tooltip("勾选 = 只有 truth_revealed 已设置才播通关演出（天台专用：区分真/坏结局）")]
    // 关闭时所有场景都只按关键线索数量判定。
    [SerializeField] private bool requireTruthRevealed = false;
    // 通关瞬间短暂显示的场景对象。
    [Tooltip("“哥哥”的影子（场景里预放好，默认隐藏）")]
    // 演出开始时启用，黑幕完全覆盖后再次隐藏。
    [SerializeField] private GameObject brotherShadow;
    // 影子保持可见的时间长度。
    [Tooltip("影子展示秒数")]
    [SerializeField] private float shadowDuration = 2f;
    // 承载全屏淡入淡出效果的画布组。
    [Tooltip("全屏黑幕 CanvasGroup")]
    [SerializeField] private CanvasGroup blackout;
    // 黑幕从透明到不透明或反向变化的用时。
    [SerializeField] private float blackoutFadeDuration = 0.6f;
    // 影子展示完成后黑幕停留的时长。
    [SerializeField] private float blackoutHoldDuration = 1f;
    // 黑幕后可选播放的结算独白。
    [Tooltip("黑幕后播放的独白（可空）")]
    [SerializeField] private DialogueData clearMonologue;

    // 此属性保持通关 Flag 的命名一致。
    // 使用场景标识生成对应的通关状态 Flag。
    private string ClearedFlag => $"scene_cleared_{sceneId}";

    private bool clearSequencePlaying;

    // Unity 启动时重置临时演出对象并注册监听。
    private void Start()
    {
        // 每次进入场景先重置影子，等待通关演出触发。
        // 影子对象未配置时跳过展示，继续后续流程。
        if (brotherShadow != null)
        {
            brotherShadow.SetActive(false);
        }

        // 黑幕初始为透明且隐藏，避免遮挡场景加载画面。
        if (blackout != null)
        {
            blackout.alpha = 0f;
            blackout.gameObject.SetActive(false);
        }

        // 同时监听线索与 Flag，支持天台真相 Flag 的补触发。
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClueCollected += HandleClueCollected;
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
        }

        // 初始化 UI，使已读档的进度立即可见。
        RefreshProgressUI();
        // 读档后可能已经集齐核心线索但尚未写入清场 Flag。
        TryTriggerClear();
    }

    // Unity 销毁时撤销对全局事件的监听。
    private void OnDestroy()
    {
        clearSequencePlaying = false;

        // 解除事件订阅，防止对象销毁后继续响应。
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
        }
    }

    // 统计当前存档中已取得的本场景关键线索。
    private int CountCollected()
    {
        // 没有管理器或线索数组时，进度固定为零。
        GameManager gm = GameManager.Instance;
        if (gm == null || keyClues == null)
        {
            return 0;
        }

        int count = 0;
        // 线索清单中的空项不会计入已收集数量。
        // 逐项检查配置的关键线索是否已写入玩家记录。
        foreach (ClueData clue in keyClues)
        {
            if (clue != null && gm.HasClue(clue.ClueId))
            {
                count++;
            }
        }

        return count;
    }

    private int CountRequiredClues()
    {
        if (keyClues == null)
        {
            return 0;
        }

        int count = 0;
        foreach (ClueData clue in keyClues)
        {
            if (clue != null && !string.IsNullOrEmpty(clue.ClueId))
            {
                count++;
            }
        }

        return count;
    }

    private void RefreshProgressUI()
    {
        // UI 与线索配置齐全时，按格式显示当前收集数量。
        if (progressText != null && keyClues != null)
        {
            progressText.text = string.Format(progressFormat, CountCollected(), CountRequiredClues());
        }
    }

    // 处理线索事件；参数无需读取，因为计数会重新查询存档。
    private void HandleClueCollected(ClueData _)
    {
        // 收集任意线索后刷新显示，并检查是否刚好集齐。
        RefreshProgressUI();
        TryTriggerClear();
    }

    // Flag 批量变化后检查真相分支，避免每个 Flag 重复计算。
    private void HandleFlagsChanged()
    {
        if (requireTruthRevealed)
        {
            TryTriggerClear();
        }
    }

    // 在条件变动后验证是否应启动一次通关演出。
    private void TryTriggerClear()
    {
        // 已通关的场景不重复启动演出协程。
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.HasFlag(ClearedFlag) || clearSequencePlaying)
        {
            return;
        }

        // 天台专用门：坏结局不设 truth_revealed，集齐线索也不通关（交给 EndingGate 收尾）
        if (requireTruthRevealed && !gm.HasFlag("truth_revealed"))
        {
            return;
        }

        // 配置了至少一项关键线索且全部收集后才播放演出。
        int requiredCount = CountRequiredClues();
        if (requiredCount > 0 && CountCollected() >= requiredCount)
        {
            // 启动后由协程负责等待已有演出结束。
            clearSequencePlaying = true;
            StartCoroutine(PlayClearSequence());
        }
    }

    // 依次执行等待、影子、黑幕、状态更新和独白。
    private IEnumerator PlayClearSequence()
    {
        // 使用逐帧等待，避免阻塞主线程和 UI 刷新。
        // 等收尾对话（最后一条线索的调查对话）关闭
        while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
            yield return null;
        }

        // 等回溯闪回演出结束，避免叠加
        while (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback)
        {
            yield return null;
        }

        // 影子出现
        // 影子对象未配置时跳过展示，继续后续流程。
        if (brotherShadow != null)
        {
            brotherShadow.SetActive(true);
            yield return new WaitForSeconds(shadowDuration);
        }

        // 黑幕
        if (blackout != null)
        {
            // 激活画布后，从透明淡入至全黑。
            blackout.gameObject.SetActive(true);
            yield return FadeBlackout(0f, 1f);

            // 黑幕覆盖后再隐藏影子，形成短暂的显现效果。
            // 影子对象未配置时跳过展示，继续后续流程。
            if (brotherShadow != null)
            {
                brotherShadow.SetActive(false);
            }

            // 保持黑幕，为场景内状态变化预留时间。
            yield return new WaitForSeconds(blackoutHoldDuration);

            // 通关 Flag 在黑幕中设置——场景门/物件在黑幕里完成变化
            GameManager.Instance.SetFlag(ClearedFlag);

            // 状态更新完成后淡回游戏画面并隐藏画布。
            yield return FadeBlackout(1f, 0f);
            blackout.gameObject.SetActive(false);
        }
        else
        {
            // 未配置黑幕时仍必须写入通关 Flag。
            GameManager.Instance.SetFlag(ClearedFlag);
        }

        // 演出完成后，如有配置则播放场景通关独白。
        if (clearMonologue != null && DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.StartDialogue(clearMonologue);
        }

        clearSequencePlaying = false;
    }

    // 在指定起止透明度之间逐帧执行黑幕过渡。
    private IEnumerator FadeBlackout(float from, float to)
    {
        // 黑幕过渡时间可配置为零，零时直接写入目标值。
        if (blackoutFadeDuration <= 0f)
        {
            blackout.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < blackoutFadeDuration)
        {
            elapsed += Time.deltaTime;
            blackout.alpha = Mathf.Lerp(from, to, elapsed / blackoutFadeDuration);
            yield return null;
        }

        // 循环结束后写入精确目标值，消除插值余量。
        blackout.alpha = to;
    }
}
