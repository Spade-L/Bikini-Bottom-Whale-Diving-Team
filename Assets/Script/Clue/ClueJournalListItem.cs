using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 绑定单条线索日志的标题与点击行为
public class ClueJournalListItem : MonoBehaviour
{
    // 处理 Bind 对应逻辑
    [SerializeField] private Button button;
    // 在 ClueJournalListItem 中处理 Bind
    [SerializeField] private TMP_Text titleText;

// 在 ClueJournalListItem 中处理 Bind（ClueJournalListItem 后续步骤）
    public void Bind(string title, UnityAction onClick)
    {
        // 文本引用缺失时跳过显示
        if (titleText != null)
        {
// 更新 Bind 的界面文本
            titleText.text = title;
        }

// 检查 Bind 的前置条件
        if (button != null)
        {
// 使用 Bind 所需功能
            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
// 使用 Bind 所需功能（Bind）
                button.onClick.AddListener(onClick);
            }
        }
    }
}
