using UnityEngine;

[CreateAssetMenu(fileName = "Clue_", menuName = "游戏数据/线索")]
// 定义 ClueData 类型
public class ClueData : ScriptableObject
{
    [Header("标识（全局唯一，存档用）")]
// 保存 clueId 数据
    [SerializeField] private string clueId;

    // 以下字段只负责日志展示，不参与线索是否已收集的判定
    [Header("线索日志中的展示")]
// 保存 title 数据
    [SerializeField] private string title;
    [TextArea(3, 8)]
// 保存 description 数据
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    // trueMeaning 仅在特定剧情 Flag 成立时覆盖表层解读；两者可独立使用文本令牌
    [Header("叙事")]
    [Tooltip("表层含义 —— 玩家一开始看到的解读")]
// 调用 TextArea
    [TextArea(2, 5)]
    [SerializeField] private string surfaceMeaning;
// 说明当前配置
    [Tooltip("真相 —— 结局揭晓后线索日志替换为这段文字（其实是主角自己留下的痕迹）")]
    [TextArea(2, 5)]
// 保存 trueMeaning 数据
    [SerializeField] private string trueMeaning;

    public string ClueId => clueId;
// 保存 Title 数据
    public string Title => title;
    public string Description => description;
// 保存 Icon 数据
    public Sprite Icon => icon;
    public string SurfaceMeaning => surfaceMeaning;
// 保存 TrueMeaning 数据
    public string TrueMeaning => trueMeaning;

    /// <summary>结局后（truth_revealed flag）返回真相文本，否则返回表层文本。</summary>
    public string GetCurrentMeaning()
    {
// 定义 HasFlag 方法
        bool revealed = GameManager.Instance != null && GameManager.Instance.HasFlag("truth_revealed");
        return revealed && !string.IsNullOrEmpty(trueMeaning) ? trueMeaning : surfaceMeaning;
    }
}
