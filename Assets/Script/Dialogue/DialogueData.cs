using UnityEngine;

/// <summary>
/// 一段完整对话：若干行 + 播放完毕后的剧情效果。
/// 在 Project 窗口右键 Create > 游戏数据 > 对话 创建。
/// </summary>
[CreateAssetMenu(fileName = "Dialogue_", menuName = "游戏数据/对话")]
public class DialogueData : ScriptableObject
{
    // 此资产只定义静态内容和结束效果；当前行索引、打字状态由 DialogueUIManager 在运行时维护。
    // 配置顺序即完成顺序：Flag、线索、正向时间推进、调查计数依次执行。
    // 因效果仅在正常播放至末行后执行，不能把需要立即生效的状态放进这里。
    // 同时配置 countsAsInvestigation 与外部交互计数时会累加，策划需明确唯一计数来源。
    // 对话文本与显示名可含 TextTokens，解析发生在 UI 展示时而不是资产加载时。
    // 一行是 UI 逐次展示与输入推进的最小单位，数组顺序即播放顺序。
    [System.Serializable]
    public class Line
    {
        // 留空不会沿用上一行立绘；用于旁白时会主动隐藏立绘。
        [Tooltip("说话人立绘（空 = 本行不显示立绘，用于旁白）")]
        public CharacterData character;

        [Tooltip("表情差分名（normal/worried/shocked…），空 = 用默认立绘")]
        public string expression;

        [Tooltip("说话人名字。留空时：有立绘则用立绘的 displayName，否则不显示名字栏")]
        public string speakerName;

        [TextArea(2, 5)]
        public string text;

        // 覆写名优先于角色资产显示名；两者为空时 UI 会隐藏名字栏。
        public string ResolveSpeakerName()
        {
            if (!string.IsNullOrEmpty(speakerName))
            {
                return speakerName;
            }

            return character != null ? character.displayName : string.Empty;
        }
    }
    // 可用于旁白。
    // 表情依赖角色。
    // UI 负责逐行显示。
    // 空管理器跳过效果。
    // Flag 按顺序设置。
    // 线索按顺序收集。
    // 仅推进正时间。
    // 最后增加计数。
    // 可跨场景复用。
    // 顺序即播放顺序。
    [Header("对话内容")]
    public Line[] lines;

    // 效果仅在最后一行结束后统一执行，不会在行与行之间提前改变剧情状态。
    [Header("播放完毕后的效果")]
    [Tooltip("勾选 = 这段对话是独白，播完后调查次数 +1（阈值事件触发的独白不要勾，避免连锁）")]
    public bool countsAsInvestigation;

    [Tooltip("对话结束后设置这些 Flag")]
    public string[] setFlagsOnComplete;

    [Tooltip("对话结束后推进的时间段数（0 = 不推进）")]
    public int advanceTimeOnComplete;

    [Tooltip("对话结束后获得的线索")]
    public ClueData[] grantCluesOnComplete;

    /// <summary>
    /// 在对话 UI 关闭后由管理器调用，按固定顺序应用剧情效果。
    /// Flag 与线索会先触发各自事件；时间推进和调查计数随后执行，配置时应避免重复计数。
    /// </summary>
    public void ApplyCompletionEffects()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }

        // 空数组不处理；单个空 Flag 是否有效由 GameManager 的 SetFlag 规则决定。
        if (setFlagsOnComplete != null)
        {
            foreach (string flag in setFlagsOnComplete)
            {
                gm.SetFlag(flag);
            }
        }

        if (grantCluesOnComplete != null)
        {
            foreach (ClueData clue in grantCluesOnComplete)
            {
                gm.CollectClue(clue);
            }
        }

        // 只接受正数，避免错误配置造成倒退或无意义的时间事件。
        if (advanceTimeOnComplete > 0)
        {
            gm.AdvanceTime(advanceTimeOnComplete);
        }

        if (countsAsInvestigation)
        {
            gm.AddInvestigation();
        }
    }
}
