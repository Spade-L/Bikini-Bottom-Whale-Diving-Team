using UnityEngine;

/// <summary>
/// 文本占位符：所有对话/线索文本里可用的性别相关 token。
/// 玩家开局选择性别后（女性 = 设置 gender_female flag），显示时自动替换：
///   {sibling} → 哥哥 / 姐姐
///   {ta}      → 他 / 她
///   {kin}     → 好兄弟 / 好姐妹
/// </summary>
public static class TextTokens
{
    public const string FemaleFlag = "gender_female";

    // 在文本显示阶段按当前全局性别 Flag 统一解析，避免在资源中复制性别分支文本。
    // 空字符串原样返回；管理器尚未创建时默认采用男性文案。
    public static string Resolve(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return raw;
        }

        bool female = GameManager.Instance != null && GameManager.Instance.HasFlag(FemaleFlag);

        return raw
            .Replace("{sibling}", female ? "姐姐" : "哥哥")
            .Replace("{ta}", female ? "她" : "他")
            .Replace("{kin}", female ? "好姐妹" : "好兄弟");
    }
}
