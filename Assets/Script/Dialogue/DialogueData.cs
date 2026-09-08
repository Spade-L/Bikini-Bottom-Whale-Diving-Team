using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue_", menuName = "游戏数据/对话")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class Line
    {
        // 留空不会沿用上一行立绘；用于旁白时会主动隐藏立绘
        [Tooltip("说话人立绘（空 = 本行不显示立绘，用于旁白）")]
        public CharacterData character;

        [Tooltip("表情差分名（normal/worried/shocked…），空 = 用默认立绘")]
        public string expression;

        [Tooltip("说话人名字。留空时：有立绘则用立绘的 displayName，否则不显示名字栏")]
        public string speakerName;

        [TextArea(2, 5)]
        public string text;

        // 覆写名优先于角色资产显示名；两者为空时 UI 会隐藏名字栏
        public string ResolveSpeakerName()
        {
            if (!string.IsNullOrEmpty(speakerName))
            {
                return speakerName;
            }

            return character != null ? character.displayName : string.Empty;
        }
    }
    [Header("对话内容")]
    public Line[] lines;

    // 效果仅在最后一行结束后统一执行，不会在行与行之间提前改变剧情状态
    [Header("播放完毕后的效果")]
    [Tooltip("勾选 = 这段对话是独白，播完后调查次数 +1（阈值事件触发的独白不要勾，避免连锁）")]
    public bool countsAsInvestigation;

    [Tooltip("对话结束后设置这些 Flag")]
    public string[] setFlagsOnComplete;

    [Tooltip("对话结束后推进的时间段数（0 = 不推进）")]
    public int advanceTimeOnComplete;

    [Tooltip("对话结束后获得的线索")]
    public ClueData[] grantCluesOnComplete;

    public void ApplyCompletionEffects()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }

        // 空数组不处理；单个空 Flag 是否有效由 GameManager 的 SetFlag 规则决定
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

        // 只接受正数，避免错误配置造成倒退或无意义的时间事件
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
