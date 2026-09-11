using UnityEngine;

// 推进 当前脚本 的当前步骤
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
// 同步 StoryCondition 的相关数据
    public string[] requiredFlags;
    [Tooltip("任意一个已设置则不满足")]
// 同步 StoryCondition 的相关数据（StoryCondition 后续步骤）
    public string[] forbiddenFlags;

// 配置 线索条件 分组
    [Header("线索条件")]
    [Tooltip("必须全部已收集（填 ClueId）")]
// 同步 StoryCondition 的相关数据（StoryCondition 后续步骤）（24）
    public string[] requiredClues;

    // 判断 IsMet 对应条件
    public bool IsMet()
    {
        // 尚未建立全局状态时不能安全满足任何剧情门槛
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
// 返回 IsMet 的处理结果
            return false;
        }

// 检查 IsMet 的前置条件
        if (minTimePeriod >= 0 && gm.CurrentTimePeriod < minTimePeriod)
        {
// 在 IsMet 中处理 返回 IsMet 的处理结果
            return false;
        }

// 在 IsMet 中处理 检查 IsMet 的前置条件
        if (maxTimePeriod >= 0 && gm.CurrentTimePeriod > maxTimePeriod)
        {
// 返回 IsMet 的处理结果（IsMet）
            return false;
        }

        // requiredFlags 采用“全部满足”；空字符串作为未配置项跳过
        if (requiredFlags != null)
        {
// 遍历全部元素
            foreach (string flag in requiredFlags)
            {
// 检查 IsMet 的前置条件（IsMet）
                if (!string.IsNullOrEmpty(flag) && !gm.HasFlag(flag))
                {
// 返回 IsMet 的处理结果（IsMet）（return）
                    return false;
                }
            }
        }

        // forbiddenFlags 采用“任一阻止”，适合互斥剧情分支
        if (forbiddenFlags != null)
        {
// 在 IsMet 中继续当前处理
            foreach (string flag in forbiddenFlags)
            {
                if (!string.IsNullOrEmpty(flag) && gm.HasFlag(flag))
                {
// 在 IsMet 中继续当前处理（IsMet 后续步骤）
                    return false;
                }
            }
        }

        // 线索要求同样必须全部收集，防止只获得部分证据就解锁后续
        if (requiredClues != null)
        {
// 在 IsMet 中继续当前处理（IsMet 后续步骤）（84）
            foreach (string clueId in requiredClues)
            {
                if (!string.IsNullOrEmpty(clueId) && !gm.HasClue(clueId))
                {
// 在 IsMet 中继续当前处理（IsMet 后续步骤）（89）
                    return false;
                }
            }
        }

// 在 IsMet 中继续当前处理（IsMet 后续步骤）（95）
        return true;
    }
}
