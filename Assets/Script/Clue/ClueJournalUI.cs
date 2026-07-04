using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 线索日志界面（按 Tab 开关）。左侧列表，右侧详情。
/// 结局揭晓（truth_revealed flag）后，详情自动切换为线索的真实含义。
/// </summary>
public class ClueJournalUI : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("列表")]
    [SerializeField] private Transform listContent;
    [SerializeField] private Button listItemPrefab;

    [Header("详情")]
    [SerializeField] private TMP_Text detailTitle;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailMeaning;
    [SerializeField] private Image detailIcon;

    private readonly List<Button> spawnedItems = new List<Button>();

    private void Start()
    {
        if (journalPanel != null)
        {
            journalPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
        {
            return;
        }

        // 对话进行中不允许开日志
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
            return;
        }

        bool opening = journalPanel != null && !journalPanel.activeSelf;
        journalPanel.SetActive(opening);

        if (opening)
        {
            RebuildList();
        }
    }

    private void RebuildList()
    {
        foreach (Button item in spawnedItems)
        {
            Destroy(item.gameObject);
        }
        spawnedItems.Clear();
        ClearDetail();

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.ClueDatabase == null || listItemPrefab == null || listContent == null)
        {
            return;
        }

        foreach (string clueId in gm.CollectedClueIds)
        {
            ClueData clue = gm.ClueDatabase.FindById(clueId);
            if (clue == null)
            {
                continue;
            }

            Button item = Instantiate(listItemPrefab, listContent);
            TMP_Text label = item.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = TextTokens.Resolve(clue.Title);
            }

            ClueData captured = clue;
            item.onClick.AddListener(() => ShowDetail(captured));
            spawnedItems.Add(item);
        }

        // 默认选中第一条
        if (gm.CollectedClueIds.Count > 0)
        {
            ClueData first = gm.ClueDatabase.FindById(gm.CollectedClueIds[0]);
            if (first != null)
            {
                ShowDetail(first);
            }
        }
    }

    private void ShowDetail(ClueData clue)
    {
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

    private void ClearDetail()
    {
        if (detailTitle != null) detailTitle.text = string.Empty;
        if (detailDescription != null) detailDescription.text = string.Empty;
        if (detailMeaning != null) detailMeaning.text = string.Empty;
        if (detailIcon != null) detailIcon.enabled = false;
    }
}
