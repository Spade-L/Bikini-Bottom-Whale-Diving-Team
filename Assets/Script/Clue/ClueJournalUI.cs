using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 显示线索日志、详情和结局禁用状态
public class ClueJournalUI : MonoBehaviour
{
// 同步 ClueJournalUI 的相关数据
    private static readonly List<ClueJournalUI> instances = new List<ClueJournalUI>();
    private static bool endingDisabled;

// 记录 ClueJournalUI 的当前状态
    public static bool IsEndingDisabled => endingDisabled;

// 设置 SetEndingDisabled 的目标状态
    public static void SetEndingDisabled(bool disabled)
    {
// 同步 SetEndingDisabled 的状态
        endingDisabled = disabled;
        if (disabled)
        {
// 循环处理当前集合
            for (int i = instances.Count - 1; i >= 0; i--)
            {
// SetEndingDisabled 缺少引用时提前结束
                if (instances[i] == null) instances.RemoveAt(i);
                else instances[i].CloseJournal();
            }
        }
    }

// 处理 ResetEndingState 对应逻辑
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEndingState()
    {
// 同步 ResetEndingState 的内部状态
        endingDisabled = false;
        instances.Clear();
    }
// 保存 journalPanel 引用
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

// 配置 列表 分组
    [Header("列表")]
    [SerializeField] private Transform listContent;
    // 子项依赖由预制体序列化绑定
    [SerializeField] private ClueJournalListItem listItemPrefab;

// 配置 详情 分组
    [Header("详情")]
    [SerializeField] private TMP_Text detailTitle;
// 同步 ResetEndingState 的相关数据
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailMeaning;
// 同步 ResetEndingState 的相关数据（ResetEndingState 后续步骤）
    [SerializeField] private Image detailIcon;

    // 动态条目只创建一次，之后按列表长度复用
    private readonly List<ClueJournalListItem> spawnedItems = new List<ClueJournalListItem>();
    private readonly List<ClueData> displayedClues = new List<ClueData>();
// 设置 ResetEndingState 的配置数值
    private int displayedClueCount = -1;
    private ClueData selectedClue;
// 记录 ResetEndingState 的当前状态
    private bool displayTextDirty = true;

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 检查 Start 的前置条件
        if (!instances.Contains(this)) instances.Add(this);
        CloseJournal();

// 在 Start 中处理 检查 Start 的前置条件
        if (GameManager.Instance != null)
        {
// 推进 Start 的当前步骤
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
        }
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 使用 OnDestroy 所需功能
        instances.Remove(this);
        if (GameManager.Instance != null)
        {
// 推进 OnDestroy 的当前步骤
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
        }
    }

// 处理 CloseJournal 对应逻辑
    public void CloseJournal()
    {
// 检查 CloseJournal 的前置条件
        if (journalPanel != null) journalPanel.SetActive(false);
    }

    // 每帧检查输入与状态变化
    private void Update()
    {
// 检测按键输入
        if (endingDisabled || !Input.GetKeyDown(toggleKey))
        {
// 返回 Update 的处理结果
            return;
        }

        // 对话进行中不允许开日志
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
// 在 Update 中处理 返回 Update 的处理结果
            return;
        }

// 缺少必要引用时退出 Update
        if (journalPanel == null)
        {
// 返回 Update 的处理结果（Update）
            return;
        }

        // 仅在从关闭切到打开时重建；关闭不销毁按钮，减少一次无意义的 UI 更新
        bool opening = !journalPanel.activeSelf;
        journalPanel.SetActive(opening);

// 检查 Update 的前置条件
        if (opening)
        {
// 推进 Update 中的必要步骤
            RebuildList();
        }
    }

// 处理 RebuildList 对应逻辑
    private void RebuildList()
    {
// 同步 RebuildList 的相关数据
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.ClueDatabase == null || listItemPrefab == null || listContent == null)
        {
// 返回 RebuildList 的处理结果
            return;
        }

// 检查 RebuildList 的前置条件
        if (displayedClueCount == gm.CollectedClueIds.Count && displayedClues.Count == displayedClueCount)
        {
// 检查 RebuildList 的前置条件（RebuildList）
            if (displayTextDirty)
            {
// 在 RebuildList 中继续当前处理
                for (int i = 0; i < displayedClues.Count && i < spawnedItems.Count; i++)
                {
// 保存 clue 引用
                    ClueData clue = displayedClues[i];
                    spawnedItems[i].Bind(TextTokens.Resolve(clue.Title), () => ShowDetail(clue));
                }

// 同步 RebuildList 的内部状态
                displayTextDirty = false;
            }

// 检查 RebuildList 的前置条件（RebuildList）（if）
            if (selectedClue != null && displayedClues.Contains(selectedClue))
            {
// 推进 RebuildList 中的必要步骤
                ShowDetail(selectedClue);
            }
// 检查其他条件
            else if (displayedClues.Count > 0)
            {
// 推进 RebuildList 中的必要步骤（RebuildList）
                ShowDetail(displayedClues[0]);
            }
// 返回 RebuildList 的处理结果（RebuildList）
            return;
        }

// 在 RebuildList 中处理 ClearDetail
        ClearDetail();
        displayedClues.Clear();
// 遍历全部元素
        foreach (string clueId in gm.CollectedClueIds)
        {
// 缓存 RebuildList 所需引用
            ClueData clue = gm.ClueDatabase.FindById(clueId);
            if (clue != null)
            {
// 使用 RebuildList 所需功能
                displayedClues.Add(clue);
            }
        }

// 在 RebuildList 中继续当前处理（RebuildList 后续步骤）
        for (int i = 0; i < displayedClues.Count; i++)
        {
// 同步 RebuildList 的相关数据（RebuildList 后续步骤）
            ClueJournalListItem item;
            if (i < spawnedItems.Count)
            {
// 同步 RebuildList 的内部状态（RebuildList）
                item = spawnedItems[i];
                item.gameObject.SetActive(true);
            }
// 处理 RebuildList 的备用分支
            else
            {
                // 创建运行时需要的对象
                item = Instantiate(listItemPrefab, listContent);
                spawnedItems.Add(item);
            }

// 在 RebuildList 中继续当前处理（RebuildList 后续步骤）（217）
            ClueData clue = displayedClues[i];
            item.Bind(TextTokens.Resolve(clue.Title), () => ShowDetail(clue));
        }

// 在 RebuildList 中继续当前处理（RebuildList 后续步骤）（222）
        for (int i = displayedClues.Count; i < spawnedItems.Count; i++)
        {
// 切换 RebuildList 的显示状态
            spawnedItems[i].gameObject.SetActive(false);
        }

// 同步 RebuildList 的内部状态（RebuildList）（displayedClueCount）
        displayedClueCount = gm.CollectedClueIds.Count;
        displayTextDirty = false;
// 同步 RebuildList 的内部状态（RebuildList）（selectedClue）
        selectedClue = displayedClues.Count > 0 ? displayedClues[0] : null;
        if (selectedClue != null)
        {
// 在 RebuildList 中处理 ShowDetail
            ShowDetail(selectedClue);
        }
    }

    // 剧情标记变化后同步当前界面与交互
    private void HandleFlagsChanged()
    {
// 同步 HandleFlagsChanged 的内部状态
        displayTextDirty = true;
    }

    // 显示 ShowDetail 对应界面
    private void ShowDetail(ClueData clue)
    {
// 缺少必要引用时退出 ShowDetail
        if (clue == null)
        {
// 推进 ShowDetail 中的必要步骤
            ClearDetail();
            selectedClue = null;
// 返回 ShowDetail 的处理结果
            return;
        }

// 同步 ShowDetail 的内部状态
        selectedClue = clue;

// 检查 ShowDetail 的前置条件
        if (detailTitle != null)
        {
// 更新 ShowDetail 的界面文本
            detailTitle.text = TextTokens.Resolve(clue.Title);
        }

// 检查 ShowDetail 的前置条件（ShowDetail）
        if (detailDescription != null)
        {
// 更新 ShowDetail 的界面文本（ShowDetail 后续步骤）
            detailDescription.text = TextTokens.Resolve(clue.Description);
        }

// 检查 ShowDetail 的前置条件（ShowDetail）（if）
        if (detailMeaning != null)
        {
// 更新 ShowDetail 的界面文本（ShowDetail 后续步骤）（281）
            detailMeaning.text = TextTokens.Resolve(clue.GetCurrentMeaning());
        }

// 在 ShowDetail 中继续当前处理
        if (detailIcon != null)
        {
// 更新显示图像
            detailIcon.sprite = clue.Icon;
            detailIcon.enabled = clue.Icon != null;
        }
    }

    // 处理 ClearDetail 对应逻辑
    private void ClearDetail()
    {
// 检查 ClearDetail 的前置条件
        if (detailTitle != null) detailTitle.text = string.Empty;
        if (detailDescription != null) detailDescription.text = string.Empty;
// 检查 ClearDetail 的前置条件（ClearDetail）
        if (detailMeaning != null) detailMeaning.text = string.Empty;
        if (detailIcon != null) detailIcon.enabled = false;
    }
}
