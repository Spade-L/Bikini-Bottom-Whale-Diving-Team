using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有线索的登记表，用于从存档中的 ClueId 找回 ClueData 资产。
/// Create > 游戏数据 > 线索数据库，把所有 ClueData 拖进列表。
/// </summary>
[CreateAssetMenu(fileName = "ClueDatabase", menuName = "游戏数据/线索数据库")]
public class ClueDatabase : ScriptableObject
{
    // 此列表是运行时以 ID 反查资产的唯一登记来源，需包含全部可存档线索。
    // 列表项允许为空以便编辑期排查，但空项不会参与查找。
    [SerializeField] private List<ClueData> allClues = new List<ClueData>();

    // 真结局所需的前天台核心线索；补充线索可登记但不必然提高结局门槛。
    [SerializeField] private List<ClueData> trueEndingRequiredClues = new List<ClueData>();

    // 以只读接口暴露，调用方不应修改资产内部的登记顺序或内容。
    public IReadOnlyList<ClueData> AllClues => allClues;
    public IReadOnlyList<ClueData> TrueEndingRequiredClues => trueEndingRequiredClues;

    /// <summary>
    /// 按存档 ID 线性查找线索资产。
    /// 存档加载依赖精确字符串匹配；重复 ID 会返回列表中最先登记的一项。
    /// </summary>
    public ClueData FindById(string clueId)
    {
        foreach (ClueData clue in allClues)
        {
            if (clue != null && clue.ClueId == clueId)
            {
                return clue;
            }
        }

        Debug.LogWarning($"[ClueDatabase] 找不到线索: {clueId}");
        return null;
    }
}
