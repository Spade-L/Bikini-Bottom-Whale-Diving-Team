using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClueDatabase", menuName = "游戏数据/线索数据库")]
public class ClueDatabase : ScriptableObject
{
    [SerializeField] private List<ClueData> allClues = new List<ClueData>();

    // 真结局所需的前天台核心线索；补充线索可登记但不必然提高结局门槛
    [SerializeField] private List<ClueData> trueEndingRequiredClues = new List<ClueData>();

    // 以只读接口暴露，调用方不应修改资产内部的登记顺序或内容
    public IReadOnlyList<ClueData> AllClues => allClues;
    public IReadOnlyList<ClueData> TrueEndingRequiredClues => trueEndingRequiredClues;

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
