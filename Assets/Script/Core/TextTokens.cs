using UnityEngine;

// 替换对白中的角色关系占位符
public static class TextTokens
{
// 推进 TextTokens 的当前步骤
    public const string FemaleFlag = "gender_female";

// 解析 Resolve 对应结果
    public static string Resolve(string raw)
    {
// 检查 Resolve 的前置条件
        if (string.IsNullOrEmpty(raw))
        {
            return raw;
        }

// 判断 HasFlag 对应条件
        bool female = GameManager.Instance != null && GameManager.Instance.HasFlag(FemaleFlag);

// 返回 Resolve 的处理结果
        return raw
            .Replace("{sibling}", female ? "姐姐" : "哥哥")
// 使用 Resolve 所需功能
            .Replace("{ta}", female ? "她" : "他")
            .Replace("{kin}", female ? "好姐妹" : "好兄弟");
    }
}
