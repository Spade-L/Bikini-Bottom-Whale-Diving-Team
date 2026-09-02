using UnityEngine;

/// <summary>
/// 全局音效播放器（自动创建，跨场景常驻，不需要在场景里摆放）。
/// 供 UI 点击等一次性音效使用：SfxManager.Instance.Play(clip, volume)。
/// 与 MusicManager（背景音乐）分开，互不影响。
/// </summary>
// 管理全局一次性音效播放。
// 管理器会在首场景加载前自动创建。
// 单例跨场景保留同一个专用声源。
// 重复实例会被销毁，避免音效播放器重复。
// 声源配置为二维模式。
// 声源不自动播放，也不循环。
// PlayOneShot 支持短音效并行叠加。
// 空音频请求会被静默忽略。
// 请求音量会限制到零到一范围。
// 背景音乐由 MusicManager 单独处理。
// 场景无需手工放置该管理器对象。
// 本类不保存音效播放队列。
// 本类不处理三维空间衰减。
// 自动创建和场景预置共存时保留先建立的实例。
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    // 专用 2D 声源；PlayOneShot 会在同一声源上叠加短音效。
    private AudioSource source;

    // 在首个场景前完成自动注册，场景无需预置管理器对象。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            new GameObject("SfxManager").AddComponent<SfxManager>();
        }
    }

    // 初始化为 2D、非循环声源；单例跨场景保留，重复自动创建时不接管播放。
    private void Awake()
    {
        // 自动创建或场景预置重叠时，仅保留最早建立的播放器。
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    /// <summary>播放一次性音效（叠加播放，不打断其他音效）。</summary>
    // 空音频静默忽略；音量限制到 Unity 声源支持的标准范围。
    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}
