using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 定义 ClueJournalUI 类型
public class ClueJournalUI : MonoBehaviour
{
// 保存 instances 数据
    private static readonly List<ClueJournalUI> instances = new List<ClueJournalUI>();
    private static bool endingDisabled;

// 记录 IsEndingDisabled 状态
    public static bool IsEndingDisabled => endingDisabled;

// 定义 SetEndingDisabled 方法
    public static void SetEndingDisabled(bool disabled)
    {
// 更新当前状态
        endingDisabled = disabled;
        if (disabled)
        {
// 循环处理当前集合
            for (int i = instances.Count - 1; i >= 0; i--)
            {
// 空引用时直接退出
                if (instances[i] == null) instances.RemoveAt(i);
                else instances[i].CloseJournal();
            }
        }
    }

// 运行前初始化状态
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEndingState()
    {
// 更新当前状态
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
// 保存 detailDescription 数据
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailMeaning;
// 保存 detailIcon 数据
    [SerializeField] private Image detailIcon;

    // 动态条目只创建一次，之后按列表长度复用
    private readonly List<ClueJournalListItem> spawnedItems = new List<ClueJournalListItem>();
    private readonly List<ClueData> displayedClues = new List<ClueData>();
// 配置 displayedClueCount 数值
    private int displayedClueCount = -1;
    private ClueData selectedClue;
// 记录 displayTextDirty 状态
    private bool displayTextDirty = true;

// 定义 Start 方法
    private void Start()
    {
// 判断当前条件
        if (!instances.Contains(this)) instances.Add(this);
        CloseJournal();

// 判断当前条件
        if (GameManager.Instance != null)
        {
// 更新当前逻辑
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
        }
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 调用 Remove
        instances.Remove(this);
        if (GameManager.Instance != null)
        {
// 更新当前逻辑
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
        }
    }

// 定义 CloseJournal 方法
    public void CloseJournal()
    {
// 判断当前条件
        if (journalPanel != null) journalPanel.SetActive(false);
    }

    // 输入轮询只处理按下瞬间，避免按住按键导致面板在连续帧内反复开关
    private void Update()
    {
// 检测按键输入
        if (endingDisabled || !Input.GetKeyDown(toggleKey))
        {
// 返回当前结果
            return;
        }

        // 对话进行中不允许开日志
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
// 返回当前结果
            return;
        }

// 空引用时直接退出
        if (journalPanel == null)
        {
// 返回当前结果
            return;
        }

        // 仅在从关闭切到打开时重建；关闭不销毁按钮，减少一次无意义的 UI 更新
        bool opening = !journalPanel.activeSelf;
        journalPanel.SetActive(opening);

// 判断当前条件
        if (opening)
        {
// 执行 RebuildList
            RebuildList();
        }
    }

// 定义 RebuildList 方法
    private void RebuildList()
    {
// 保存 gm 数据
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.ClueDatabase == null || listItemPrefab == null || listContent == null)
        {
// 返回当前结果
            return;
        }

// 判断当前条件
        if (displayedClueCount == gm.CollectedClueIds.Count && displayedClues.Count == displayedClueCount)
        {
// 判断当前条件
            if (displayTextDirty)
            {
// 循环处理当前集合
                for (int i = 0; i < displayedClues.Count && i < spawnedItems.Count; i++)
                {
// 保存 clue 引用
                    ClueData clue = displayedClues[i];
                    spawnedItems[i].Bind(TextTokens.Resolve(clue.Title), () => ShowDetail(clue));
                }

// 更新当前状态
                displayTextDirty = false;
            }

// 判断当前条件
            if (selectedClue != null && displayedClues.Contains(selectedClue))
            {
// 执行 ShowDetail
                ShowDetail(selectedClue);
            }
// 检查其他条件
            else if (displayedClues.Count > 0)
            {
// 执行 ShowDetail
                ShowDetail(displayedClues[0]);
            }
// 返回当前结果
            return;
        }

// 执行 ClearDetail
        ClearDetail();
        displayedClues.Clear();
// 遍历全部元素
        foreach (string clueId in gm.CollectedClueIds)
        {
// 定义 FindById 方法
            ClueData clue = gm.ClueDatabase.FindById(clueId);
            if (clue != null)
            {
// 调用 Add
                displayedClues.Add(clue);
            }
        }

// 循环处理当前集合
        for (int i = 0; i < displayedClues.Count; i++)
        {
// 保存 item 数据
            ClueJournalListItem item;
            if (i < spawnedItems.Count)
            {
// 更新当前状态
                item = spawnedItems[i];
                item.gameObject.SetActive(true);
            }
// 处理其他分支
            else
            {
                // 创建运行时需要的对象
                item = Instantiate(listItemPrefab, listContent);
                spawnedItems.Add(item);
            }

// 保存 clue 引用
            ClueData clue = displayedClues[i];
            item.Bind(TextTokens.Resolve(clue.Title), () => ShowDetail(clue));
        }

// 循环处理当前集合
        for (int i = displayedClues.Count; i < spawnedItems.Count; i++)
        {
// 切换显示状态
            spawnedItems[i].gameObject.SetActive(false);
        }

// 更新当前状态
        displayedClueCount = gm.CollectedClueIds.Count;
        displayTextDirty = false;
// 更新当前状态
        selectedClue = displayedClues.Count > 0 ? displayedClues[0] : null;
        if (selectedClue != null)
        {
// 执行 ShowDetail
            ShowDetail(selectedClue);
        }
    }

    // Flag 变化只标记文本脏状态，打开日志时再更新可见条目
    private void HandleFlagsChanged()
    {
// 更新当前状态
        displayTextDirty = true;
    }

    // 文本先经令牌解析，使称谓等动态内容按当前剧情状态显示
    private void ShowDetail(ClueData clue)
    {
// 空引用时直接退出
        if (clue == null)
        {
// 执行 ClearDetail
            ClearDetail();
            selectedClue = null;
// 返回当前结果
            return;
        }

// 更新当前状态
        selectedClue = clue;

// 判断当前条件
        if (detailTitle != null)
        {
// 更新界面文本
            detailTitle.text = TextTokens.Resolve(clue.Title);
        }

// 判断当前条件
        if (detailDescription != null)
        {
// 更新界面文本
            detailDescription.text = TextTokens.Resolve(clue.Description);
        }

// 判断当前条件
        if (detailMeaning != null)
        {
// 更新界面文本
            detailMeaning.text = TextTokens.Resolve(clue.GetCurrentMeaning());
        }

// 判断当前条件
        if (detailIcon != null)
        {
// 更新显示图像
            detailIcon.sprite = clue.Icon;
            detailIcon.enabled = clue.Icon != null;
        }
    }

    // 空列表或重建期间清除旧详情，防止已失效的选择残留在右侧
    private void ClearDetail()
    {
// 判断当前条件
        if (detailTitle != null) detailTitle.text = string.Empty;
        if (detailDescription != null) detailDescription.text = string.Empty;
// 判断当前条件
        if (detailMeaning != null) detailMeaning.text = string.Empty;
        if (detailIcon != null) detailIcon.enabled = false;
    }
}
