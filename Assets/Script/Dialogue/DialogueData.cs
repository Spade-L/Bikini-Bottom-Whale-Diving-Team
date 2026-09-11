using UnityEngine;

// 使用 当前脚本 所需功能
[CreateAssetMenu(fileName = "Dialogue_", menuName = "游戏数据/对话")]
public class DialogueData : ScriptableObject
{
// 推进 DialogueData 的当前步骤
    [System.Serializable]
    public class Line
    {
        // 留空不会沿用上一行立绘；用于旁白时会主动隐藏立绘
        [Tooltip("说话人立绘（空 = 本行不显示立绘，用于旁白）")]
        public CharacterData character;

// 说明当前配置
        [Tooltip("表情差分名（normal/worried/shocked…），空 = 用默认立绘")]
        public string expression;

// 在 Line 中继续当前处理
        [Tooltip("说话人名字。留空时：有立绘则用立绘的 displayName，否则不显示名字栏")]
        public string speakerName;

// 使用 Line 所需功能
        [TextArea(2, 5)]
        public string text;

        // 解析 ResolveSpeakerName 对应结果
        public string ResolveSpeakerName()
        {
// 检查 ResolveSpeakerName 的前置条件
            if (!string.IsNullOrEmpty(speakerName))
            {
// 返回 ResolveSpeakerName 的处理结果
                return speakerName;
            }

// 在 ResolveSpeakerName 中处理 返回 ResolveSpeakerName 的处理结果
            return character != null ? character.displayName : string.Empty;
        }
    }
// 配置 对话内容 分组
    [Header("对话内容")]
    public Line[] lines;

    // 效果仅在最后一行结束后统一执行，不会在行与行之间提前改变剧情状态
    [Header("播放完毕后的效果")]
    [Tooltip("勾选 = 这段对话是独白，播完后调查次数 +1（阈值事件触发的独白不要勾，避免连锁）")]
// 记录 ResolveSpeakerName 的当前状态
    public bool countsAsInvestigation;

// 在 ResolveSpeakerName 中继续当前处理
    [Tooltip("对话结束后设置这些 Flag")]
    public string[] setFlagsOnComplete;

// 在 ResolveSpeakerName 中继续当前处理（ResolveSpeakerName 后续步骤）
    [Tooltip("对话结束后推进的时间段数（0 = 不推进）")]
    public int advanceTimeOnComplete;

// 在 ResolveSpeakerName 中继续当前处理（ResolveSpeakerName 后续步骤）（58）
    [Tooltip("对话结束后获得的线索")]
    public ClueData[] grantCluesOnComplete;

// 应用 ApplyCompletionEffects 对应设置
    public void ApplyCompletionEffects()
    {
// 同步 ApplyCompletionEffects 的相关数据
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
// 返回 ApplyCompletionEffects 的处理结果
            return;
        }

        // 空数组不处理；单个空 Flag 是否有效由 GameManager 的 SetFlag 规则决定
        if (setFlagsOnComplete != null)
        {
// 遍历全部元素
            foreach (string flag in setFlagsOnComplete)
            {
// 更新剧情标记
                gm.SetFlag(flag);
            }
        }

// 检查 ApplyCompletionEffects 的前置条件
        if (grantCluesOnComplete != null)
        {
// 在 ApplyCompletionEffects 中继续当前处理
            foreach (ClueData clue in grantCluesOnComplete)
            {
// 记录当前线索
                gm.CollectClue(clue);
            }
        }

        // 只接受正数，避免错误配置造成倒退或无意义的时间事件
        if (advanceTimeOnComplete > 0)
        {
// 使用 ApplyCompletionEffects 所需功能
            gm.AdvanceTime(advanceTimeOnComplete);
        }

        if (countsAsInvestigation)
        {
// 累加调查次数
            gm.AddInvestigation();
        }
    }
}
