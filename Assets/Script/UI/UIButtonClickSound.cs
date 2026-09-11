using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
// 统一处理按钮点击音效
public class UIButtonClickSound : MonoBehaviour
{
    // 初始化组件引用和运行状态
    [Header("点击音效")]
// 在 UIButtonClickSound 中处理 Awake
    [SerializeField] private AudioClip clickClip;
    // 在 UIButtonClickSound 中处理 Awake（UIButtonClickSound 后续步骤）
    [Range(0f, 1f)]
    // 在 UIButtonClickSound 中处理 Awake（UIButtonClickSound 后续步骤）（后续处理 2）
    [SerializeField] private float volume = 1f;

    // 在 UIButtonClickSound 中处理 Awake（UIButtonClickSound 后续步骤）（后续处理 3）
    private void Awake()
    {
        // RequireComponent 会在添加脚本时补齐 Button；此处无需空值分支
        GetComponent<Button>().onClick.AddListener(PlayClick);
    }

    // 播放 PlayClick 对应演出
    private void PlayClick()
    {
// 检查 PlayClick 的前置条件
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.Play(clickClip, volume);
        }
    }
}
