using UnityEngine;

/// <summary>
/// 通用剧情条件：时间段范围 + 需要/排除的 Flag + 需要的线索。
/// 挂在对话行、NPC 出场、拾取物上复用。
/// </summary>
[System.Serializable]
// 剧情条件
// 离散时间
// -1 不限
// 全部 Flag
// 禁止 Flag
// 全部线索
// 忽略空项
// 无管理器失败
// 顺序短路
// 减少查询
// 只读状态
// 数组可空
// 包含下限
// 包含上限
// Flag 前置
// Flag 互斥
// 线索前置
// Unity 序列化
// 使用者处理
public class StoryCondition
{
    [Header("时间段（-1 表示不限制）")]
    // 两端均为可选边界；未填写的一端不会参与比较。
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

    // 按时间、Flag、线索顺序短路判定；任一条件未满足即不可用。
    public bool IsMet()
    {
        // 尚未建立全局状态时不能安全满足任何剧情门槛。
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

        // requiredFlags 采用“全部满足”；空字符串作为未配置项跳过。
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

        // forbiddenFlags 采用“任一阻止”，适合互斥剧情分支。
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

        // 线索要求同样必须全部收集，防止只获得部分证据就解锁后续。
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
