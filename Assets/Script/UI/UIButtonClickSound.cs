using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 给按钮加点击音效。挂在任意带 Button 的物体上，配一个点击音效 clip。
/// 通过代码监听 onClick，不占用按钮 Inspector 里的 OnClick 列表（原有绑定不受影响）。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonClickSound : MonoBehaviour
{
    [Header("点击音效")]
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.Play(clickClip, volume);
        }
    }
}
