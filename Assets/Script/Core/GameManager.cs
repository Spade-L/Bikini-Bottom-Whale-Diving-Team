using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 定义 GameManager 类型
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

// 记录 PendingFemaleSelection 状态
    public static bool PendingFemaleSelection;

// 配置 线索数据库（所有 ClueData 都要登记在此） 分组
    [Header("线索数据库（所有 ClueData 都要登记在此）")]
    // 结局完成度以此数据库为准，未登记的线索不会参与全收集判定
    [SerializeField] private ClueDatabase clueDatabase;

    [Header("调试")]
    // 仅控制状态变更日志，不影响事件派发或存档内容
    [SerializeField] private bool logStateChanges = true;

    // HashSet 保证 Flag 只记录一次；List 保留线索收集顺序供存档和展示使用
    private readonly HashSet<string> flags = new HashSet<string>();
    private readonly List<string> collectedClueIds = new List<string>();

    // 状态只能经公开方法变更，确保变更后的事件通知顺序一致
    public int CurrentTimePeriod { get; private set; }
    public int InvestigationCount { get; private set; }
// 保存 ClueDatabase 数据
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

    // 单例在首个场景建立；重复实例直接销毁，避免覆盖已恢复的全局状态
    private void Awake()
    {
// 判断当前条件
        if (Instance != null && Instance != this)
        {
// 清理当前对象
            Destroy(gameObject);
            return;
        }

// 更新当前状态
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 消费菜单阶段的临时选择，随后由普通 Flag 和存档机制接管
        if (PendingFemaleSelection)
        {
// 更新剧情标记
            SetFlag(TextTokens.FemaleFlag);
            PendingFemaleSelection = false;
        }
    }

// 定义 HasFlag 方法
    public bool HasFlag(string flag)
    {
// 返回当前结果
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    // 仅在首次加入成功后记录并派发事件，使监听者不会重复响应同一 Flag
    public void SetFlag(string flag)
    {
// 判断当前条件
        if (string.IsNullOrEmpty(flag) || !flags.Add(flag))
        {
// 返回当前结果
            return;
        }

        if (logStateChanges)
        {
// 输出调试信息
            Debug.Log($"[GameManager] 设置 Flag: {flag}");
        }

// 调用 Invoke
        OnFlagSet?.Invoke(flag);
        OnFlagsChanged?.Invoke();
    }

// 定义 SetFlags 方法
    public void SetFlags(IEnumerable<string> newFlags)
    {
// 空引用时直接退出
        if (newFlags == null)
        {
// 返回当前结果
            return;
        }

        bool changed = false;
// 遍历全部元素
        foreach (string flag in newFlags)
        {
// 判断当前条件
            if (string.IsNullOrEmpty(flag) || !flags.Add(flag))
            {
                continue;
            }

// 更新当前状态
            changed = true;
            OnFlagSet?.Invoke(flag);
// 判断当前条件
            if (logStateChanges)
            {
// 输出调试信息
                Debug.Log($"[GameManager] 设置 Flag: {flag}");
            }
        }

// 判断当前条件
        if (changed)
        {
// 调用 Invoke
            OnFlagsChanged?.Invoke();
        }
    }

    public void AdvanceTime(int periods = 1)
    {
// 判断当前条件
        if (periods <= 0)
        {
// 返回当前结果
            return;
        }

        CurrentTimePeriod += periods;

// 判断当前条件
        if (logStateChanges)
        {
// 输出调试信息
            Debug.Log($"[GameManager] 时间推进到时间段 {CurrentTimePeriod}");
        }

// 调用 Invoke
        OnTimeAdvanced?.Invoke(CurrentTimePeriod);
    }

    // ---------- 调查次数 ----------

    public void AddInvestigation(int amount = 1)
    {
// 判断当前条件
        if (amount <= 0)
        {
// 返回当前结果
            return;
        }

        InvestigationCount += amount;

// 判断当前条件
        if (logStateChanges)
        {
// 输出调试信息
            Debug.Log($"[GameManager] 调查次数: {InvestigationCount}");
        }

        OnInvestigationCountChanged?.Invoke(InvestigationCount);
    }

// 定义 HasClue 方法
    public bool HasClue(string clueId)
    {
// 返回当前结果
        return collectedClueIds.Contains(clueId);
    }

// 定义 HasCollectedAllPreRooftopClues 方法
    public bool HasCollectedAllPreRooftopClues()
    {
        bool hasDatabaseCheck = clueDatabase != null
// 更新当前逻辑
            && clueDatabase.TrueEndingRequiredClues != null
            && clueDatabase.TrueEndingRequiredClues.Count > 0;

// 判断当前条件
        if (hasDatabaseCheck)
        {
// 保存 missingClues 数据
            System.Collections.Generic.List<string> missingClues = null;
            foreach (ClueData clue in clueDatabase.TrueEndingRequiredClues)
            {
// 判断当前条件
                if (clue != null && !HasClue(clue.ClueId))
                {
// 空引用时直接退出
                    if (missingClues == null) missingClues = new System.Collections.Generic.List<string>();
                    missingClues.Add(clue.ClueId);
                }
            }

// 空引用时直接退出
            if (missingClues == null)
            {
// 返回当前结果
                return true;
            }

// 输出调试信息
            Debug.LogWarning($"[GameManager] 真结局缺少关键线索: {string.Join(", ", missingClues)}");
        }

// 保存 requiredSceneFlags 数据
        string[] requiredSceneFlags =
        {
// 更新当前逻辑
            "scene_cleared_home",
            "scene_cleared_school",
// 更新当前逻辑
            "scene_cleared_store",
            "scene_cleared_alley",
// 更新当前逻辑
            "scene_cleared_playground"
        };

// 保存 missingSceneFlags 数据
        System.Collections.Generic.List<string> missingSceneFlags = null;
        foreach (string flag in requiredSceneFlags)
        {
// 判断当前条件
            if (!HasFlag(flag))
            {
// 空引用时直接退出
                if (missingSceneFlags == null) missingSceneFlags = new System.Collections.Generic.List<string>();
                missingSceneFlags.Add(flag);
            }
        }

// 判断当前条件
        if (missingSceneFlags != null)
        {
// 输出调试信息
            Debug.LogWarning($"[GameManager] 真结局缺少场景通关 Flag: {string.Join(", ", missingSceneFlags)}");
            return false;
        }

// 返回当前结果
        return true;
    }

    // 空线索与重复 Id 都不产生事件，保证收集提示和相关 UI 只出现一次
    public void CollectClue(ClueData clue)
    {
// 空引用时直接退出
        if (clue == null || collectedClueIds.Contains(clue.ClueId))
        {
// 返回当前结果
            return;
        }

// 调用 Add
        collectedClueIds.Add(clue.ClueId);

// 判断当前条件
        if (logStateChanges)
        {
// 输出调试信息
            Debug.Log($"[GameManager] 收集线索: {clue.ClueId} ({clue.Title})");
        }

        OnClueCollected?.Invoke(clue);
    }

// 定义 CaptureSaveData 方法
    public SaveData CaptureSaveData()
    {
// 保存 player 引用
        PlayerMovement2D player = FindFirstObjectByType<PlayerMovement2D>();
        if (player == null)
        {
// 输出调试信息
            Debug.LogWarning("[GameManager] 当前场景找不到玩家，无法创建存档。");
            return null;
        }

// 保存 position 引用
        Vector3 position = player.transform.position;
        return new SaveData
        {
// 更新当前状态
            flags = new List<string>(flags),
            collectedClueIds = new List<string>(collectedClueIds),
// 更新当前状态
            timePeriod = CurrentTimePeriod,
            investigationCount = InvestigationCount,
// 执行场景切换
            sceneName = SceneManager.GetActiveScene().name,
            playerX = position.x,
// 更新当前状态
            playerY = position.y,
        };
    }

// 定义 ResetRuntimeState 方法
    public void ResetRuntimeState()
    {
// 调用 ReleaseAll
        GameplayInputLock.ReleaseAll();
        ClueJournalUI.SetEndingDisabled(false);
// 调用 Clear
        flags.Clear();
        collectedClueIds.Clear();
// 更新当前状态
        CurrentTimePeriod = 0;
        InvestigationCount = 0;
// 调用 Invoke
        OnFlagsChanged?.Invoke();
        OnTimeAdvanced?.Invoke(CurrentTimePeriod);
// 调用 Invoke
        OnInvestigationCountChanged?.Invoke(InvestigationCount);
    }

// 定义 RestoreSaveData 方法
    public void RestoreSaveData(SaveData data)
    {
// 空引用时直接退出
        if (data == null)
        {
// 返回当前结果
            return;
        }

// 调用 Clear
        flags.Clear();
        collectedClueIds.Clear();

// 判断当前条件
        if (data.flags != null)
        {
// 遍历全部元素
            foreach (string flag in data.flags)
            {
// 判断当前条件
                if (!string.IsNullOrEmpty(flag)) flags.Add(flag);
            }
        }

// 判断当前条件
        if (data.collectedClueIds != null)
        {
// 遍历全部元素
            foreach (string clueId in data.collectedClueIds)
            {
// 判断当前条件
                if (!string.IsNullOrEmpty(clueId) && !collectedClueIds.Contains(clueId))
                {
// 调用 Add
                    collectedClueIds.Add(clueId);
                }
            }
        }

// 更新当前状态
        CurrentTimePeriod = Mathf.Max(0, data.timePeriod);
        InvestigationCount = Mathf.Max(0, data.investigationCount);
// 调用 Invoke
        OnFlagsChanged?.Invoke();
        OnTimeAdvanced?.Invoke(CurrentTimePeriod);
// 调用 Invoke
        OnInvestigationCountChanged?.Invoke(InvestigationCount);
    }
}
