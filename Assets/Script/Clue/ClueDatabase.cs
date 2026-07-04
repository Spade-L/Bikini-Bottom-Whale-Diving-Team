using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有线索的登记表，用于从存档中的 ClueId 找回 ClueData 资产。
/// Create > 游戏数据 > 线索数据库，把所有 ClueData 拖进列表。
/// </summary>
[CreateAssetMenu(fileName = "ClueDatabase", menuName = "游戏数据/线索数据库")]
public class ClueDatabase : ScriptableObject
{
    [SerializeField] private List<ClueData> allClues = new List<ClueData>();

    public IReadOnlyList<ClueData> AllClues => allClues;

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
