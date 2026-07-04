using UnityEngine;

/// <summary>
/// 角色立绘定义：一张基础立绘 + 一组按名字索引的表情差分。
/// 主角（哥哥/姐姐版本各建一个资产，或男女主各一个）、重要 NPC 各建一个。
/// </summary>
[CreateAssetMenu(fileName = "Char_", menuName = "游戏数据/角色立绘")]
public class CharacterData : ScriptableObject
{
    [System.Serializable]
    public class Expression
    {
        [Tooltip("表情名，对话行里填这个。建议统一：normal / worried / shocked / sad / doubt / smile")]
        public string expressionName;
        public Sprite portrait;
    }

    [Header("显示名（对话框名字栏用，可被对话行覆盖）")]
    public string displayName;

    [Header("默认立绘（找不到表情时兜底）")]
    public Sprite defaultPortrait;

    [Header("表情差分")]
    public Expression[] expressions;

    public Sprite GetPortrait(string expressionName)
    {
        if (!string.IsNullOrEmpty(expressionName) && expressions != null)
        {
            foreach (Expression expr in expressions)
            {
                if (expr.expressionName == expressionName)
                {
                    return expr.portrait != null ? expr.portrait : defaultPortrait;
                }
            }

            Debug.LogWarning($"[CharacterData] {name} 缺少表情差分: {expressionName}，使用默认立绘");
        }

        return defaultPortrait;
    }
}
