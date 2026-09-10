using UnityEngine;

// 定义 SfxManager 类型
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    // 专用 2D 声源；PlayOneShot 会在同一声源上叠加短音效
    private AudioSource source;

    // 在首个场景前完成自动注册，场景无需预置管理器对象
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// 空引用时直接退出
        if (Instance == null)
        {
// 添加所需组件
            new GameObject("SfxManager").AddComponent<SfxManager>();
        }
    }

    // 初始化为 2D、非循环声源；单例跨场景保留，重复自动创建时不接管播放
    private void Awake()
    {
        // 自动创建或场景预置重叠时，仅保留最早建立的播放器
        if (Instance != null && Instance != this)
        {
// 清理当前对象
            Destroy(gameObject);
            return;
        }

// 更新当前状态
        Instance = this;
        DontDestroyOnLoad(gameObject);

// 更新当前状态
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
// 更新当前状态
        source.loop = false;
        source.spatialBlend = 0f;
    }

// 定义 Play 方法
    public void Play(AudioClip clip, float volume = 1f)
    {
// 判断当前条件
        if (clip != null)
        {
// 配置 globalVolume 数值
            float globalVolume = SettingsManager.Instance == null ? 1f : SettingsManager.Instance.SfxVolume;
            source.PlayOneShot(clip, Mathf.Clamp01(volume * globalVolume));
        }
    }
}
