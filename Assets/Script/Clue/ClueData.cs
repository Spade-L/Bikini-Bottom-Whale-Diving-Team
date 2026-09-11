using UnityEngine;

[CreateAssetMenu(fileName = "Clue_", menuName = "游戏数据/线索")]
// 保存线索展示文本与真相内容
public class ClueData : ScriptableObject
{
    [Header("标识（全局唯一，存档用）")]
// 同步 ClueData 的相关数据
    [SerializeField] private string clueId;

    // 以下字段只负责日志展示，不参与线索是否已收集的判定
    [Header("线索日志中的展示")]
// 同步 ClueData 的相关数据（ClueData 后续步骤）
    [SerializeField] private string title;
    [TextArea(3, 8)]
// 同步 ClueData 的相关数据（ClueData 后续步骤）（15）
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    // trueMeaning 仅在特定剧情 Flag 成立时覆盖表层解读；两者可独立使用文本令牌
    [Header("叙事")]
    [Tooltip("表层含义 —— 玩家一开始看到的解读")]
// 使用 ClueData 所需功能
    [TextArea(2, 5)]
    [SerializeField] private string surfaceMeaning;
// 说明当前配置
    [Tooltip("真相 —— 结局揭晓后线索日志替换为这段文字（其实是主角自己留下的痕迹）")]
    [TextArea(2, 5)]
// 同步 ClueData 的相关数据（ClueData 后续步骤）（28）
    [SerializeField] private string trueMeaning;

    public string ClueId => clueId;
// 同步 ClueData 的相关数据（ClueData 后续步骤）（32）
    public string Title => title;
    public string Description => description;
// 同步 ClueData 的相关数据（ClueData 后续步骤）（35）
    public Sprite Icon => icon;
    public string SurfaceMeaning => surfaceMeaning;
// 同步 ClueData 的相关数据（ClueData 后续步骤）（38）
    public string TrueMeaning => trueMeaning;

    // 获取 GetCurrentMeaning 所需引用
    public string GetCurrentMeaning()
    {
// 判断 HasFlag 对应条件
        bool revealed = GameManager.Instance != null && GameManager.Instance.HasFlag("truth_revealed");
        return revealed && !string.IsNullOrEmpty(trueMeaning) ? trueMeaning : surfaceMeaning;
    }
}
