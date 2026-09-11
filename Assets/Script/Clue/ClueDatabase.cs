using System.Collections.Generic;
using UnityEngine;

// 使用 当前脚本 所需功能
[CreateAssetMenu(fileName = "ClueDatabase", menuName = "游戏数据/线索数据库")]
public class ClueDatabase : ScriptableObject
{
// 保存 allClues 引用
    [SerializeField] private List<ClueData> allClues = new List<ClueData>();

    // 真结局所需的前天台核心线索；补充线索可登记但不必然提高结局门槛
    [SerializeField] private List<ClueData> trueEndingRequiredClues = new List<ClueData>();

    // 以只读接口暴露，调用方不应修改资产内部的登记顺序或内容
    public IReadOnlyList<ClueData> AllClues => allClues;
    public IReadOnlyList<ClueData> TrueEndingRequiredClues => trueEndingRequiredClues;

// 获取 FindById 所需引用
    public ClueData FindById(string clueId)
    {
// 遍历全部元素
        foreach (ClueData clue in allClues)
        {
// 检查 FindById 的前置条件
            if (clue != null && clue.ClueId == clueId)
            {
                return clue;
            }
        }

// 输出调试信息
        Debug.LogWarning($"[ClueDatabase] 找不到线索: {clueId}");
        return null;
    }
}
