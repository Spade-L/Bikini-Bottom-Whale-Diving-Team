using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局游戏状态：剧情 Flag、时间段、已收集线索。
/// 时间采用离散“时间段”推进（由剧情事件触发 AdvanceTime），而非真实计时。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// 主菜单在 GameManager 诞生前记录的性别选择（true = 女性/姐姐线）。
    /// GameManager Awake 时转正为 gender_female flag，之后随存档走。
    /// </summary>
    public static bool PendingFemaleSelection;

    [Header("线索数据库（所有 ClueData 都要登记在此）")]
    [SerializeField] private ClueDatabase clueDatabase;

    [Header("调试")]
    [SerializeField] private bool logStateChanges = true;

    private readonly HashSet<string> flags = new HashSet<string>();
    private readonly List<string> collectedClueIds = new List<string>();

    public int CurrentTimePeriod { get; private set; }
    public int InvestigationCount { get; private set; }
    public ClueDatabase ClueDatabase => clueDatabase;
    public IReadOnlyList<string> CollectedClueIds => collectedClueIds;

    /// <summary>Flag 被设置时触发（参数：flag 名）。</summary>
    public event Action<string> OnFlagSet;
    /// <summary>时间段推进时触发（参数：新的时间段）。</summary>
    public event Action<int> OnTimeAdvanced;
    /// <summary>收集到线索时触发。</summary>
    public event Action<ClueData> OnClueCollected;
    /// <summary>调查次数增加时触发（参数：新的总次数）。</summary>
    public event Action<int> OnInvestigationCountChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (PendingFemaleSelection)
        {
            SetFlag(TextTokens.FemaleFlag);
            PendingFemaleSelection = false;
        }
    }

    // ---------- Flag ----------

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    public void SetFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag) || !flags.Add(flag))
        {
            return;
        }

        if (logStateChanges)
        {
            Debug.Log($"[GameManager] 设置 Flag: {flag}");
        }

        OnFlagSet?.Invoke(flag);
    }

    // ---------- 时间 ----------

    public void AdvanceTime(int periods = 1)
    {
        if (periods <= 0)
        {
            return;
        }

        CurrentTimePeriod += periods;

        if (logStateChanges)
        {
            Debug.Log($"[GameManager] 时间推进到时间段 {CurrentTimePeriod}");
        }

        OnTimeAdvanced?.Invoke(CurrentTimePeriod);
    }

    // ---------- 调查次数 ----------

    /// <summary>
    /// 调查物品 +1、触发独白 +1（收集线索不额外计数）。
    /// 阈值事件由 InvestigationDirector 监听 OnInvestigationCountChanged 处理。
    /// </summary>
    public void AddInvestigation(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        InvestigationCount += amount;

        if (logStateChanges)
        {
            Debug.Log($"[GameManager] 调查次数: {InvestigationCount}");
        }

        OnInvestigationCountChanged?.Invoke(InvestigationCount);
    }

    // ---------- 线索 ----------

    public bool HasClue(string clueId)
    {
        return collectedClueIds.Contains(clueId);
    }

    public void CollectClue(ClueData clue)
    {
        if (clue == null || collectedClueIds.Contains(clue.ClueId))
        {
            return;
        }

        collectedClueIds.Add(clue.ClueId);

        if (logStateChanges)
        {
            Debug.Log($"[GameManager] 收集线索: {clue.ClueId} ({clue.Title})");
        }

        OnClueCollected?.Invoke(clue);
    }

    // ---------- 存档 ----------

    public SaveData CaptureSaveData()
    {
        return new SaveData
        {
            flags = new List<string>(flags),
            collectedClueIds = new List<string>(collectedClueIds),
            timePeriod = CurrentTimePeriod,
            investigationCount = InvestigationCount,
        };
    }

    public void RestoreSaveData(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        flags.Clear();
        collectedClueIds.Clear();

        if (data.flags != null)
        {
            foreach (string flag in data.flags)
            {
                flags.Add(flag);
            }
        }

        if (data.collectedClueIds != null)
        {
            collectedClueIds.AddRange(data.collectedClueIds);
        }

        CurrentTimePeriod = data.timePeriod;
        InvestigationCount = data.investigationCount;
        OnTimeAdvanced?.Invoke(CurrentTimePeriod);
        OnInvestigationCountChanged?.Invoke(InvestigationCount);
    }
}
