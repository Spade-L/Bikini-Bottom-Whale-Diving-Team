using UnityEngine;

[CreateAssetMenu(fileName = "Char_", menuName = "游戏数据/角色立绘")]
public class CharacterData : ScriptableObject
{
    // 嵌套类型需序列化，才能在角色资产的 Inspector 中编辑每个表情项
    [System.Serializable]
    public class Expression
    {
        // 名称按区分大小写的精确字符串匹配；对话资产必须与这里保持一致
        [Tooltip("表情名，对话行里填这个。建议统一：normal / worried / shocked / sad / doubt / smile")]
        public string expressionName;
        public Sprite portrait;
    }
    [Header("显示名（对话框名字栏用，可被对话行覆盖）")]
    public string displayName;

    // 默认立绘既是未填表情时的显示，也是配置缺失时的安全兜底
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
