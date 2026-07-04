using UnityEngine;

/// <summary>
/// 一段完整对话：若干行 + 播放完毕后的剧情效果。
/// 在 Project 窗口右键 Create > 游戏数据 > 对话 创建。
/// </summary>
[CreateAssetMenu(fileName = "Dialogue_", menuName = "游戏数据/对话")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class Line
    {
        [Tooltip("说话人立绘（空 = 本行不显示立绘，用于旁白）")]
        public CharacterData character;

        [Tooltip("表情差分名（normal/worried/shocked…），空 = 用默认立绘")]
        public string expression;

        [Tooltip("说话人名字。留空时：有立绘则用立绘的 displayName，否则不显示名字栏")]
        public string speakerName;

        [TextArea(2, 5)]
        public string text;

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
