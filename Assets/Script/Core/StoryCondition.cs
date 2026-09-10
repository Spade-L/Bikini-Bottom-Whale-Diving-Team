using UnityEngine;

// 更新当前逻辑
[System.Serializable]
public class StoryCondition
{
// 配置 时间段（-1 表示不限制） 分组
    [Header("时间段（-1 表示不限制）")]
    // 两端均为可选边界；未填写的一端不会参与比较
    public int minTimePeriod = -1;
    public int maxTimePeriod = -1;

// 配置 Flag 条件 分组
    [Header("Flag 条件")]
    [Tooltip("必须全部已设置")]
// 保存 requiredFlags 数据
    public string[] requiredFlags;
    [Tooltip("任意一个已设置则不满足")]
// 保存 forbiddenFlags 数据
    public string[] forbiddenFlags;

// 配置 线索条件 分组
    [Header("线索条件")]
    [Tooltip("必须全部已收集（填 ClueId）")]
// 保存 requiredClues 数据
    public string[] requiredClues;

    // 按时间、Flag、线索顺序短路判定；任一条件未满足即不可用
    public bool IsMet()
    {
        // 尚未建立全局状态时不能安全满足任何剧情门槛
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
// 返回当前结果
            return false;
        }

// 判断当前条件
        if (minTimePeriod >= 0 && gm.CurrentTimePeriod < minTimePeriod)
        {
// 返回当前结果
            return false;
        }

// 判断当前条件
        if (maxTimePeriod >= 0 && gm.CurrentTimePeriod > maxTimePeriod)
        {
// 返回当前结果
            return false;
        }

        // requiredFlags 采用“全部满足”；空字符串作为未配置项跳过
        if (requiredFlags != null)
        {
// 遍历全部元素
            foreach (string flag in requiredFlags)
            {
// 判断当前条件
                if (!string.IsNullOrEmpty(flag) && !gm.HasFlag(flag))
                {
// 返回当前结果
                    return false;
                }
            }
        }

        // forbiddenFlags 采用“任一阻止”，适合互斥剧情分支
        if (forbiddenFlags != null)
        {
// 遍历全部元素
            foreach (string flag in forbiddenFlags)
            {
                if (!string.IsNullOrEmpty(flag) && gm.HasFlag(flag))
                {
// 返回当前结果
                    return false;
                }
            }
        }

        // 线索要求同样必须全部收集，防止只获得部分证据就解锁后续
        if (requiredClues != null)
        {
// 遍历全部元素
            foreach (string clueId in requiredClues)
            {
                if (!string.IsNullOrEmpty(clueId) && !gm.HasClue(clueId))
                {
// 返回当前结果
                    return false;
                }
            }
        }

// 返回当前结果
        return true;
    }
}
