using UnityEngine;

public static class TextTokens
{
    public const string FemaleFlag = "gender_female";

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
