using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ClueJournalListItem : MonoBehaviour
{
    // 根按钮
    [SerializeField] private Button button;
    // 标题文本
    [SerializeField] private TMP_Text titleText;

    public void Bind(string title, UnityAction onClick)
    {
        // 文本引用缺失时跳过显示
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
