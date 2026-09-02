using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 线索日志列表项。
/// 依赖由预制体直接绑定。
/// </summary>
public class ClueJournalListItem : MonoBehaviour
{
    // 根按钮。
    [SerializeField] private Button button;
    // 标题文本。
    [SerializeField] private TMP_Text titleText;

    /// <summary>
    /// 写入标题并绑定点击事件。
    /// </summary>
    public void Bind(string title, UnityAction onClick)
    {
        // 文本引用缺失时跳过显示。
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
        }
    }
}
