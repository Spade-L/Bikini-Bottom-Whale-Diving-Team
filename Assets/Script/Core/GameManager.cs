using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 保存剧情标记、线索进度和运行时状态
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

// 记录 GameManager 的当前状态
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
// 同步 GameManager 的相关数据
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

    // 初始化组件引用和运行状态
    private void Awake()
    {
// 检查 Awake 的前置条件
        if (Instance != null && Instance != this)
        {
// 清理当前对象
            Destroy(gameObject);
            return;
        }

// 同步 Awake 的状态
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

// 判断 HasFlag 对应条件
    public bool HasFlag(string flag)
    {
// 返回 HasFlag 的处理结果
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    // 设置 SetFlag 的目标状态
    public void SetFlag(string flag)
    {
// 检查 SetFlag 的前置条件
        if (string.IsNullOrEmpty(flag) || !flags.Add(flag))
        {
// 返回 SetFlag 的处理结果
            return;
        }

        if (logStateChanges)
        {
// 输出调试信息
            Debug.Log($"[GameManager] 设置 Flag: {flag}");
        }

// 使用 SetFlag 所需功能
        OnFlagSet?.Invoke(flag);
        OnFlagsChanged?.Invoke();
    }

// 设置 SetFlags 的目标状态
    public void SetFlags(IEnumerable<string> newFlags)
    {
// SetFlags 缺少引用时提前结束
        if (newFlags == null)
        {
// 返回 SetFlags 的处理结果
            return;
        }

        bool changed = false;
// 遍历全部元素
        foreach (string flag in newFlags)
        {
// 检查 SetFlags 的前置条件
            if (string.IsNullOrEmpty(flag) || !flags.Add(flag))
            {
                continue;
            }

// 同步 SetFlags 的内部状态
            changed = true;
            OnFlagSet?.Invoke(flag);
// 检查 SetFlags 的前置条件（SetFlags）
            if (logStateChanges)
            {
// 在 SetFlags 中继续当前处理
                Debug.Log($"[GameManager] 设置 Flag: {flag}");
            }
        }

// 检查 SetFlags 的前置条件（SetFlags）（if）
        if (changed)
        {
// 使用 SetFlags 所需功能
            OnFlagsChanged?.Invoke();
        }
    }

    public void AdvanceTime(int periods = 1)
    {
// 检查 AdvanceTime 的前置条件
        if (periods <= 0)
        {
// 返回 AdvanceTime 的处理结果
            return;
        }

        CurrentTimePeriod += periods;

// 检查 AdvanceTime 的前置条件（AdvanceTime）
        if (logStateChanges)
        {
// 在 AdvanceTime 中继续当前处理
            Debug.Log($"[GameManager] 时间推进到时间段 {CurrentTimePeriod}");
        }

// 使用 AdvanceTime 所需功能
        OnTimeAdvanced?.Invoke(CurrentTimePeriod);
    }

    // 处理 AddInvestigation 对应逻辑

    public void AddInvestigation(int amount = 1)
    {
// 检查 AddInvestigation 的前置条件
        if (amount <= 0)
        {
// 返回 AddInvestigation 的处理结果
            return;
        }

        InvestigationCount += amount;

// 检查 AddInvestigation 的前置条件（AddInvestigation）
        if (logStateChanges)
        {
// 在 AddInvestigation 中继续当前处理
            Debug.Log($"[GameManager] 调查次数: {InvestigationCount}");
        }

        OnInvestigationCountChanged?.Invoke(InvestigationCount);
    }

// 判断 HasClue 对应条件
    public bool HasClue(string clueId)
    {
// 返回 HasClue 的处理结果
        return collectedClueIds.Contains(clueId);
    }

// 判断 HasCollectedAllPreRooftopClues 对应条件
    public bool HasCollectedAllPreRooftopClues()
    {
        bool hasDatabaseCheck = clueDatabase != null
// 推进 HasCollectedAllPreRooftopClues 的当前步骤
            && clueDatabase.TrueEndingRequiredClues != null
            && clueDatabase.TrueEndingRequiredClues.Count > 0;

// 检查 HasCollectedAllPreRooftopClues 的前置条件
        if (hasDatabaseCheck)
        {
// 同步 HasCollectedAllPreRooftopClues 的相关数据
            System.Collections.Generic.List<string> missingClues = null;
            foreach (ClueData clue in clueDatabase.TrueEndingRequiredClues)
            {
// 检查 HasCollectedAllPreRooftopClues 的前置条件（HasCollectedAllPreRooftopClues）
                if (clue != null && !HasClue(clue.ClueId))
                {
// 缺少必要引用时退出 HasCollectedAllPreRooftopClues
                    if (missingClues == null) missingClues = new System.Collections.Generic.List<string>();
                    missingClues.Add(clue.ClueId);
                }
            }

// 缺少必要引用时退出 HasCollectedAllPreRooftopClues（HasCollectedAllPreRooftopClues）
            if (missingClues == null)
            {
// 返回 HasCollectedAllPreRooftopClues 的处理结果
                return true;
            }

// 在 HasCollectedAllPreRooftopClues 中继续当前处理
            Debug.LogWarning($"[GameManager] 真结局缺少关键线索: {string.Join(", ", missingClues)}");
        }

// 同步 HasCollectedAllPreRooftopClues 的相关数据（HasCollectedAllPreRooftopClues 后续步骤）
        string[] requiredSceneFlags =
        {
// 在 HasCollectedAllPreRooftopClues 中处理 推进 HasCollectedAllPreRooftopClues 的当前步骤
            "scene_cleared_home",
            "scene_cleared_school",
// 推进 HasCollectedAllPreRooftopClues 的当前步骤（HasCollectedAllPreRooftopClues）
            "scene_cleared_store",
            "scene_cleared_alley",
// 推进 HasCollectedAllPreRooftopClues 的当前步骤（HasCollectedAllPreRooftopClues）（scene_cleared_playground）
            "scene_cleared_playground"
        };

// 同步 HasCollectedAllPreRooftopClues 的相关数据（HasCollectedAllPreRooftopClues 后续步骤）（235）
        System.Collections.Generic.List<string> missingSceneFlags = null;
        foreach (string flag in requiredSceneFlags)
        {
// 检查 HasCollectedAllPreRooftopClues 的前置条件（HasCollectedAllPreRooftopClues）（if）
            if (!HasFlag(flag))
            {
// 缺少必要引用时退出 HasCollectedAllPreRooftopClues（HasCollectedAllPreRooftopClues）（if）
                if (missingSceneFlags == null) missingSceneFlags = new System.Collections.Generic.List<string>();
                missingSceneFlags.Add(flag);
            }
        }

// 在 HasCollectedAllPreRooftopClues 中继续当前处理（HasCollectedAllPreRooftopClues 后续步骤）
        if (missingSceneFlags != null)
        {
// 在 HasCollectedAllPreRooftopClues 中继续当前处理（HasCollectedAllPreRooftopClues 后续步骤）（251）
            Debug.LogWarning($"[GameManager] 真结局缺少场景通关 Flag: {string.Join(", ", missingSceneFlags)}");
            return false;
        }

// 返回 HasCollectedAllPreRooftopClues 的处理结果（HasCollectedAllPreRooftopClues）
        return true;
    }

    // 处理 CollectClue 对应逻辑
    public void CollectClue(ClueData clue)
    {
// 缺少必要引用时退出 CollectClue
        if (clue == null || collectedClueIds.Contains(clue.ClueId))
        {
// 返回 CollectClue 的处理结果
            return;
        }

// 使用 CollectClue 所需功能
        collectedClueIds.Add(clue.ClueId);

// 检查 CollectClue 的前置条件
        if (logStateChanges)
        {
// 在 CollectClue 中继续当前处理
            Debug.Log($"[GameManager] 收集线索: {clue.ClueId} ({clue.Title})");
        }

        OnClueCollected?.Invoke(clue);
    }

// 处理 CaptureSaveData 对应逻辑
    public SaveData CaptureSaveData()
    {
// 保存 player 引用
        PlayerMovement2D player = FindFirstObjectByType<PlayerMovement2D>();
        if (player == null)
        {
// 在 CaptureSaveData 中继续当前处理
            Debug.LogWarning("[GameManager] 当前场景找不到玩家，无法创建存档。");
            return null;
        }

// 保存 position 引用
        Vector3 position = player.transform.position;
        return new SaveData
        {
// 同步 CaptureSaveData 的内部状态
            flags = new List<string>(flags),
            collectedClueIds = new List<string>(collectedClueIds),
// 同步 CaptureSaveData 的内部状态（CaptureSaveData）
            timePeriod = CurrentTimePeriod,
            investigationCount = InvestigationCount,
// 执行场景切换
            sceneName = SceneManager.GetActiveScene().name,
            playerX = position.x,
// 同步 CaptureSaveData 的内部状态（CaptureSaveData）（playerY）
            playerY = position.y,
        };
    }

// 处理 ResetRuntimeState 对应逻辑
    public void ResetRuntimeState()
    {
// 使用 ResetRuntimeState 所需功能
        GameplayInputLock.ReleaseAll();
        ClueJournalUI.SetEndingDisabled(false);
// 使用 ResetRuntimeState 所需功能（ResetRuntimeState）
        flags.Clear();
        collectedClueIds.Clear();
// 同步 ResetRuntimeState 的内部状态
        CurrentTimePeriod = 0;
        InvestigationCount = 0;
// 在 ResetRuntimeState 中处理 Invoke
        OnFlagsChanged?.Invoke();
        OnTimeAdvanced?.Invoke(CurrentTimePeriod);
// 在 ResetRuntimeState 中处理 Invoke（ResetRuntimeState 后续步骤）
        OnInvestigationCountChanged?.Invoke(InvestigationCount);
    }

// 恢复 RestoreSaveData 对应状态
    public void RestoreSaveData(SaveData data)
    {
// 缺少必要引用时退出 RestoreSaveData
        if (data == null)
        {
// 返回 RestoreSaveData 的处理结果
            return;
        }

// 使用 RestoreSaveData 所需功能
        flags.Clear();
        collectedClueIds.Clear();

// 检查 RestoreSaveData 的前置条件
        if (data.flags != null)
        {
// 在 RestoreSaveData 中继续当前处理
            foreach (string flag in data.flags)
            {
// 检查 RestoreSaveData 的前置条件（RestoreSaveData）
                if (!string.IsNullOrEmpty(flag)) flags.Add(flag);
            }
        }

// 检查 RestoreSaveData 的前置条件（RestoreSaveData）（if）
        if (data.collectedClueIds != null)
        {
// 在 RestoreSaveData 中继续当前处理（RestoreSaveData 后续步骤）
            foreach (string clueId in data.collectedClueIds)
            {
// 在 RestoreSaveData 中继续当前处理（RestoreSaveData 后续步骤）（363）
                if (!string.IsNullOrEmpty(clueId) && !collectedClueIds.Contains(clueId))
                {
// 使用 RestoreSaveData 所需功能（RestoreSaveData）
                    collectedClueIds.Add(clueId);
                }
            }
        }

// 同步 RestoreSaveData 的内部状态
        CurrentTimePeriod = Mathf.Max(0, data.timePeriod);
        InvestigationCount = Mathf.Max(0, data.investigationCount);
// 在 RestoreSaveData 中处理 Invoke
        OnFlagsChanged?.Invoke();
        OnTimeAdvanced?.Invoke(CurrentTimePeriod);
// 在 RestoreSaveData 中处理 Invoke（RestoreSaveData 后续步骤）
        OnInvestigationCountChanged?.Invoke(InvestigationCount);
    }
}
