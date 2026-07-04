using UnityEngine;

/// <summary>
/// 通用剧情条件：时间段范围 + 需要/排除的 Flag + 需要的线索。
/// 挂在对话行、NPC 出场、拾取物上复用。
/// </summary>
[System.Serializable]
public class StoryCondition
{
    [Header("时间段（-1 表示不限制）")]
    public int minTimePeriod = -1;
    public int maxTimePeriod = -1;

    [Header("Flag 条件")]
    [Tooltip("必须全部已设置")]
    public string[] requiredFlags;
    [Tooltip("任意一个已设置则不满足")]
    public string[] forbiddenFlags;

    [Header("线索条件")]
    [Tooltip("必须全部已收集（填 ClueId）")]
    public string[] requiredClues;

    public bool IsMet()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return false;
        }

        if (minTimePeriod >= 0 && gm.CurrentTimePeriod < minTimePeriod)
        {
            return false;
        }

        if (maxTimePeriod >= 0 && gm.CurrentTimePeriod > maxTimePeriod)
        {
            return false;
        }

        if (requiredFlags != null)
        {
            foreach (string flag in requiredFlags)
            {
                if (!string.IsNullOrEmpty(flag) && !gm.HasFlag(flag))
                {
                    return false;
                }
            }
        }

        if (forbiddenFlags != null)
        {
            foreach (string flag in forbiddenFlags)
            {
                if (!string.IsNullOrEmpty(flag) && gm.HasFlag(flag))
                {
                    return false;
                }
            }
        }

        if (requiredClues != null)
        {
            foreach (string clueId in requiredClues)
            {
                if (!string.IsNullOrEmpty(clueId) && !gm.HasClue(clueId))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
