using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局游戏状态：剧情 Flag、时间段、已收集线索。
/// 时间采用离散“时间段”推进（由剧情事件触发 AdvanceTime），而非真实计时。
/// </summary>
// 全局进度
// 剧情 Flag
// 离散时间
// 调查计数
// 已收集线索
// 线索完成度
// 不计真实时间
// 不处理演出
// 事件通知
// Flag 去重
// 线索去重
// 忽略空 Flag
// 忽略空线索
// 整体读档
// 不重放 Flag
// 刷新派生状态
// 暂存性别
// 单例保护
// 跨场景保留
// 存档副本
// Id 判重
// 排除天台线索
// 调试日志
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// 主菜单在 GameManager 诞生前记录的性别选择（true = 女性/姐姐线）。
    /// GameManager Awake 时转正为 gender_female flag，之后随存档走。
    /// </summary>
    public static bool PendingFemaleSelection;

    [Header("线索数据库（所有 ClueData 都要登记在此）")]
    // 结局完成度以此数据库为准，未登记的线索不会参与全收集判定。
    [SerializeField] private ClueDatabase clueDatabase;

    [Header("调试")]
    // 仅控制状态变更日志，不影响事件派发或存档内容。
    [SerializeField] private bool logStateChanges = true;

    // HashSet 保证 Flag 只记录一次；List 保留线索收集顺序供存档和展示使用。
    private readonly HashSet<string> flags = new HashSet<string>();
    private readonly List<string> collectedClueIds = new List<string>();

    // 状态只能经公开方法变更，确保变更后的事件通知顺序一致。
    public int CurrentTimePeriod { get; private set; }
    public int InvestigationCount { get; private set; }
    public ClueDatabase ClueDatabase => clueDatabase;
    public IReadOnlyList<string> CollectedClueIds => collectedClueIds;

    /// <summary>Flag 被设置时触发（参数：flag 名）。</summary>
    public event Action<string> OnFlagSet;
    /// <summary>一批 Flag 设置完成后触发一次。</summary>
    public event Action OnFlagsChanged;
    /// <summary>时间段推进时触发（参数：新的时间段）。</summary>
    public event Action<int> OnTimeAdvanced;
    /// <summary>收集到线索时触发。</summary>
    public event Action<ClueData> OnClueCollected;
    /// <summary>调查次数增加时触发（参数：新的总次数）。</summary>
    public event Action<int> OnInvestigationCountChanged;

    // 单例在首个场景建立；重复实例直接销毁，避免覆盖已恢复的全局状态。
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 消费菜单阶段的临时选择，随后由普通 Flag 和存档机制接管。
        if (PendingFemaleSelection)
        {
            SetFlag(TextTokens.FemaleFlag);
            PendingFemaleSelection = false;
        }
    }

    // ---------- Flag ----------

    // 空名称视为不存在，防止配置中的空数组项意外通过条件判断。
    public bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    // 仅在首次加入成功后记录并派发事件，使监听者不会重复响应同一 Flag。
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
        OnFlagsChanged?.Invoke();
    }

    /// <summary>
    /// 一次设置多个 Flag，只在所有状态写入后通知一次批量变化。
    /// 单个 Flag 事件仍保留，兼容依赖具体 Flag 的旧订阅者。
    /// </summary>
    public void SetFlags(IEnumerable<string> newFlags)
    {
        if (newFlags == null)
        {
            return;
        }

        bool changed = false;
        foreach (string flag in newFlags)
        {
            if (string.IsNullOrEmpty(flag) || !flags.Add(flag))
            {
                continue;
            }

            changed = true;
            OnFlagSet?.Invoke(flag);
            if (logStateChanges)
            {
                Debug.Log($"[GameManager] 设置 Flag: {flag}");
            }
        }

        if (changed)
        {
            OnFlagsChanged?.Invoke();
        }
    }

    // ---------- 时间 ----------

    // 只接受正向离散推进；数值写入完成后再通知依赖时间段的对象。
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

    // 线索以稳定 Id 比较，不依赖可本地化或会变更的显示标题。
    public bool HasClue(string clueId)
    {
        return collectedClueIds.Contains(clueId);
    }

    /// <summary>
    /// 前五关的核心线索是否都已收集。补充线索不因登记到数据库而自动成为结局门槛。
    /// </summary>
    public bool HasCollectedAllPreRooftopClues()
    {
        if (clueDatabase == null || clueDatabase.TrueEndingRequiredClues == null)
        {
            return false;
        }

        foreach (ClueData clue in clueDatabase.TrueEndingRequiredClues)
        {
            if (clue != null && !HasClue(clue.ClueId))
            {
                return false;
            }
        }

        return clueDatabase.TrueEndingRequiredClues.Count > 0;
    }

    // 空线索与重复 Id 都不产生事件，保证收集提示和相关 UI 只出现一次。
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

    // 复制集合而非暴露内部引用，保证写盘期间不会受后续状态变更影响。
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

    // 先替换全部状态，再依次广播时间和调查次数，以便订阅者刷新派生显示。
    // Flag 和线索不逐项派发事件，避免读档时重复触发剧情。
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
