using UnityEngine;

/// <summary>
/// 一条线索。ClueId 是全局唯一标识，用于存档和条件判断。
/// 命名建议按章节前缀：ch1_letter、ch2_photo_torn 等。
/// </summary>
[CreateAssetMenu(fileName = "Clue_", menuName = "游戏数据/线索")]
public class ClueData : ScriptableObject
{
    // 存档稳定键。
    // 不随资源名变化。
    // 不参与本地化。
    // 应保持非空。
    // 用于反查。
    // 用于去重。
    [Header("标识（全局唯一，存档用）")]
    [SerializeField] private string clueId;

    // 以下字段只负责日志展示，不参与线索是否已收集的判定。
    [Header("线索日志中的展示")]
    [SerializeField] private string title;
    [TextArea(3, 8)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    // trueMeaning 仅在特定剧情 Flag 成立时覆盖表层解读；两者可独立使用文本令牌。
    [Header("叙事")]
    [Tooltip("表层含义 —— 玩家一开始看到的解读")]
    [TextArea(2, 5)]
    [SerializeField] private string surfaceMeaning;
    [Tooltip("真相 —— 结局揭晓后线索日志替换为这段文字（其实是主角自己留下的痕迹）")]
    [TextArea(2, 5)]
    [SerializeField] private string trueMeaning;

    public string ClueId => clueId;
    public string Title => title;
    public string Description => description;
    public Sprite Icon => icon;
    public string SurfaceMeaning => surfaceMeaning;
    public string TrueMeaning => trueMeaning;

    /// <summary>结局后（truth_revealed flag）返回真相文本，否则返回表层文本。</summary>
    public string GetCurrentMeaning()
    {
        bool revealed = GameManager.Instance != null && GameManager.Instance.HasFlag("truth_revealed");
        return revealed && !string.IsNullOrEmpty(trueMeaning) ? trueMeaning : surfaceMeaning;
    }
}
