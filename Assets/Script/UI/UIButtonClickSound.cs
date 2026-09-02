using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 给按钮加点击音效。挂在任意带 Button 的物体上，配一个点击音效 clip。
/// 通过代码监听 onClick，不占用按钮 Inspector 里的 OnClick 列表（原有绑定不受影响）。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonClickSound : MonoBehaviour
{
    // Inspector 中配置按钮点击时播放的音频资源。
    [Header("点击音效")]
    [SerializeField] private AudioClip clickClip;
    // 将播放音量限制在 Unity 标准归一化范围内。
    [Range(0f, 1f)]
    // 默认以原始音量播放，便于 Inspector 按按钮单独调整。
    [SerializeField] private float volume = 1f;

    // Awake 早于 Start 执行，确保按钮在首帧交互前已注册音效回调。
    private void Awake()
    {
        // RequireComponent 会在添加脚本时补齐 Button；此处无需空值分支。
        GetComponent<Button>().onClick.AddListener(PlayClick);
    }

    // 未配置 SfxManager 时静默跳过；clickClip 是否有效由音频管理器处理。
    private void PlayClick()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.Play(clickClip, volume);
        }
    }
}
