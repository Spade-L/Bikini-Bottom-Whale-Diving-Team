using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 定义 ClueJournalListItem 类型
public class ClueJournalListItem : MonoBehaviour
{
    // 根按钮
    [SerializeField] private Button button;
    // 标题文本
    [SerializeField] private TMP_Text titleText;

// 定义 Bind 方法
    public void Bind(string title, UnityAction onClick)
    {
        // 文本引用缺失时跳过显示
        if (titleText != null)
        {
// 更新界面文本
            titleText.text = title;
        }

// 判断当前条件
        if (button != null)
        {
// 调用 RemoveAllListeners
            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
// 调用 AddListener
                button.onClick.AddListener(onClick);
            }
        }
    }
}
