using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClueJournalUI : MonoBehaviour
{
    private static readonly List<ClueJournalUI> instances = new List<ClueJournalUI>();
    private static bool endingDisabled;

    public static bool IsEndingDisabled => endingDisabled;

    public static void SetEndingDisabled(bool disabled)
    {
        endingDisabled = disabled;
        if (disabled)
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                if (instances[i] == null) instances.RemoveAt(i);
                else instances[i].CloseJournal();
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEndingState()
    {
        endingDisabled = false;
        instances.Clear();
    }
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("列表")]
    [SerializeField] private Transform listContent;
    // 子项依赖由预制体序列化绑定
    [SerializeField] private ClueJournalListItem listItemPrefab;

    [Header("详情")]
    [SerializeField] private TMP_Text detailTitle;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailMeaning;
    [SerializeField] private Image detailIcon;

    // 动态条目只创建一次，之后按列表长度复用
    private readonly List<ClueJournalListItem> spawnedItems = new List<ClueJournalListItem>();
    private readonly List<ClueData> displayedClues = new List<ClueData>();
    private int displayedClueCount = -1;
    private ClueData selectedClue;
    private bool displayTextDirty = true;

    private void Start()
    {
        if (!instances.Contains(this)) instances.Add(this);
        CloseJournal();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
        }
    }

    private void OnDestroy()
    {
        instances.Remove(this);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
        }
    }

    public void CloseJournal()
    {
        if (journalPanel != null) journalPanel.SetActive(false);
    }

    // 输入轮询只处理按下瞬间，避免按住按键导致面板在连续帧内反复开关
    private void Update()
    {
        if (endingDisabled || !Input.GetKeyDown(toggleKey))
        {
            return;
        }

        // 对话进行中不允许开日志
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
            return;
        }

        if (journalPanel == null)
        {
            return;
        }

        // 仅在从关闭切到打开时重建；关闭不销毁按钮，减少一次无意义的 UI 更新
        bool opening = !journalPanel.activeSelf;
        journalPanel.SetActive(opening);

        if (opening)
        {
            RebuildList();
        }
    }

    private void RebuildList()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.ClueDatabase == null || listItemPrefab == null || listContent == null)
        {
            return;
        }

        if (displayedClueCount == gm.CollectedClueIds.Count && displayedClues.Count == displayedClueCount)
        {
            if (displayTextDirty)
            {
                for (int i = 0; i < displayedClues.Count && i < spawnedItems.Count; i++)
                {
                    ClueData clue = displayedClues[i];
                    spawnedItems[i].Bind(TextTokens.Resolve(clue.Title), () => ShowDetail(clue));
                }

                displayTextDirty = false;
            }

            if (selectedClue != null && displayedClues.Contains(selectedClue))
            {
                ShowDetail(selectedClue);
            }
            else if (displayedClues.Count > 0)
            {
                ShowDetail(displayedClues[0]);
            }
            return;
        }

        ClearDetail();
        displayedClues.Clear();
        foreach (string clueId in gm.CollectedClueIds)
        {
            ClueData clue = gm.ClueDatabase.FindById(clueId);
            if (clue != null)
            {
                displayedClues.Add(clue);
            }
        }

        for (int i = 0; i < displayedClues.Count; i++)
        {
            ClueJournalListItem item;
            if (i < spawnedItems.Count)
            {
                item = spawnedItems[i];
                item.gameObject.SetActive(true);
            }
            else
            {
                // 创建运行时需要的对象
                item = Instantiate(listItemPrefab, listContent);
                spawnedItems.Add(item);
            }

            ClueData clue = displayedClues[i];
            item.Bind(TextTokens.Resolve(clue.Title), () => ShowDetail(clue));
        }

        for (int i = displayedClues.Count; i < spawnedItems.Count; i++)
        {
            spawnedItems[i].gameObject.SetActive(false);
        }

        displayedClueCount = gm.CollectedClueIds.Count;
        displayTextDirty = false;
        selectedClue = displayedClues.Count > 0 ? displayedClues[0] : null;
        if (selectedClue != null)
        {
            ShowDetail(selectedClue);
        }
    }

    // Flag 变化只标记文本脏状态，打开日志时再更新可见条目
    private void HandleFlagsChanged()
    {
        displayTextDirty = true;
    }

    // 文本先经令牌解析，使称谓等动态内容按当前剧情状态显示
    private void ShowDetail(ClueData clue)
    {
        if (clue == null)
        {
            ClearDetail();
            selectedClue = null;
            return;
        }

        selectedClue = clue;

        if (detailTitle != null)
        {
            detailTitle.text = TextTokens.Resolve(clue.Title);
        }

        if (detailDescription != null)
        {
            detailDescription.text = TextTokens.Resolve(clue.Description);
        }

        if (detailMeaning != null)
        {
            detailMeaning.text = TextTokens.Resolve(clue.GetCurrentMeaning());
        }

        if (detailIcon != null)
        {
            detailIcon.sprite = clue.Icon;
            detailIcon.enabled = clue.Icon != null;
        }
    }

    // 空列表或重建期间清除旧详情，防止已失效的选择残留在右侧
    private void ClearDetail()
    {
        if (detailTitle != null) detailTitle.text = string.Empty;
        if (detailDescription != null) detailDescription.text = string.Empty;
        if (detailMeaning != null) detailMeaning.text = string.Empty;
        if (detailIcon != null) detailIcon.enabled = false;
    }
}
