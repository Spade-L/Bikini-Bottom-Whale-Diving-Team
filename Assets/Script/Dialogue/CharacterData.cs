using UnityEngine;

/// <summary>
/// 角色立绘定义：一张基础立绘 + 一组按名字索引的表情差分。
/// 主角（哥哥/姐姐版本各建一个资产，或男女主各一个）、重要 NPC 各建一个。
/// </summary>
[CreateAssetMenu(fileName = "Char_", menuName = "游戏数据/角色立绘")]
public class CharacterData : ScriptableObject
{
    // 嵌套类型需序列化，才能在角色资产的 Inspector 中编辑每个表情项。
    [System.Serializable]
    public class Expression
    {
        // 名称按区分大小写的精确字符串匹配；对话资产必须与这里保持一致。
        [Tooltip("表情名，对话行里填这个。建议统一：normal / worried / shocked / sad / doubt / smile")]
        public string expressionName;
        public Sprite portrait;
    }
    // 仅存视觉数据。
    // 顺序不影响查找。
    // 空数组安全回退。
    // 名称可被覆写。
    [Header("显示名（对话框名字栏用，可被对话行覆盖）")]
    public string displayName;

    // 默认立绘既是未填表情时的显示，也是配置缺失时的安全兜底。
    [Header("默认立绘（找不到表情时兜底）")]
    public Sprite defaultPortrait;

    [Header("表情差分")]
    public Expression[] expressions;

    /// <summary>
    /// 返回指定表情；未填写、未找到或表情项未配置图片时均回退默认立绘。
    /// 警告只在填写了不存在的名称时输出，便于定位对话资产的拼写错误。
    /// </summary>
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
