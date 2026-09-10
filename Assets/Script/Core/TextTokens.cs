using UnityEngine;

// 定义 TextTokens 类型
public static class TextTokens
{
// 更新当前逻辑
    public const string FemaleFlag = "gender_female";

// 定义 Resolve 方法
    public static string Resolve(string raw)
    {
// 判断当前条件
        if (string.IsNullOrEmpty(raw))
        {
            return raw;
        }

// 定义 HasFlag 方法
        bool female = GameManager.Instance != null && GameManager.Instance.HasFlag(FemaleFlag);

// 返回当前结果
        return raw
            .Replace("{sibling}", female ? "姐姐" : "哥哥")
// 调用 Replace
            .Replace("{ta}", female ? "她" : "他")
            .Replace("{kin}", female ? "好姐妹" : "好兄弟");
    }
}
